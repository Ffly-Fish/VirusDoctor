using System.Collections;
using UnityEngine;

/// <summary>控制特效对象的生命周期。</summary>
public class VFXController : MonoBehaviour
{
    /// <summary>特效对象销毁前的等待时间。</summary>
    [SerializeField, Min(0f)] private float lifetime = 5f;

    /// <summary>安排特效对象在指定时间后销毁。</summary>
    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
