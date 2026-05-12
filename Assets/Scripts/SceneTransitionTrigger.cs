using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a cube (or any collider). When the player touches it, loads the target scene.
/// Works with both trigger colliders (Is Trigger = true) and solid colliders.
///
/// Pairs with <see cref="PlayerPersistence"/>: when the player is DDOL'd, this trigger
/// teleports them to the named spawn point in the destination scene.
/// </summary>
public class SceneTransitionTrigger : MonoBehaviour
{
    [Tooltip("Exact name of the scene to load (must be added to Build Settings).")]
    public string targetScene;

    [Tooltip("Name of the empty GameObject in the destination scene where the player should land. Leave empty to use PlayerPersistence's default ('PlayerSpawnpoint').")]
    public string targetSpawnPoint;

    [Tooltip("If true, only the player can trigger this. If false, any collider works.")]
    public bool playerOnly = true;

    private bool fired;

    private void OnTriggerEnter(Collider other)
    {
        if (!playerOnly || other.CompareTag("Player"))
            LoadScene();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!playerOnly || collision.gameObject.CompareTag("Player"))
            LoadScene();
    }

    private void LoadScene()
    {
        if (fired) return; // prevent re-trigger while loading
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("SceneTransitionTrigger: no target scene set.", this);
            return;
        }
        fired = true;

        // PlayerPersistence reads this on the next sceneLoaded and teleports there.
        if (!string.IsNullOrEmpty(targetSpawnPoint))
            PlayerPersistence.PendingSpawnPointName = targetSpawnPoint;

        SceneManager.LoadScene(targetScene);
    }
}
