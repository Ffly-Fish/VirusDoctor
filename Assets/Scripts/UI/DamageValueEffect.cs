using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// 在世界空间显示伤害数字：数字从命中点上飘，逐渐淡出。
/// 实例由静态池复用，避免每次命中都新建 Canvas / TMP 物体。
/// </summary>
public class DamageValueEffect : MonoBehaviour
{
    private static readonly Stack<DamageValueEffect> Pool = new Stack<DamageValueEffect>();

    /// <summary>控制伤害数字透明度的 CanvasGroup。</summary>
    private CanvasGroup canvasGroup;

    /// <summary>用于确定伤害数字朝向的相机。</summary>
    private Camera effectCamera;

    /// <summary>控制上飘与淡出的 DoTween 序列。</summary>
    private Tween animationTween;

    private const float MinimumDamageScale = 0.8f;
    private const float MaximumDamageScale = 1.6f;
    private const float DamageForMaximumScale = 100f;

    /// <summary>在指定位置显示一次伤害数字。</summary>
    public static void Show(Vector3 position, int damage)
    {
        DamageValueEffect effect = null;
        while (Pool.Count > 0)
        {
            effect = Pool.Pop();
            if (effect != null)
            {
                break;
            }
        }

        if (effect == null)
        {
            effect = CreateEffect();
        }

        effect.transform.position = position + Vector3.up;
        effect.SetDamage(damage);
        effect.gameObject.SetActive(true);
        effect.Play();
    }

    /// <summary>首次使用时创建一套 Canvas + TMP 结构。</summary>
    private static DamageValueEffect CreateEffect()
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

        GameObject textObject = new GameObject("DamageText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(effectObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI damageText = textObject.GetComponent<TextMeshProUGUI>();
        damageText.font = TMP_Settings.defaultFontAsset;
        damageText.fontSize = 48f;
        damageText.color = Color.red;
        damageText.alignment = TextAlignmentOptions.Center;
        damageText.enableWordWrapping = false;
        damageText.raycastTarget = false;

        return effectObject.GetComponent<DamageValueEffect>();
    }

    /// <summary>复用实例时只更新文本与字号缩放。</summary>
    private void SetDamage(int damage)
    {
        TextMeshProUGUI damageText = GetComponentInChildren<TextMeshProUGUI>(true);
        damageText.text = $"-{damage}";

        float normalizedDamage = Mathf.InverseLerp(0f, DamageForMaximumScale, Mathf.Abs((float)damage));
        float damageScale = Mathf.Lerp(MinimumDamageScale, MaximumDamageScale, normalizedDamage);
        damageText.rectTransform.localScale = Vector3.one * damageScale;
    }

    /// <summary>启动本次上飘动画，结束后回到池中。</summary>
    private void Play()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        effectCamera = Camera.main;
        GetComponent<Canvas>().worldCamera = effectCamera;

        float duration = Random.Range(1.2f, 1.8f);
        float riseDistance = Random.Range(0.6f, 1f);
        float startY = transform.position.y;

        animationTween = DOTween.Sequence()
            .Join(transform.DOMoveY(startY + riseDistance, duration).SetEase(Ease.OutSine))
            .Join(canvasGroup.DOFade(0f, duration * 0.75f).SetDelay(duration * 0.25f).SetEase(Ease.InQuad))
            .OnComplete(ReturnToPool);
    }

    /// <summary>停止动画、隐藏并回收实例。</summary>
    private void ReturnToPool()
    {
        animationTween?.Kill();
        animationTween = null;
        gameObject.SetActive(false);
        Pool.Push(this);
    }

    /// <summary>让文字始终面向相机。</summary>
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

    private void OnDestroy()
    {
        animationTween?.Kill();
    }
}
