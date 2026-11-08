using UnityEngine;

/// <summary>长按蓄力并在松开开火键时发射的武器。</summary>
[RequireComponent(typeof(Animator))]
public class ChargedWeapon : WeaponBase
{
    /// <summary>蓄力时间上限，单位为秒。</summary>
    [SerializeField, Min(0.01f)] private float maxChargeTime = 2f;

    /// <summary>武器动画控制器。</summary>
    private Animator animator;

    /// <summary>当前蓄力使用的子弹。</summary>
    private ChargeBullet chargeBullet;

    /// <summary>当前已蓄力时长。</summary>
    private float chargeTime;

    /// <summary>当前是否正在蓄力。</summary>
    private bool isCharging;

    /// <summary>获取武器图标。</summary>
    public override Sprite WeaponSprite => weaponParameters != null ? weaponParameters.WeaponSprite : null;

    /// <summary>初始化动画控制器及武器基础参数。</summary>
    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        maxChargeTime = Mathf.Max(0.01f, maxChargeTime);
    }

    /// <summary>获取当前换弹状态。</summary>
    public override bool IsReloading => isReloading;

    /// <summary>设置武器装备状态并更新界面。</summary>
    public override void SwitchWeapon(bool equipped)
    {
        isEquipped = equipped;
        gameObject.SetActive(equipped);

        if (!equipped)
        {
            CancelCharge();
        }
        else
        {
            animator.SetTrigger("switch");
            isReloading = false;
            UIManager.Instance?.SetWeapon(WeaponSprite);
            UpdateAmmoUI();
        }
    }

    /// <summary>按住开火键时开始或持续蓄力。</summary>
    public override void Fire()
    {
        if (!IsDataLoaded)
            return;

        if (isCharging)
        {
            if (!isEquipped || isReloading)
            {
                CancelCharge();
                return;
            }

            chargeTime = Mathf.Min(maxChargeTime, chargeTime + Time.deltaTime);
            chargeBullet.SetChargeProgress(chargeTime / maxChargeTime);
            return;
        }

        if (!isEquipped || isReloading || currentAmmo <= 0 || Time.time - lastShotTime < shootingInterval)
        {
            return;
        }

        Transform spawnTransform = MuzzleTransform != null ? MuzzleTransform : transform;
        IProjectile projectile = GetAvailableProjectile(spawnTransform);
        chargeBullet = projectile as ChargeBullet;
        if (chargeBullet == null)
        {
            Debug.LogWarning("蓄力武器的子弹预制体必须实现 ChargeBullet。", this);
            return;
        }

        chargeTime = 0f;
        isCharging = true;
        chargeBullet.BeginCharge(spawnTransform);
    }

    /// <summary>在松开开火键时按蓄力比例发射子弹。</summary>
    public override void ReleaseFire()
    {
        if (!IsDataLoaded || !isCharging)
        {
            return;
        }

        float chargeRatio = Mathf.Clamp01(chargeTime / maxChargeTime);
        int chargedDamage = Mathf.RoundToInt(damage * chargeRatio);
        Transform spawnTransform = MuzzleTransform != null ? MuzzleTransform : transform;
        chargeBullet.Fire(GetAimDirection(spawnTransform), chargedDamage);
        chargeBullet = null;
        isCharging = false;

        isFiring = true;
        ApplyRecoil();
        currentAmmo--;
        totalAmmo--;
        UpdateAmmoUI();
        lastShotTime = Time.time;
        animator.SetTrigger("fire");
    }

    /// <summary>取消蓄力并回收尚未发射的子弹。</summary>
    private void CancelCharge()
    {
        if (chargeBullet != null)
        {
            chargeBullet.CancelCharge();
            chargeBullet = null;
        }

        chargeTime = 0f;
        isCharging = false;
    }

    /// <summary>尝试开始换弹并取消当前蓄力。</summary>
    public override void Reload()
    {
        if (!IsDataLoaded)
            return;

        if (!isEquipped || isReloading || currentAmmo >= magazineCapacity || totalAmmo <= currentAmmo)
        {
            return;
        }

        CancelCharge();
        isReloading = true;
        animator.SetTrigger("reload");
    }

    /// <summary>增加武器备弹并刷新弹药界面。</summary>
    public override void AddAmmo(int amount)
    {
        if (amount > 0)
        {
            totalAmmo += amount;
            UpdateAmmoUI();
        }
    }

}
