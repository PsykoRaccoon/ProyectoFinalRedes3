using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Heals : MonoBehaviour
{
    public float healAmount;

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth player = other.GetComponent<PlayerHealth>();

        bool healed = player.Heal(healAmount);

        if (healed)
        {
            Debug.Log("Curacion:" + healAmount);
            Destroy(gameObject);
        }
    }
}
