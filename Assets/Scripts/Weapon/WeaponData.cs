using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon/Weapon Data")]
public class WeaponData : ScriptableObject
{
    /// <summary>武器名称。</summary>
    [SerializeField] private string weaponName = "Weapon";
    /// <summary>武器伤害。</summary>
    [SerializeField] private float damage = 10f;
    /// <summary>两次射击之间的间隔。</summary>
    [SerializeField] private float shootingInterval = 0.2f;
    /// <summary>弹夹容量。</summary>
    [SerializeField] private int magazineCapacity = 30;

    /// <summary>获取武器名称。</summary>
    public string WeaponName => weaponName;
    /// <summary>获取武器伤害。</summary>
    public float Damage => damage;
    /// <summary>获取射击间隔。</summary>
    public float ShootingInterval => shootingInterval;
    /// <summary>获取弹夹容量。</summary>
    public int MagazineCapacity => magazineCapacity;
}
