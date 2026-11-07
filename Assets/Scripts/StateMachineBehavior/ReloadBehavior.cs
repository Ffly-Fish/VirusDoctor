using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReloadBehavior : StateMachineBehaviour
{
    /// <summary>换弹动画状态结束时完成装填。</summary>
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // 获取当前动画对象上的武器组件。
        IWeapon weapon = animator.GetComponent<IWeapon>();
        if (weapon != null)
        {
            weapon.CompleteReload();
        }
    }
}
