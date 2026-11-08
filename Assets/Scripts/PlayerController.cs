using System.Collections.Generic;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    /// <summary>
    /// 玩家位置历史记录，用于回溯或其他功能
    /// </summary>
    public List<Vector3> PositionHistory { get; private set; } = new List<Vector3>();

    private const float PositionHistoryInterval = 0.2f;
    private const int PositionHistoryCapacity = 25;
    private float positionHistoryTimer;

    [Header("移动速度")]
    public float walkSpeed = 4f;

    [Header("奔跑速度")]
    public float runSpeed = 8f;

    [Header("加速 / 减速")]
    public float acceleration = 25f;
    public float deceleration = 30f;

    [Header("跳跃")]
    public float jumpHeight = 1.4f;
    public float gravity = -22f;

    [Header("鼠标转向")]
    public float mouseTurnSensitivity = 3f;

    [Header("相机控制")]
    [SerializeField]
    public Transform playerCamera;


    [Header("准星")]
    [SerializeField]
    private Texture2D texture2D;

    [SerializeField]
    private float cameraPitchSensitivity = 3f;

    [SerializeField]
    private float minCameraPitch = -90f;

    [SerializeField]
    private float maxCameraPitch = 90f;

    private CharacterController controller;
    private float currentSpeed;
    private float verticalVelocity;
    private float cameraPitch;
    private Vector2 recoilOffset;
    private Tween recoilTween;
    private Vector3 lastHorizontalDir = Vector3.forward;

    [Header("生命值")]
    [SerializeField] private int maxHealth = 100;
    /// <summary>当前生命值的内部存储。</summary>
    private int currentHealth;
    /// <summary>当前生命值；数值变化时同步生命条。</summary>
    public int CurrentHealth
    {
        get => currentHealth;
        private set
        {
            currentHealth = value;
            UIManager.Instance?.SetHp(MaxHealth > 0 ? (float)currentHealth / MaxHealth : 0f);
        }
    }
    public int MaxHealth => maxHealth;

    //[SerializeField]
    //private AutoWeapon defaultWeapon;
    [SerializeField]
    private List<WeaponBase> weaponList = new List<WeaponBase>();
    private readonly List<WeaponBase> weapons = new List<WeaponBase>();
    private int currentWeaponIndex = -1;

    private void Awake()
    {
        Instance = this;
        controller = GetComponent<CharacterController>();
        maxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = maxHealth;

        //if (defaultWeapon != null)
        //{
        //    if (!weaponList.Contains(defaultWeapon))
        //    {
        //        weaponList.Add(defaultWeapon);
        //    }

        //    //AddWeapon(defaultWeapon);
        //}

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.SetCursor(texture2D, Vector2.zero,CursorMode.ForceSoftware);

        if (playerCamera == null)
        {
            Camera childCamera = GetComponentInChildren<Camera>();

            if (childCamera != null)
            {
                playerCamera = childCamera.transform;
            }
        }

        if (playerCamera != null)
        {
            cameraPitch = Mathf.DeltaAngle(0f, playerCamera.localEulerAngles.x);
            cameraPitch = Mathf.Clamp(cameraPitch, minCameraPitch, maxCameraPitch);
        }
        else
        {
            Debug.LogWarning("PlayerController 未找到子对象相机。", this);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>同步初始生命值，处理界面管理器稍后初始化的情况。</summary>
    private void Start()
    {
        UIManager.Instance?.SetHp(MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f);
    }

    private void Update()
    {
        // 鼠标左右移动控制角色水平转向
        float mouseX = Input.GetAxis("Mouse X");
        transform.Rotate(Vector3.up, mouseX * mouseTurnSensitivity);

        // 鼠标上下移动控制相机上下旋转
        UpdateCameraRotation();
        UpdateWeaponInput();

        // 获取移动输入
        Vector3 inputDir = InputManager.Instance.GetMoveDirection();
        bool hasInput = inputDir.sqrMagnitude > 0.01f;

        // 获取目标速度
        bool isSprinting = InputManager.Instance[InputAction.Sprint] && hasInput;
        float targetSpeed = hasInput ? (isSprinting ? runSpeed : walkSpeed) : 0f;

        // 速度平滑
        float rate = hasInput ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, rate * Time.deltaTime);

        // 水平移动
        if (hasInput)
        {
            lastHorizontalDir = transform.TransformDirection(inputDir);
        }

        Vector3 horizontalMove = lastHorizontalDir * currentSpeed * Time.deltaTime;

        // 垂直移动与跳跃
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            bool jumpPressed = InputManager.Instance[InputAction.Jump];

            if (jumpPressed)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 verticalMove = Vector3.up * verticalVelocity * Time.deltaTime;

        // 合并移动
        controller.Move(horizontalMove + verticalMove);

        positionHistoryTimer += Time.deltaTime;
        if (positionHistoryTimer >= PositionHistoryInterval)
        {
            positionHistoryTimer %= PositionHistoryInterval;
            PositionHistory.Add(transform.position);

            if (PositionHistory.Count > PositionHistoryCapacity)
            {
                PositionHistory.RemoveAt(0);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != LayerMask.NameToLayer("Pickup"))
        {
            return;
        }

        WeaponPickup pickup = other.GetComponent<WeaponPickup>();
        if (pickup == null)
        {
            pickup = other.GetComponentInParent<WeaponPickup>();
        }

        if (pickup != null)
        {
            ActivateWeapon(pickup.WeaponName);
            Destroy(pickup.transform.parent.gameObject);
        }
    }

    private void ActivateWeapon(string weaponName)
    {
        foreach (WeaponBase weapon in weaponList)
        {
            if (weapon == null || weapon.weaponParameters == null || weapon.weaponParameters.WeaponName != weaponName)
            {
                continue;
            }

            if (!weapons.Contains(weapon) && !AddWeapon(weapon))
            {
                return;
            }

            int weaponIndex = weapons.IndexOf(weapon);
            if (weaponIndex < 0)
            {
                return;
            }

            if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
            {
                weapons[currentWeaponIndex].SwitchWeapon(false);
            }

            currentWeaponIndex = weaponIndex;
            weapons[currentWeaponIndex].SwitchWeapon(true);
            return;
        }
    }

    /// <summary>
    /// 将武器加入背包；第一件武器会立即装备，其余武器默认收起。
    /// </summary>
    public bool AddWeapon(WeaponBase weapon)
    {
        if (weapon == null || !weaponList.Contains(weapon) || weapons.Contains(weapon))
        {
            return false;
        }

        weapons.Add(weapon);

        if (currentWeaponIndex < 0)
        {
            currentWeaponIndex = 0;
            weapon.SwitchWeapon(true);
        }
        else
        {
            weapon.SwitchWeapon(false);
        }

        return true;
    }

    /// <summary>
    /// 为背包中的指定武器增加子弹。
    /// </summary>
    public bool AddAmmo(WeaponBase weapon, int amount)
    {
        if (weapon == null || amount <= 0 || !weapons.Contains(weapon))
        {
            return false;
        }

        weapon.AddAmmo(amount);
        return true;
    }

    public bool TakeDamage(int damage)
    {
        if (damage <= 0 || CurrentHealth <= 0)
        {
            return false;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        AudioManager.Instance.PlaySfx("PlayerHurt");
        Debug.Log($"玩家受到伤害: {damage}，当前生命值: {CurrentHealth}/{MaxHealth}");
        return true;
    }

    private void UpdateWeaponInput()
    {
        if (weapons.Count == 0)
        {
            return;
        }

        if (InputManager.Instance.GetIsDown(InputAction.SwitchWeapon))
        {
            TrySwitchWeapon();
        }

        WeaponBase currentWeapon = weapons[currentWeaponIndex];
        if (InputManager.Instance[InputAction.Fire])
        {
            currentWeapon.Fire();
            //Debug.Log($"当前武器: {currentWeapon.GetType().Name}，开火！");
        }

        if (InputManager.Instance.GetIsOver(InputAction.Fire))
        {
            currentWeapon.ReleaseFire();
        }

        if (InputManager.Instance.GetIsDown(InputAction.Reload))
        {
            currentWeapon.Reload();
            //Debug.Log($"当前武器: {currentWeapon.GetType().Name}，重新装填！");
        }
    }

    private bool TrySwitchWeapon()
    {
        if (weapons.Count < 2 || currentWeaponIndex < 0 || weapons[currentWeaponIndex].IsReloading || weapons[currentWeaponIndex].IsFiring)
        {
            return false;
        }

        weapons[currentWeaponIndex].SwitchWeapon(false);
        currentWeaponIndex = (currentWeaponIndex + 1) % weapons.Count;
        weapons[currentWeaponIndex].SwitchWeapon(true);
        return true;
    }

    private void UpdateCameraRotation()
    {
        if (playerCamera == null)
        {
            return;
        }

        float mouseY = Input.GetAxis("Mouse Y");

        // 鼠标上移时抬头，因此使用减法
        cameraPitch -= mouseY * cameraPitchSensitivity;
        cameraPitch = Mathf.Clamp(cameraPitch, minCameraPitch, maxCameraPitch);

        Vector3 localEulerAngles = playerCamera.localEulerAngles;
        playerCamera.localRotation = Quaternion.Euler(
            Mathf.Clamp(cameraPitch + recoilOffset.x, minCameraPitch, maxCameraPitch),
            localEulerAngles.y,
            localEulerAngles.z);
    }

    public void ApplyRecoil(float recoilAmount)
    {
        if (recoilAmount <= 0f || playerCamera == null)
        {
            return;
        }

        recoilTween?.Kill();

        Vector2 recoilTarget = new Vector2(
            recoilOffset.x - Mathf.Sqrt(Random.value) * recoilAmount,
            recoilOffset.y + (Random.value < 0.5f ? -1f : 1f) * Mathf.Sqrt(Random.value) * recoilAmount / 4f);

        Tween myTween = DOTween.To(
                () => recoilOffset,
                value =>
                {
                    transform.Rotate(Vector3.up, value.y - recoilOffset.y);
                    recoilOffset = value;
                },
                recoilTarget,
                0.08f)
            .SetEase(Ease.OutBack);

        myTween.OnComplete(() =>
        {
            recoilTween = DOTween.To(
                    () => recoilOffset,
                    value =>
                    {
                        transform.Rotate(Vector3.up, value.y - recoilOffset.y);
                        recoilOffset = value;
                    },
                    Vector2.zero,
                    0.2f)
                .SetEase(Ease.OutSine);
        });

        recoilTween = myTween;
    }
}