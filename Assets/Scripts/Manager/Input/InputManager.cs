using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 输入管理器（纯 C# 类，非单例，不继承 MonoBehaviour）。
///
/// 核心设计：
///   1. keyBindings    —— 键位映射表（InputAction <-> KeyCode），唯一可持久化数据
///   2. isPressed      —— 当帧是否按住
///   3. holdDuration   —— 累计按住时长（秒）
///   4. isOver         —— 当帧是否"抬起"（松手的那一帧为 true）
///
/// 关键时序（Update 三步走，顺序不可交换）：
///   ① 用【上一帧】的 isPressed     决定 holdDuration 累加或保留
///   ② 用【本帧】的 Input.GetKey     写入 isPressed
///   ③ 用"本帧未按 且 上帧按过"     得出 isOver
///
/// 蓄力语义（重要）：
///   - 按住的整段时间：isPressed=true，holdDuration 每帧累加
///   - 松手的那一帧  ：isOver=true，holdDuration 【不清零】，仍可读取本次按压时长
///   - 松手后的下一帧：isPressed=false，holdDuration 清零，isOver=false
///   由此，蓄力技能在 isOver 为 true 的当帧读取 holdDuration，即为完整蓄力时间。
///
/// 由外部（Game.Update()）每帧调用 Update()。
/// </summary>
public class InputManager : MonoBehaviour
{
    private static InputManager instance;

    public static InputManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject managerObject = new GameObject(nameof(InputManager));
                instance = managerObject.AddComponent<InputManager>();
            }

            return instance;
        }
    }

    // =====================================================================
    // 字段
    // =====================================================================

    /// <summary>键位映射表：逻辑行为 -> 物理按键。</summary>
    private readonly Dictionary<InputAction, KeyCode> keyBindings
        = new Dictionary<InputAction, KeyCode>();

    /// <summary>当帧是否按住。</summary>
    private readonly Dictionary<InputAction, bool> isPressed
        = new Dictionary<InputAction, bool>();

    /// <summary>该行为是否在当帧刚刚按下。</summary>
    private readonly Dictionary<InputAction, bool> isDown
        = new Dictionary<InputAction, bool>();

    /// <summary>
    /// 累计按住时长（秒）。
    /// 蓄力释放（isOver）的当帧仍保留有效值，供蓄力技能读取；
    /// 下一帧检测到"上一帧已抬起"后才清零。
    /// </summary>
    private readonly Dictionary<InputAction, float> holdDuration
        = new Dictionary<InputAction, float>();

    /// <summary>当帧是否抬起（松手的那一帧为 true，仅一帧）。</summary>
    private readonly Dictionary<InputAction, bool> isOver
        = new Dictionary<InputAction, bool>();

    // =====================================================================
    // 常量
    // =====================================================================

    private const string PlayerPrefsKey = "InputKeyBindings_v1";
    private const char SeparatorBinding = ';'; // 多条绑定之间
    private const char SeparatorPair = '='; // Action 与 Key 之间

    // =====================================================================
    // 构造 / 持久化
    // =====================================================================

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>从 PlayerPrefs 读取键位；若无存档则创建默认配置并立即保存。</summary>
    private void Load()
    {
        CreateDefault();

        if (!PlayerPrefs.HasKey(PlayerPrefsKey))
        {
            Save();
            return;
        }

        string saved = PlayerPrefs.GetString(PlayerPrefsKey);
        if (string.IsNullOrWhiteSpace(saved))
            return;

        foreach (string token in saved.Split(SeparatorBinding))
        {
            if (string.IsNullOrWhiteSpace(token))
                continue;

            string[] pair = token.Split(SeparatorPair);
            if (pair.Length != 2)
                continue;
            if (!int.TryParse(pair[0], out int actionId) || !int.TryParse(pair[1], out int keyCodeId))
                continue;
            if (!Enum.IsDefined(typeof(InputAction), actionId) || !Enum.IsDefined(typeof(KeyCode), keyCodeId))
                continue;

            keyBindings[(InputAction)actionId] = (KeyCode)keyCodeId;
        }

        // 容错：存档缺字段（版本升级 / 新增行为）时自动补齐默认值
        foreach (InputAction action in Enum.GetValues(typeof(InputAction)))
        {
            if (!keyBindings.ContainsKey(action))
                keyBindings[action] = GetDefaultKey(action);
        }
    }

    /// <summary>将键位映射编码为字符串并写入 PlayerPrefs。</summary>
    public void Save()
    {
        StringBuilder sb = new StringBuilder();
        foreach (KeyValuePair<InputAction, KeyCode> kv in keyBindings)
        {
            if (sb.Length > 0)
                sb.Append(SeparatorBinding);
            sb.Append((int)kv.Key).Append(SeparatorPair).Append((int)kv.Value);
        }
        PlayerPrefs.SetString(PlayerPrefsKey, sb.ToString());
        PlayerPrefs.Save();
    }

    /// <summary>创建默认键位配置（仅初始化内存，是否写盘由 Load 决定）。</summary>
    private void CreateDefault()
    {
        foreach (InputAction action in Enum.GetValues(typeof(InputAction)))
        {
            keyBindings[action] = GetDefaultKey(action);
            isPressed[action] = false;
            isDown[action] = false;
            holdDuration[action] = 0f;
            isOver[action] = false;
        }
    }

    /// <summary>每个逻辑行为对应的默认按键。</summary>
    private static KeyCode GetDefaultKey(InputAction action)
    {
        switch (action)
        {
            case InputAction.MoveForward: return KeyCode.W;
            case InputAction.MoveBackward: return KeyCode.S;
            case InputAction.MoveLeft: return KeyCode.A;
            case InputAction.MoveRight: return KeyCode.D;
            case InputAction.Jump: return KeyCode.Space;
            case InputAction.Quit: return KeyCode.Escape;
            case InputAction.Fire: return KeyCode.Mouse0; // Fire1
            case InputAction.Reload: return KeyCode.R;
            case InputAction.Sprint: return KeyCode.LeftShift;
            case InputAction.SwitchWeapon: return KeyCode.Tab;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    // =====================================================================
    // 每帧更新
    // =====================================================================

    /// <summary>
    /// 刷新 isPressed / holdDuration / isOver 三组状态。
    ///
    /// 更新顺序（重要，不可交换）：
    ///   1. 依据【上一帧】的 isPressed 决定 holdDuration：
    ///        - 上帧按住 且 本帧仍按住 → 累加 unscaledDeltaTime
    ///        - 上帧按住 且 本帧已松开 → 保留（不清零），供蓄力技能在 isOver 当帧读取
    ///        - 上帧未按           → 清零
    ///   2. 采样【本帧】物理按键，写入 isPressed
    ///   3. 由"本帧未按 且 上帧按过"得出 isOver
    ///
    /// 必须在 MonoBehaviour.Update 中调用。
    /// </summary>
    private void Update()
    {
        foreach (InputAction action in Enum.GetValues(typeof(InputAction)))
        {
            KeyCode key = keyBindings[action];
            bool wasPressed = isPressed[action];      // ① 取出【上一帧】值（此时尚未被覆盖）

            // ② 更新 holdDuration
            if (wasPressed)
            {
                // 上帧按着：若本帧仍按着则累加；若本帧已松开则【保留】，不清零
                // 这样 isOver 为 true 的当帧，holdDuration 仍是完整蓄力时长
                if (Input.GetKey(key))
                    holdDuration[action] += Time.unscaledDeltaTime;
            }
            else
            {
                // 上帧没按：本次为新的一次按压起点，时长归零
                holdDuration[action] = 0f;
            }

            // ③ 采样【本帧】物理按键
            bool nowPressed = Input.GetKey(key);
            isPressed[action] = nowPressed;
            isDown[action] = nowPressed && !wasPressed;

            // ④ 计算 isOver：本帧没按，但上一帧按过 → 这一帧就是"抬起"
            isOver[action] = !nowPressed && wasPressed;
        }
    }

    // =====================================================================
    // 改键
    // =====================================================================

    /// <summary>
    /// 修改某个行为的绑定按键。
    /// 若该按键已被其它行为占用，返回 false 且不生效；
    /// 否则生效、立即 Save()，返回 true。
    /// </summary>
    public bool Change(InputAction action, KeyCode newKey)
    {
        if (!Enum.IsDefined(typeof(InputAction), action))
            return false;

        // 改成自己，视为成功
        if (keyBindings[action] == newKey)
            return true;

        // 冲突检测
        foreach (KeyValuePair<InputAction, KeyCode> kv in keyBindings)
        {
            if (kv.Key == action)
                continue;
            if (kv.Value == newKey)
                return false; // 被占用，拒绝
        }

        keyBindings[action] = newKey;

        // 切换按键后，旧按键的"按住/时长/抬起"状态已无意义，立即归零，避免脏数据
        isPressed[action] = false;
        isDown[action] = false;
        holdDuration[action] = 0f;
        isOver[action] = false;

        Save();
        return true;
    }

    /// <summary>恢复全部默认键位并保存。</summary>
    public void ResetToDefault()
    {
        foreach (InputAction action in Enum.GetValues(typeof(InputAction)))
            keyBindings[action] = GetDefaultKey(action);
        Save();
    }

    // =====================================================================
    // 查询 API（只读，不暴露 Dictionary 引用）
    // =====================================================================

    /// <summary>该行为当前是否按住。</summary>
    public bool this[InputAction action] => isPressed[action];

    /// <summary>该行为是否在当帧刚刚按下。</summary>
    public bool GetIsDown(InputAction action) => isDown[action];

    /// <summary>
    /// 累计按住时长（秒）。
    /// 蓄力释放（isOver）的当帧仍返回本次完整按压时长；
    /// 下一帧起归零。
    /// </summary>
    public float GetHoldDuration(InputAction action) => holdDuration[action];

    /// <summary>该行为在本帧是否抬起（松手的那一帧为 true）。用于蓄力释放。</summary>
    public bool GetIsOver(InputAction action) => isOver[action];

    /// <summary>该行为当前绑定的按键。</summary>
    public KeyCode GetKeyCode(InputAction action) => keyBindings[action];

    /// <summary>移动方向向量（已归一化）。前/右为正。</summary>
    public Vector3 GetMoveDirection()
    {
        float x = (isPressed[InputAction.MoveRight] ? 1f : 0f)
                - (isPressed[InputAction.MoveLeft] ? 1f : 0f);
        float z = (isPressed[InputAction.MoveForward] ? 1f : 0f)
                - (isPressed[InputAction.MoveBackward] ? 1f : 0f);
        return new Vector3(x, 0f, z).normalized;
    }

    /// <summary>是否存在任意移动输入。</summary>
    public bool IsMoving()
    {
        return isPressed[InputAction.MoveForward]
            || isPressed[InputAction.MoveBackward]
            || isPressed[InputAction.MoveLeft]
            || isPressed[InputAction.MoveRight];
    }
}