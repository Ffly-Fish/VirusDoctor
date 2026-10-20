using System.Collections;
using UnityEngine;

public class StraightBullet : MonoBehaviour, IProjectile
{
    [Header("子弹属性")]
    [SerializeField] private float bulletSpeed = 50f;
    [SerializeField] private float maxFlightTime = 3f;
    [SerializeField] private LayerMask raycastLayers = Physics.DefaultRaycastLayers;
    [SerializeField] private GameObject bullet;
    [SerializeField] private GameObject effect;

    private Vector3 direction;
    private int damage;
    private float elapsedFlightTime;
    private bool isAvailable = true;
    private bool isFlying;
    private bool isWaitingForEffect;

    /// <summary>从枪口位置初始化子弹的朝向、伤害和显示状态。</summary>
    public void Initialize(Transform muzzleTransform, Vector3 direction, int damage)
    {
        if (muzzleTransform == null)
        {
            Debug.LogWarning("初始化子弹失败：枪口位置为空。", this);
            return;
        }

        this.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : muzzleTransform.forward;
        this.damage = damage;
        elapsedFlightTime = 0f;
        isAvailable = false;
        isFlying = true;
        isWaitingForEffect = false;

        transform.SetPositionAndRotation(muzzleTransform.position, Quaternion.LookRotation(this.direction));
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

    public bool IsAvailable() => isAvailable;

    private void Update()
    {
        if (isFlying)
        {
            float travelDistance = bulletSpeed * Time.deltaTime;

            if (Physics.Raycast(transform.position, direction, out RaycastHit hitInfo, travelDistance, raycastLayers))
            {
                transform.position = hitInfo.point;
                Hit(hitInfo);
                return;
            }

            transform.position += direction * travelDistance;
            elapsedFlightTime += Time.deltaTime;

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

    private void Hit(RaycastHit hitInfo)
    {
        if (hitInfo.collider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            IEnemy enemy = hitInfo.collider.GetComponentInParent<IEnemy>();
            enemy?.TakeDamage(damage);
            DamageValueEffect.Show(transform.position, damage);
        }

        isFlying = false;

        if (bullet != null)
        {
            bullet.SetActive(false);
        }

        if (effect != null)
        {
            effect.transform.SetPositionAndRotation(
                hitInfo.point,
                Quaternion.LookRotation(hitInfo.normal));
            effect.SetActive(true);

            ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem particle in particles)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particle.Play();
            }

            isWaitingForEffect = particles.Length > 0;
        }

        if (!isWaitingForEffect)
        {
            CompleteEffect();
        }
    }

    private void CompleteFlight()
    {
        if (bullet != null)
        {
            bullet.SetActive(false);
        }

        isFlying = false;
        isAvailable = true;
        enabled = false;
    }

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
