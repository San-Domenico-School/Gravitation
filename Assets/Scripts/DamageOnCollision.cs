/*
 * DamageOnCollision
 * 
 * This script handles damage dealing upon collision with the player.
 * Attach this script to any GameObject that should deal damage when colliding with the player.
 * When this object collides with a GameObject tagged "Player", it will call TakeDamage()
 * on the player's PlayerHealth component.
 */

using UnityEngine;

public class DamageOnCollision : MonoBehaviour
{
    [SerializeField]
    public float damageAmount = 10f;

    private void OnCollisionEnter(Collision collision)
    {
        // Check if the colliding object is tagged as "Player"
        if (collision.gameObject.CompareTag("Player"))
        {
            // Try to get the PlayerHealth component from the colliding object
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();

            // If the PlayerHealth component exists, deal damage to the player
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
            }
        }
    }
}
