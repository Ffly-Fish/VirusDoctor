using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GermSlime : MonoBehaviour, IEnemy
{
    public int Hp { get; private set; } = 100;
    public void TakeDamage(int damage)
    {
        Hp -= damage;
        if (Hp <= 0)
        {
            Die();
        }
    }
    /// <summary>
    /// ËÀÍöÂß¼­
    /// </summary>
    private void Die()
    {
        // ËÀÍöÂß¼­
        Destroy(gameObject);
    }
}