using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEnemy
{
    /// <summary>
    /// 血量
    /// </summary>
    int Hp { get; }
    /// <summary>
    /// 受到伤害
    /// </summary>
    /// <param name="damage">伤害值</param>
    void TakeDamage(int damage);
}
