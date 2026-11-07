using UnityEngine;

/// <summary>可在枪口蓄力放大并直线飞行的子弹。</summary>
public class ChargeBullet : MonoBehaviour, IProjectile
{
    /// <summary>子弹飞行速度。</summary>
    [Header("子弹属性")]
    [SerializeField] private float bulletSpeed = 50f;

    /// <summary>子弹飞行时间上限。</summary>
    [SerializeField] private float maxFlightTime = 3f;

    /// <summary>子弹射线检测的层级。</summary>
    [SerializeField] private LayerMask raycastLayers = Physics.DefaultRaycastLayers;

    /// <summary>子弹显示对象。</summary>
    [SerializeField] private GameObject bullet;

    /// <summary>命中时播放的特效对象。</summary>
    [SerializeField] private GameObject effect;

    /// <summary>蓄力达到上限时的子弹尺寸倍率。</summary>
    [SerializeField, Min(1f)] private float maxScaleMultiplier = 2.5f;

    /// <summary>子弹当前飞行方向。</summary>
    private Vector3 direction;

    /// <summary>子弹命中时造成的伤害。</summary>
    private int damage;

    /// <summary>当前飞行时长。</summary>
    private float elapsedFlightTime;

    /// <summary>子弹未使用时的初始尺寸。</summary>
    private Vector3 initialScale = Vector3.one;

    /// <summary>蓄力期间跟随的枪口。</summary>
    private Transform muzzleTransform;

    /// <summary>子弹是否可供弹丸池复用。</summary>
    private bool isAvailable = true;

    /// <summary>子弹是否正在飞行。</summary>
    private bool isFlying;

    /// <summary>子弹是否正在蓄力。</summary>
    private bool isCharging;

    /// <summary>是否正在等待命中特效播放结束。</summary>
    private bool isWaitingForEffect;

    /// <summary>保存子弹初始尺寸并隐藏未使用的显示对象。</summary>
    private void Awake()
    {
        if (bullet != null)
        {
            initialScale = bullet.transform.localScale;
            bullet.SetActive(false);
        }

        if (effect != null)
        {
            effect.SetActive(false);
        }
    }

    /// <summary>初始化子弹方向、伤害及显示状态。</summary>
    public void Initialize(Transform muzzleTransform, Vector3 direction, int damage)
    {
        BeginCharge(muzzleTransform);
        SetChargeProgress(1f);
        Fire(direction, damage);
    }

    /// <summary>查询子弹是否可供弹丸池复用。</summary>
    public bool IsAvailable() => isAvailable;

    /// <summary>从枪口开始蓄力并显示子弹。</summary>
    public void BeginCharge(Transform muzzleTransform)
    {
        if (muzzleTransform == null)
        {
            Debug.LogWarning("蓄力子弹初始化失败：枪口位置为空。", this);
            return;
        }

        this.muzzleTransform = muzzleTransform;
        transform.SetPositionAndRotation(muzzleTransform.position, muzzleTransform.rotation);
        isAvailable = false;
        isFlying = false;
        isCharging = true;
        isWaitingForEffect = false;
        elapsedFlightTime = 0f;
        enabled = true;

        if (bullet != null)
        {
            bullet.transform.localScale = initialScale;
            bullet.SetActive(true);
        }

        if (effect != null)
        {
            effect.SetActive(false);
        }
    }

    /// <summary>根据蓄力比例调整子弹尺寸。</summary>
    public void SetChargeProgress(float progress)
    {
        if (bullet == null)
        {
            return;
        }

        float scaleMultiplier = Mathf.Lerp(1f, maxScaleMultiplier, Mathf.Clamp01(progress));
        bullet.transform.localScale = initialScale * scaleMultiplier;
    }

    /// <summary>结束蓄力并按指定方向发射子弹。</summary>
    public void Fire(Vector3 direction, int damage)
    {
        if (!isCharging)
        {
            return;
        }

        this.direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        this.damage = damage;
        this.muzzleTransform = null;
        transform.rotation = Quaternion.LookRotation(this.direction);
        isCharging = false;
        isFlying = true;
    }

    /// <summary>取消蓄力并将子弹归还弹丸池。</summary>
    public void CancelCharge()
    {
        if (bullet != null)
        {
            bullet.SetActive(false);
            bullet.transform.localScale = initialScale;
        }

        muzzleTransform = null;
        isCharging = false;
        isFlying = false;
        isWaitingForEffect = false;
        isAvailable = true;
        enabled = false;
    }

    /// <summary>更新蓄力跟随、飞行及命中特效状态。</summary>
    private void Update()
    {
        if (isCharging)
        {
            if (muzzleTransform == null)
            {
                CancelCharge();
                return;
            }

            transform.SetPositionAndRotation(muzzleTransform.position, muzzleTransform.rotation);
            return;
        }

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

        if (isWaitingForEffect && effect != null)
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

    /// <summary>处理碰撞伤害并播放命中特效。</summary>
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
            effect.transform.SetPositionAndRotation(hitInfo.point, Quaternion.LookRotation(hitInfo.normal));
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

    /// <summary>飞行结束后隐藏子弹并释放弹丸。</summary>
    private void CompleteFlight()
    {
        if (bullet != null)
        {
            bullet.SetActive(false);
            bullet.transform.localScale = initialScale;
        }

        isFlying = false;
        isAvailable = true;
        enabled = false;
    }

    /// <summary>命中特效结束后释放子弹。</summary>
    private void CompleteEffect()
    {
        if (effect != null)
        {
            effect.SetActive(false);
        }

        if (bullet != null)
        {
            bullet.transform.localScale = initialScale;
        }

        isWaitingForEffect = false;
        isAvailable = true;
        enabled = false;
    }
}
