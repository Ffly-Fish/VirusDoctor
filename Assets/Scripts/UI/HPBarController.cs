using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 管理红色真实血条和黄色缓动血条。
public class HPBarController : MonoBehaviour
{
    [SerializeField] private Image redBar; // 红色真实血条。
    [SerializeField] private Image yellowBar; // 黄色缓动血条。
    [SerializeField] private float duration = 0.5f; // 黄色血条的缓动时长。
    [SerializeField] private Camera targetCamera; // 用于确定朝向的摄像机。

    private Tween tween; // 黄色血条当前的缓动动画。

    // 设置血量百分比，并缓动黄色血条。
    public void SetHealth(float value)
    {
        value = Mathf.Clamp01(value);

        if (redBar != null)
        {
            redBar.transform.localScale = new Vector3(value, redBar.transform.localScale.y, redBar.transform.localScale.z);
        }

        if (yellowBar != null)
        {
            tween?.Kill();
            tween = yellowBar.transform.DOScaleX(value, duration);
        }
    }

    // 停用组件时结束血条动画。
    private void OnDisable()
    {
        tween?.Kill();
    }

    // 每帧让物体朝向摄像机。
    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        Vector3 direction = transform.position - targetCamera.transform.position; // 从物体指向摄像机的方向。
        if (direction.sqrMagnitude > 0f)
        {
            transform.rotation = Quaternion.LookRotation(direction, targetCamera.transform.up);
        }
    }
}
