using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[RequireComponent(typeof(Rigidbody))]
public class GermSlime : MonoBehaviour, IEnemy
{
    /// <summary>史莱姆的最大生命值。</summary>
    [SerializeField, Min(1)] private int maxHp = 100;

    [Header("移动")]
    [SerializeField] private float sightLength = 10f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 360f;
    [SerializeField] private float groundHeight = 0.5f;
    [SerializeField] private float groundRayStartHeight = 5f;
    [SerializeField] private float groundRayDistance = 10f;

    [Header("接触伤害")]
    [SerializeField] private int contactDamage = 10;
    [SerializeField] private float damageCooldown = 1f;
    /// <summary>史莱姆死亡时播放的特效预制体。</summary>
    [SerializeField] private GameObject deathEffect;

    private Collider ownCollider;
    private int environmentLayerMask;
    private int playerLayer;
    private float nextDamageTime;
    private float movementPausedUntil;
    /// <summary>史莱姆是否已经死亡。</summary>
    private bool isDead;
    private HPBarController hpBar; // 子物体上的血条控制器。

    public int Hp { get; private set; }

    // 查找子物体血条并显示初始血量。
    private void Start()
    {
        hpBar = GetComponentInChildren<HPBarController>(true);
        UpdateHpBar();
    }

    private void Awake()
    {
        Hp = Mathf.Max(1, maxHp);
        ownCollider = GetComponent<Collider>();
        environmentLayerMask = LayerMask.GetMask("Environment");
        playerLayer = LayerMask.NameToLayer("Player");
    }

    private void Update()
    {
        if (Time.time >= movementPausedUntil)
        {
            MoveTowardPlayerHistory();
        }

        KeepGroundHeight();
    }

    private void MoveTowardPlayerHistory()
    {
        PlayerController player = PlayerController.Instance;
        if (player == null || player.PositionHistory == null)
        {
            return;
        }

        for (int i = player.PositionHistory.Count - 1; i >= 0; i--)
        {
            Vector3 targetPosition = player.PositionHistory[i];
            Vector3 toTarget = targetPosition - transform.position;
            float distance = toTarget.magnitude;

            if (distance <= 0.001f || distance > sightLength)
            {
                continue;
            }

            if (Physics.Raycast(transform.position, toTarget / distance, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            Vector3 horizontalDirection = Vector3.ProjectOnPlane(toTarget, Vector3.up).normalized;
            if (horizontalDirection.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            Quaternion targetRotation = Quaternion.LookRotation(horizontalDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
            return;
        }
    }

    private void KeepGroundHeight()
    {
        if (environmentLayerMask == 0)
        {
            return;
        }

        Vector3 rayOrigin = transform.position + Vector3.up * groundRayStartHeight;
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, environmentLayerMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 position = transform.position;
            position.y = hit.point.y + groundHeight;
            transform.position = position;
        }
    }


    private void OnTriggerStay(Collider other)
    {
        TryDamagePlayer(other);
    }

    private void TryDamagePlayer(Collider other)
    {
        if (ownCollider == null || !ownCollider.enabled || Time.time < nextDamageTime)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || playerLayer < 0 ||
            (other.gameObject.layer != playerLayer && player.gameObject.layer != playerLayer))
        {
            return;
        }

        player.TakeDamage(contactDamage);
        float cooldown = Mathf.Max(0f, damageCooldown);
        nextDamageTime = Time.time + cooldown;
        movementPausedUntil = nextDamageTime;
    }

    public void TakeDamage(int damage)
    {
        Hp -= damage;
        UpdateHpBar();

        if (Hp <= 0)
        {
            Die();
        }
    }

    // 根据当前生命值更新血条比例。
    private void UpdateHpBar()
    {
        hpBar?.SetHealth((float)Hp / Mathf.Max(1, maxHp));
    }

    /// <summary>
    /// 死亡逻辑
    /// </summary>
    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position+new Vector3(0,1.5f,0), transform.rotation);
        }

        Destroy(gameObject);
    }
}