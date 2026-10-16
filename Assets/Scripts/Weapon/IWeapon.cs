using System.Collections;
using UnityEngine;

public abstract class IWeapon : MonoBehaviour
{
    [Header("武器参数")]
    public WeaponData weaponParameters;

    [SerializeField] protected float damage = 10f;
    [SerializeField] protected float shootingInterval = 0.2f;
    [SerializeField] protected int magazineCapacity = 30;

    protected int currentAmmo;
    protected int totalAmmo;
    protected float lastShotTime;
    protected bool isEquipped = true;
    protected bool isReloading;

    protected virtual void Awake()
    {
        if (weaponParameters != null)
        {
            damage = weaponParameters.Damage;
            shootingInterval = weaponParameters.ShootingInterval;
            magazineCapacity = weaponParameters.MagazineCapacity;
        }

        currentAmmo = magazineCapacity;
        totalAmmo = magazineCapacity * 4;
        lastShotTime = Time.time - shootingInterval;
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

    /// <summary>
    /// 换弹。
    /// </summary>
    public abstract void Reload();

    /// <summary>
    /// 增加弹药。
    /// </summary>
    public abstract void AddAmmo(int amount);
}  
