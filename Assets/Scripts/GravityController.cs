using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A persistent singleton that tracks all GravityBody instances and provides a centralized
/// way to update their gravity direction.
/// </summary>
public class GravityController : MonoBehaviour
{
    /// <summary>
    /// Singleton instance. Use GravityController.Instance to access the controller.
    /// </summary>
    public static GravityController Instance { get; private set; }

    /// <summary>
    /// Global gravity strength applied to all registered bodies.
    /// </summary>
    public float gravityStrength = 9.81f;

    /// <summary>
    /// Active set of bodies currently registered with the controller.
    /// </summary>
    public readonly List<GravityBody> activeBodies = new List<GravityBody>();

    private void Awake()
    {
        // Enforce singleton pattern.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        // Clear the singleton reference if this instance is being destroyed.
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Registers a GravityBody with the controller.
    /// </summary>
    /// <param name="body">The body to register.</param>
    public void RegisterBody(GravityBody body)
    {
        if (body == null)
            return;

        if (!activeBodies.Contains(body))
            activeBodies.Add(body);
    }

    /// <summary>
    /// Deregisters a GravityBody from the controller.
    /// </summary>
    /// <param name="body">The body to remove.</param>
    public void RemoveBody(GravityBody body)
    {
        if (body == null)
            return;

        activeBodies.Remove(body);
    }

    /// <summary>
    /// Sets gravity direction for a single body.
    /// </summary>
    /// <param name="body">The target body.</param>
    /// <param name="direction">The desired gravity direction.</param>
    public void SetGravity(GravityBody body, Vector3 direction)
    {
        if (body == null)
            return;

        Vector3 normalized = direction.normalized;
        body.SetGravity(normalized, gravityStrength);
    }

    /// <summary>
    /// Sets gravity direction for a collection of bodies.
    /// </summary>
    /// <param name="bodies">The bodies to update.</param>
    /// <param name="direction">The desired gravity direction.</param>
    public void SetGravity(List<GravityBody> bodies, Vector3 direction)
    {
        if (bodies == null || bodies.Count == 0)
            return;

        Vector3 normalized = direction.normalized;

        for (int i = 0; i < bodies.Count; i++)
        {
            GravityBody body = bodies[i];
            if (body == null)
                continue;

            body.SetGravity(normalized, gravityStrength);
        }
    }

    /// <summary>
    /// Applies a new gravity strength to all currently registered bodies without changing direction.
    /// </summary>
    /// <param name="strength">The new gravity strength.</param>
    public void ApplyGravityStrengthToActiveBodies(float strength)
    {
        gravityStrength = strength;

        for (int i = 0; i < activeBodies.Count; i++)
        {
            GravityBody body = activeBodies[i];
            if (body == null)
                continue;

            body.SetGravity(body.gravityDirection, strength);
        }
    }
}
