using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that manages all gravity zones in the world.
/// Allows the GravityController to query whether a body is affected by gravity anomalies.
/// </summary>
public class GravityZoneManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static GravityZoneManager Instance { get; private set; }

    /// <summary>
    /// All active gravity zones in the current world.
    /// </summary>
    private List<GravityZone> activeZones = new List<GravityZone>();

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
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Registers a gravity zone with the manager.
    /// </summary>
    public void RegisterZone(GravityZone zone)
    {
        if (zone == null || activeZones.Contains(zone))
            return;

        activeZones.Add(zone);
    }

    /// <summary>
    /// Unregisters a gravity zone from the manager.
    /// </summary>
    public void UnregisterZone(GravityZone zone)
    {
        if (zone == null)
            return;

        activeZones.Remove(zone);
    }

    /// <summary>
    /// Clears all registered gravity zones.
    /// Call this when generating a new world.
    /// </summary>
    public void ClearAllZones()
    {
        activeZones.Clear();
    }

    /// <summary>
    /// Finds the strongest gravity zone affecting the given position, if any.
    /// Returns null if no zone affects this position.
    /// </summary>
    public GravityZone GetZoneAtPosition(Vector3 position)
    {
        GravityZone strongestZone = null;
        float maxInfluence = 0f;

        for (int i = 0; i < activeZones.Count; i++)
        {
            GravityZone zone = activeZones[i];
            if (zone == null)
                continue;

            float influence = zone.GetInfluenceAtPosition(position);

            if (influence > maxInfluence)
            {
                maxInfluence = influence;
                strongestZone = zone;
            }
        }

        return strongestZone;
    }

    /// <summary>
    /// Gets all zones that affect the given position.
    /// Used for blending multiple zone effects (advanced feature for later).
    /// </summary>
    public void GetZonesAtPosition(Vector3 position, List<GravityZone> result)
    {
        result.Clear();

        for (int i = 0; i < activeZones.Count; i++)
        {
            GravityZone zone = activeZones[i];
            if (zone == null)
                continue;

            if (zone.GetInfluenceAtPosition(position) > 0f)
                result.Add(zone);
        }
    }

    /// <summary>
    /// Returns the count of active zones.
    /// </summary>
    public int GetZoneCount()
    {
        return activeZones.Count;
    }
}
