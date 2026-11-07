using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>计时结束后对范围内敌人造成伤害的投射物。</summary>
[RequireComponent(typeof(Rigidbody))]
public class RealBullets : MonoBehaviour, IProjectile
{
    [Header("子弹属性")]
    /// <summary>子弹触发爆炸前的持续时间。</summary>
    [SerializeField] private float maxFlightTime = 3f;
    /// <summary>子弹的初始飞行速度。</summary>
    [SerializeField] private float bulletSpeed = 5f;
    /// <summary>爆炸伤害的球形范围半径。</summary>
    [SerializeField] private float explosionRadius = 3f;
    /// <summary>子弹的显示对象。</summary>
    [SerializeField] private GameObject bullet;
    /// <summary>子弹爆炸时播放的特效对象。</summary>
    [SerializeField] private GameObject effect;

    /// <summary>子弹造成的伤害。</summary>
    private int damage;
    /// <summary>子弹已等待的时间。</summary>
    private float elapsedFlightTime;
    /// <summary>子弹是否可供再次使用。</summary>
    private bool isAvailable = true;
    /// <summary>子弹是否正在等待爆炸。</summary>
    private bool isWaitingForExplosion;
    /// <summary>子弹是否正在等待爆炸特效播放结束。</summary>
    private bool isWaitingForEffect;
    /// <summary>子弹的刚体组件。</summary>
    private Rigidbody bulletRigidbody;

    /// <summary>获取子弹的刚体组件。</summary>
    private void Awake()
    {
        bulletRigidbody = GetComponent<Rigidbody>();
    }

    /// <summary>初始化子弹的位置、朝向、初速度、伤害和显示状态。</summary>
    public void Initialize(Transform muzzleTransform, Vector3 direction, int damage)
    {
        if (muzzleTransform == null)
        {
            Debug.LogWarning("初始化子弹失败：枪口位置为空。", this);
            return;
        }

        Vector3 launchDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : muzzleTransform.forward;
        this.damage = damage;
        elapsedFlightTime = 0f;
        isAvailable = false;
        isWaitingForExplosion = true;
        isWaitingForEffect = false;

        transform.SetPositionAndRotation(muzzleTransform.position, Quaternion.LookRotation(launchDirection));
        bulletRigidbody.velocity = launchDirection * bulletSpeed;
        bulletRigidbody.angularVelocity = Vector3.zero;
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

    /// <summary>等待爆炸时间并检查特效播放状态。</summary>
    private void Update()
    {
        if (isWaitingForExplosion)
        {
            elapsedFlightTime += Time.deltaTime;
            if (elapsedFlightTime >= maxFlightTime)
            {
                Explode();
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

    /// <summary>隐藏子弹并对爆炸范围内的敌人造成伤害。</summary>
    private void Explode()
    {
        isWaitingForExplosion = false;

        if (bullet != null)
        {
            bullet.SetActive(false);
        }

        DamageEnemiesInRange(transform.position);
        PlayEffect();
    }

    /// <summary>对球形范围内的 Enemy 层对象造成伤害。</summary>
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

    /// <summary>在当前位置播放爆炸特效。</summary>
    private void PlayEffect()
    {
        if (effect == null)
        {
            CompleteEffect();
            return;
        }

        effect.transform.position = transform.position;
        effect.SetActive(true);

        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>();
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
