using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 创建并播放世界空间伤害数字：数字会面向主相机、向上飘动并逐渐淡出。
/// </summary>
public class DamageValueEffect : MonoBehaviour
{
    /// <summary>用于控制伤害数字透明度的 CanvasGroup。</summary>
    private CanvasGroup canvasGroup;

    /// <summary>用于确定伤害数字朝向的主相机。</summary>
    private Camera effectCamera;

    /// <summary>控制数字上升和淡出的 DoTween 动画。</summary>
    private Tween animationTween;

    /// <summary>伤害值较小时数字采用的缩放比例。</summary>
    private const float MinimumDamageScale = 0.8f;

    /// <summary>伤害值达到缩放上限时数字采用的缩放比例。</summary>
    private const float MaximumDamageScale = 1.6f;

    /// <summary>达到此伤害值后使用最大数字缩放比例。</summary>
    private const float DamageForMaximumScale = 100f;

    /// <summary>
    /// 在指定位置创建伤害数字，并开始播放上升和淡出动画。
    /// </summary>
    /// <param name="position">受击对象的世界坐标。</param>
    /// <param name="damage">受到的伤害值。</param>
    public static void Show(Vector3 position, int damage)
    {
        GameObject effectObject = new GameObject(
            "DamageValueEffect",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasGroup),
            typeof(DamageValueEffect));

        Canvas canvas = effectObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        RectTransform effectRect = effectObject.GetComponent<RectTransform>();
        effectRect.sizeDelta = new Vector2(180f, 80f);
        effectRect.localScale = Vector3.one * 0.01f;
        effectObject.transform.position = position + Vector3.up;

        GameObject textObject = new GameObject("DamageText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(effectObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI damageText = textObject.GetComponent<TextMeshProUGUI>();
        damageText.font = TMP_Settings.defaultFontAsset;
        damageText.text = $"-{damage}";
        damageText.fontSize = 48f;
        damageText.color = Color.red;
        damageText.alignment = TextAlignmentOptions.Center;
        damageText.enableWordWrapping = false;
        damageText.raycastTarget = false;

        float normalizedDamage = Mathf.InverseLerp(0f, DamageForMaximumScale, Mathf.Abs((float)damage));
        float damageScale = Mathf.Lerp(MinimumDamageScale, MaximumDamageScale, normalizedDamage);
        damageText.rectTransform.localScale = Vector3.one * damageScale;

        effectObject.GetComponent<DamageValueEffect>().Play();
    }

    /// <summary>随机设置动画参数，并启动数字上升和淡出动画。</summary>
    private void Play()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        effectCamera = Camera.main;

        float duration = Random.Range(1.2f, 1.8f);
        float riseDistance = Random.Range(0.6f, 1f);
        float startY = transform.position.y;

        animationTween = DOTween.Sequence()
            .Join(transform.DOMoveY(startY + riseDistance, duration).SetEase(Ease.OutSine))
            .Join(canvasGroup.DOFade(0f, duration * 0.75f).SetDelay(duration * 0.25f).SetEase(Ease.InQuad))
            .OnComplete(() => Destroy(gameObject));
    }

    /// <summary>每帧调整特效朝向，让世界空间文字始终正面朝向主相机。</summary>
    private void LateUpdate()
    {
        if (effectCamera == null)
        {
            return;
        }

        Vector3 directionAwayFromCamera = transform.position - effectCamera.transform.position;
        if (directionAwayFromCamera.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(directionAwayFromCamera, effectCamera.transform.up);
        }
    }

    /// <summary>销毁特效对象时停止关联的 DoTween 动画。</summary>
    private void OnDestroy()
    {
        animationTween?.Kill();
    }
}
