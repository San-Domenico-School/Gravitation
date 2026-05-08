using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to a cube (or any collider). When the player touches it, loads the target scene.
/// Works with both trigger colliders (Is Trigger = true) and solid colliders.
/// </summary>
public class SceneTransitionTrigger : MonoBehaviour
{
    [Tooltip("Exact name of the scene to load (must be added to Build Settings).")]
    public string targetScene;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            LoadScene();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
            LoadScene();
    }

    private void LoadScene()
    {
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("SceneTransitionTrigger: no target scene set.", this);
            return;
        }
        SceneManager.LoadScene(targetScene);
    }
}
