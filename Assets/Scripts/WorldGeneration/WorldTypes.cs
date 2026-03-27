using System;
using UnityEngine;

public enum WorldType
{
    Stone = 0,
    Wood = 1,
    Iron = 2
}

public enum PersistentObjectKind
{
    Pickup = 0,
    GravityProp = 1
}

public static class WorldTypeUtility
{
    public static string ToDisplayName(WorldType worldType)
    {
        return worldType.ToString();
    }

    public static int GetStableSeed(string worldKey)
    {
        unchecked
        {
            const int offset = unchecked((int)2166136261u);
            const int prime = 16777619;
            int hash = offset;

            for (int i = 0; i < worldKey.Length; i++)
            {
                hash ^= worldKey[i];
                hash *= prime;
            }

            return hash;
        }
    }

    public static Vector2 GetNoiseOffset(string worldKey)
    {
        int seed = GetStableSeed(worldKey);
        float x = Mathf.Abs(seed % 10000) * 0.001f;
        float y = Mathf.Abs((seed / 10000) % 10000) * 0.001f;
        return new Vector2(x, y);
    }
}
