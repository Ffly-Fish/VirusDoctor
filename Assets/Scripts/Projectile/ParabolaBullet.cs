using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>沿抛物线飞行并处理命中的子弹。</summary>
public class ParabolaBullet : MonoBehaviour, IProjectile
{
    [Header("子弹属性")]
    /// <summary>子弹的初始飞行速度。</summary>
    [SerializeField] private float bulletSpeed = 50f;
    /// <summary>子弹的最大飞行时间。</summary>
    [SerializeField] private float maxFlightTime = 3f;
    /// <summary>子弹受重力加速度，单位为米每二次方秒。</summary>
    [SerializeField] private float gravity = 9.81f;
    /// <summary>爆炸伤害的球形范围半径。</summary>
    [SerializeField] private float explosionRadius = 3f;
    /// <summary>子弹可检测的碰撞层。</summary>
    [SerializeField] private LayerMask raycastLayers = Physics.DefaultRaycastLayers;
    /// <summary>子弹的显示对象。</summary>
    [SerializeField] private GameObject bullet;
    /// <summary>子弹命中后的特效对象。</summary>
    [SerializeField] private GameObject effect;

    /// <summary>子弹当前的速度向量。</summary>
    private Vector3 velocity;
    /// <summary>子弹造成的伤害。</summary>
    private int damage;
    /// <summary>子弹已飞行的时间。</summary>
    private float elapsedFlightTime;
    /// <summary>子弹是否可供再次使用。</summary>
    private bool isAvailable = true;
    /// <summary>子弹是否正在飞行。</summary>
    private bool isFlying;
    /// <summary>子弹是否正在等待特效播放结束。</summary>
    private bool isWaitingForEffect;

    /// <summary>从枪口位置初始化子弹的速度、伤害和显示状态。</summary>
    public void Initialize(Transform muzzleTransform, Vector3 direction, int damage)
    {
        if (muzzleTransform == null)
        {
            Debug.LogWarning("初始化子弹失败：枪口位置为空。", this);
            return;
        }

        Vector3 launchDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : muzzleTransform.forward;
        velocity = launchDirection * bulletSpeed;
        this.damage = damage;
        elapsedFlightTime = 0f;
        isAvailable = false;
        isFlying = true;
        isWaitingForEffect = false;

        transform.SetPositionAndRotation(muzzleTransform.position, Quaternion.LookRotation(launchDirection));
        enabled = true;

        if (bullet != null)
        {
            bullet.SetActive(true);
        }

        if (effect != null)
        {
            effect.SetActive(false);
        }
    }

    /// <summary>返回子弹是否可供再次使用。</summary>
    public bool IsAvailable() => isAvailable;

    /// <summary>推进子弹飞行并等待命中特效结束。</summary>
    private void Update()
    {
        if (isFlying)
        {
            float deltaTime = Mathf.Min(Time.deltaTime, Mathf.Max(0f, maxFlightTime - elapsedFlightTime));
            if (deltaTime <= 0f)
            {
                CompleteFlight();
                return;
            }

            Vector3 acceleration = Vector3.down * gravity;
            Vector3 displacement = velocity * deltaTime + 0.5f * acceleration * deltaTime * deltaTime;
            float travelDistance = displacement.magnitude;
            velocity += acceleration * deltaTime;
            elapsedFlightTime += deltaTime;

            if (travelDistance > 0f && Physics.Raycast(transform.position, displacement / travelDistance, out RaycastHit hitInfo, travelDistance, raycastLayers))
            {
                Bounce(hitInfo);
            }
            else
            {
                transform.position += displacement;
            }

            if (velocity.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(velocity);
            }

            if (elapsedFlightTime >= maxFlightTime)
            {
                CompleteFlight();
            }

            return;
        }

        if (isWaitingForEffect)
        {
            foreach (ParticleSystem particle in effect.GetComponentsInChildren<ParticleSystem>())
            {
                if (particle.IsAlive(true))
                {
                    return;
                }
            }

            CompleteEffect();
        }
    }

    /// <summary>让子弹沿碰撞法线反弹。</summary>
    private void Bounce(RaycastHit hitInfo)
    {
        transform.position = hitInfo.point + hitInfo.normal * 0.001f;
        velocity = Vector3.Reflect(velocity, hitInfo.normal) * Mathf.Sqrt(1f / 3f);
    }

    /// <summary>结束飞行并在当前位置触发爆炸特效。</summary>
    private void CompleteFlight()
    {
        if (bullet != null)
        {
            bullet.SetActive(false);
        }

        isFlying = false;
        DamageEnemiesInRange(transform.position);
        PlayEffect(transform.position, velocity.sqrMagnitude > 0.0001f ? -velocity.normalized : transform.forward);
    }

    /// <summary>对爆炸球形范围内的敌人造成伤害。</summary>
    private void DamageEnemiesInRange(Vector3 position)
    {
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer < 0)
        {
            return;
        }

        Collider[] colliders = Physics.OverlapSphere(position, Mathf.Max(0f, explosionRadius), 1 << enemyLayer);
        HashSet<IEnemy> damagedEnemies = new HashSet<IEnemy>();
        foreach (Collider enemyCollider in colliders)
        {
            IEnemy enemy = enemyCollider.GetComponentInParent<IEnemy>();
            if (enemy != null && damagedEnemies.Add(enemy))
            {
                enemy.TakeDamage(damage);
                DamageValueEffect.Show(enemyCollider.ClosestPoint(position), damage);
            }
        }
    }

    /// <summary>在指定位置播放爆炸特效。</summary>
    private void PlayEffect(Vector3 position, Vector3 normal)
    {
        if (effect == null)
        {
            CompleteEffect();
            return;
        }

        effect.transform.SetPositionAndRotation(position, Quaternion.LookRotation(normal));
        effect.SetActive(true);

        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem particle in particles)
        {
            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Play();
        }

        isWaitingForEffect = particles.Length > 0;
        if (!isWaitingForEffect)
        {
            CompleteEffect();
        }
    }

    /// <summary>结束特效播放并释放子弹。</summary>
    private void CompleteEffect()
    {
        if (effect != null)
        {
            effect.SetActive(false);
        }

        isWaitingForEffect = false;
        isAvailable = true;
        enabled = false;
    }
}
