using UnityEngine;

[RequireComponent(typeof(Animator))]
/// <summary>连发武器：按住开火，按射击间隔连续发射。</summary>
public class AutoWeapon : WeaponBase
{
    /// <summary>获取武器数据中的界面图标。</summary>
    public override Sprite WeaponSprite => weaponParameters != null ? weaponParameters.WeaponSprite : null;

    /// <summary>武器自身的动画组件。</summary>
    private Animator animator;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// 当前是否正在播放换弹动作。
    /// </summary>
    public override bool IsReloading => isReloading;

    /// <summary>
    /// 设置武器的装备状态。
    /// </summary>
    public override void SwitchWeapon(bool equipped)
    {
        isEquipped = equipped;
        gameObject.SetActive(equipped);
        if (equipped && animator != null)
        {
            animator.SetTrigger("switch");
            isReloading = false; // 切换武器时重置换弹状态
        }

        if (equipped)
        {
            UIManager.Instance?.SetWeapon(WeaponSprite);
            UpdateAmmoUI();
        }
    }

    /// <summary>
    /// 在射击间隔和弹药允许时开火。
    /// </summary>
    public override void Fire()
    {
        if (!IsDataLoaded)
            return;

        if (!isEquipped || isReloading || currentAmmo <= 0 || Time.time - lastShotTime < shootingInterval)
            return;

        if (SpawnBullet() == null)
            return;

        isFiring = true;
        ApplyRecoil();
        Debug.Log($"开火！伤害: {damage}, 剩余弹药: {currentAmmo - 1}/{totalAmmo - 1}");
        currentAmmo--;
        totalAmmo--;
        UpdateAmmoUI();
        lastShotTime = Time.time;
        animator.SetTrigger("fire");
    }

    /// <summary>
    /// 从剩余子弹中向当前弹夹补充弹药。
    /// </summary>
    public override void Reload()
    {
        if (!IsDataLoaded)
            return;

        if (!isEquipped || isReloading || currentAmmo >= magazineCapacity || totalAmmo <= currentAmmo)
            return;

        isReloading = true;
        animator.SetTrigger("reload");
    }

    /// <summary>
    /// 增加可用弹药。
    /// </summary>
    public override void AddAmmo(int amount)
    {
        if (amount > 0)
        {
            totalAmmo += amount;
            UpdateAmmoUI();
        }
    }

}
