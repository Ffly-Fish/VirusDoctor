using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IProjectile
{
    /// <summary>
    /// 是否可得
    /// </summary>
    /// <returns></returns>
    bool IsAvailable();
    /// <summary>
    /// 初始化子弹的朝向、伤害和显示状态。
    /// </summary>
    /// <param name="muzzleTransform">枪口位置</param>
    /// <param name="direction">子弹的飞行方向</param>
    /// <param name="damage">子弹的伤害值</param>
    void Initialize(Transform muzzleTransform, Vector3 direction, int damage);


}
