using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public abstract class IWeapon : MonoBehaviour
{
    [Header("武器参数")]
    public WeaponData weaponParameters;

    [Header("切换武器音效")]
    [SerializeField] private AudioClip switchAudioClip;

    [Header("开枪音效")]
    [SerializeField] private AudioClip fireAudioClip;

    [Header("换弹音效")]
    [SerializeField] private AudioClip reloadAudioClip;

    [Header("枪口位置")]
    [SerializeField] private Transform muzzleTransform;

    /// <summary>获取当前武器的枪口变换。</summary>
    protected Transform MuzzleTransform => muzzleTransform;

    [Header("子弹预制体")]
    /// <summary>开火时生成的子弹预制体。</summary>
    [SerializeField] protected GameObject bulletPrefab;

    private readonly List<IProjectile> projectilePool = new List<IProjectile>();

    protected int damage = 10;
    protected float shootingInterval = 0.2f;
    protected int magazineCapacity = 30;
    protected float shootingOffset;
    protected float recoilAmount;

    private AudioSource audioSource;
    private PlayerController playerController;

    protected int currentAmmo;
    protected int totalAmmo;
    protected float lastShotTime;
    protected bool isEquipped = true;
    protected bool isReloading;
    protected bool isFiring;

    public bool IsFiring => isFiring;

    protected virtual void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        playerController = GetComponentInParent<PlayerController>();

        if (weaponParameters != null)
        {
            damage = weaponParameters.Damage;
            shootingInterval = weaponParameters.ShootingInterval;
            magazineCapacity = weaponParameters.MagazineCapacity;
            shootingOffset = weaponParameters.ShootingOffset;
            recoilAmount = weaponParameters.RecoilAmount;
        }

        currentAmmo = magazineCapacity;
        totalAmmo = magazineCapacity * 4;
        lastShotTime = Time.time - shootingInterval;
    }

    /// <summary>由切换武器动画事件调用，播放切换音效。</summary>
    public void PlaySwitchAudio()
    {
        PlayAudio(switchAudioClip);
    }

    /// <summary>由开枪动画事件调用，播放开枪音效。</summary>
    public void PlayFireAudio()
    {
        isFiring = false;
        PlayAudio(fireAudioClip);
    }

    /// <summary>由换弹动画事件调用，播放换弹音效。</summary>
    public void PlayReloadAudio()
    {
        PlayAudio(reloadAudioClip);
    }

    private void PlayAudio(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>由换弹动画事件调用，完成弹药装填。</summary>
    public void CompleteReload()
    {
        if (!isReloading)
        {
            return;
        }

        int remainingAmmo = totalAmmo - currentAmmo;
        int ammoToLoad = Mathf.Min(magazineCapacity - currentAmmo, remainingAmmo);
        currentAmmo += ammoToLoad;
        isReloading = false;
        UpdateAmmoUI();
        Debug.Log($"换弹完成！当前弹药: {currentAmmo}/{totalAmmo}");
    }

    /// <summary>根据当前弹夹余量更新弹药界面。</summary>
    protected void UpdateAmmoUI()
    {
        if (isEquipped)
        {
            float ammoRatio = magazineCapacity > 0 ? (float)currentAmmo / magazineCapacity : 0f;
            UIManager.Instance?.SetAmmo(ammoRatio);
        }
    }

    protected IProjectile SpawnBullet()
    {
        if (bulletPrefab == null)
        {
            return null;
        }

        Transform spawnTransform = muzzleTransform != null ? muzzleTransform : transform;
        IProjectile projectile = GetAvailableProjectile(spawnTransform);
        if (projectile == null)
        {
            return null;
        }

        Vector3 aimDirection = GetAimDirection(spawnTransform);
        projectile.Initialize(spawnTransform, aimDirection, damage);
        return projectile;
    }

    /// <summary>获取可用弹丸；池中没有可用弹丸时创建一个。</summary>
    protected IProjectile GetAvailableProjectile(Transform spawnTransform)
    {
        if (bulletPrefab == null || spawnTransform == null)
        {
            return null;
        }

        IProjectile projectile = null;
        foreach (IProjectile pooledProjectile in projectilePool)
        {
            if (pooledProjectile is MonoBehaviour behaviour && behaviour != null && pooledProjectile.IsAvailable())
            {
                projectile = pooledProjectile;
                break;
            }
        }

        if (projectile == null)
        {
            GameObject bulletObject = Instantiate(bulletPrefab, spawnTransform.position, spawnTransform.rotation);
            projectile = bulletObject.GetComponent<IProjectile>();

            if (projectile == null)
            {
                Debug.LogWarning("子弹预制体未实现 IProjectile 接口。", bulletObject);
                Destroy(bulletObject);
                return null;
            }

            projectilePool.Add(projectile);
        }

        return projectile;
    }

    protected void ApplyRecoil()
    {
        if (playerController != null && recoilAmount > 0f)
        {
            playerController.ApplyRecoil(recoilAmount);
        }
    }

    /// <summary>根据鼠标位置和武器散布计算瞄准方向。</summary>
    protected Vector3 GetAimDirection(Transform spawnTransform)
    {
        Camera aimCamera = Camera.main;
        Ray aimRay = aimCamera != null
            ? aimCamera.ScreenPointToRay(Input.mousePosition)
            : new Ray(spawnTransform.position, spawnTransform.forward);
        Vector3 aimPoint = Physics.Raycast(aimRay, out RaycastHit hitInfo)
            ? hitInfo.point
            : aimRay.GetPoint(1000f);
        Vector3 direction = (aimPoint - spawnTransform.position).normalized;

        if (shootingOffset > 0f)
        {
            Vector2 spread = Random.insideUnitCircle * shootingOffset;
            direction = Quaternion.LookRotation(direction) * Quaternion.Euler(-spread.y, spread.x, 0f) * Vector3.forward;
        }

        return direction;
    }

    /// <summary>
    /// 当前是否正在换弹。
    /// </summary>
    public abstract bool IsReloading { get; }

    /// <summary>
    /// 设置武器的装备状态。
    /// </summary>
    public abstract void SwitchWeapon(bool isEquipped);

    /// <summary>
    /// 开火。
    /// </summary>
    public abstract void Fire();

    /// <summary>处理开火按键松开事件，供蓄力武器在松开时发射。</summary>
    public virtual void ReleaseFire()
    {
    }

    /// <summary>
    /// 换弹。
    /// </summary>
    public abstract void Reload();

    /// <summary>
    /// 增加弹药。
    /// </summary>
    public abstract void AddAmmo(int amount);
}  
