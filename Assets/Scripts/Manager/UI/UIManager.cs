using UnityEngine;
using UnityEngine;
using UnityEngine.UI;

// 管理全局界面引用。
public class UIManager : MonoBehaviour
{
    [SerializeField] private Image hpBar; // HPbar 图像引用。
    [SerializeField] private Image weaponImage;//武器图像引用。
    [SerializeField] private Image ammoBar; // Ammobar 图像引用。
    /// <summary>受伤时显示的红色覆盖效果。</summary>
    [SerializeField] private Image injuryFX;
    /// <summary>上一次记录的血条比例。</summary>
    private float previousHp = 1f;

    // 当前唯一的界面管理器实例。
    public static UIManager Instance { get; private set; }

    // 设置 HP 血条在 X 轴上的缩放。
    public void SetHp(float value)
    {
        float currentHp = Mathf.Clamp01(value); // 当前血条比例。
        if (hpBar != null)
        {
            Vector3 scale = hpBar.transform.localScale; // 当前血条缩放。
            scale.x = currentHp;
            hpBar.transform.localScale = scale;
        }

        if (currentHp < previousHp && injuryFX != null)
        {
            Color color = injuryFX.color; // 受伤效果当前颜色。
            float alphaIncrease = (previousHp - currentHp) / 0.25f * 255f / 255f; // 本次血量下降对应的不透明度增量。
            color.a = Mathf.Clamp01(color.a + alphaIncrease);
            injuryFX.color = color;
        }

        previousHp = currentHp;
    }

    // 每帧以每秒 32 点的速度降低受伤效果不透明度。
    private void Update()
    {
        if (injuryFX == null)
        {
            return;
        }

        Color color = injuryFX.color; // 受伤效果当前颜色。
        if (color.a > 0f)
        {
            color.a = Mathf.MoveTowards(color.a, 0f, 32f / 255f * Time.deltaTime);
            injuryFX.color = color;
        }
    }

    // 设置 Ammo 血条在 X 轴上的缩放。
    public void SetAmmo(float value)
    {
        if (ammoBar == null)
        {
            return;
        }

        Vector3 scale = ammoBar.transform.localScale; // 当前弹药条缩放。
        scale.x = Mathf.Clamp01(value);
        ammoBar.transform.localScale = scale;
    }

    // 设置武器图像。
    public void SetWeapon(Sprite sprite)
    {
        if (weaponImage != null)
        {
            weaponImage.sprite = sprite;
        }
    }

    // 设置单例并销毁重复实例。
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // 销毁实例时清除单例引用。
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
