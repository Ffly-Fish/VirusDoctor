using System.Collections.Generic;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
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
    [SerializeField]
    private Weapon defaultWeapon;
    [SerializeField]
    private List<Weapon> weaponList = new List<Weapon>();
    private readonly List<IWeapon> weapons = new List<IWeapon>();
    private int currentWeaponIndex = -1;

    private void Awake()
    {
        Instance = this;
        controller = GetComponent<CharacterController>();

        if (defaultWeapon != null)
        {
            if (!weaponList.Contains(defaultWeapon))
            {
                weaponList.Add(defaultWeapon);
            }

            //AddWeapon(defaultWeapon);
        }

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
        foreach (Weapon weapon in weaponList)
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
    public bool AddWeapon(Weapon weapon)
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
    public bool AddAmmo(IWeapon weapon, int amount)
    {
        if (weapon == null || amount <= 0 || !weapons.Contains(weapon))
        {
            return false;
        }

        weapon.AddAmmo(amount);
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

        IWeapon currentWeapon = weapons[currentWeaponIndex];
        if (InputManager.Instance[InputAction.Fire])
        {
            currentWeapon.Fire();
            //Debug.Log($"当前武器: {currentWeapon.GetType().Name}，开火！");
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