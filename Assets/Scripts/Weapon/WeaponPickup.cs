using DG.Tweening;
using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private string weaponName = "Weapon";
    [SerializeField] private float rotationDuration = 3f;
    [SerializeField] private float bobHeight = 0.25f;
    [SerializeField] private float bobDuration = 1f;

    private Tween rotationTween;
    private Tween bobTween;

    public string WeaponName => weaponName;

    private void OnEnable()
    {
        Vector3 localEulerAngles = transform.localEulerAngles;
        rotationTween = transform
            .DOLocalRotate(localEulerAngles + Vector3.up * 360f, rotationDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Incremental);

        bobTween = transform
            .DOLocalMoveY(transform.localPosition.y + bobHeight, bobDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void OnDisable()
    {
        rotationTween?.Kill();
        bobTween?.Kill();
    }
}
