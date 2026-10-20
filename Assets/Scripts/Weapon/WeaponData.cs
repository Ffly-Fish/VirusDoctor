using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon/Weapon Data")]
public class WeaponData : ScriptableObject
{
    /// <summary>武器名称。</summary>
    [SerializeField] private string weaponName = "Weapon";
    /// <summary>武器伤害。</summary>
    [SerializeField] private int damage = 10;
    /// <summary>两次射击之间的间隔。</summary>
    [SerializeField] private float shootingInterval = 0.2f;
    /// <summary>弹夹容量。</summary>
    [SerializeField] private int magazineCapacity = 30;
    [Header("射击设置")]
    [Tooltip("射击方向的随机偏移角度（度）。")]
    [SerializeField, Min(0f)] private float shootingOffset = 0f;
    [Tooltip("每次开火产生的随机后坐力角度（度）。")]
    [SerializeField, Min(0f)] private float recoilAmount = 0f;

    /// <summary>获取武器名称。</summary>
    public string WeaponName => weaponName;
    /// <summary>获取武器伤害。</summary>
    public int Damage => damage;
    /// <summary>获取射击间隔。</summary>
    public float ShootingInterval => shootingInterval;
    /// <summary>获取弹夹容量。</summary>
    public int MagazineCapacity => magazineCapacity;
    public float ShootingOffset => shootingOffset;
    public float RecoilAmount => recoilAmount;
}
