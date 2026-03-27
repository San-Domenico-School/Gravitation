using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WorldSaveData
{
    public string worldKey;
    public WorldType worldType;
    public string seedText;
    public SerializableVector3 playerPosition;
    public SerializableQuaternion playerRotation;
    public float playerHealth;
    public List<PersistentObjectSaveData> objects = new List<PersistentObjectSaveData>();
}

[Serializable]
public class PersistentObjectSaveData
{
    public string persistentId;
    public PersistentObjectKind kind;
    public bool active;
    public SerializableVector3 position;
    public SerializableQuaternion rotation;
}

[Serializable]
public struct SerializableVector3
{
    public float x;
    public float y;
    public float z;

    public SerializableVector3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public static SerializableVector3 From(Vector3 value)
    {
        return new SerializableVector3(value.x, value.y, value.z);
    }

    public Vector3 ToVector3()
    {
        return new Vector3(x, y, z);
    }
}

[Serializable]
public struct SerializableQuaternion
{
    public float x;
    public float y;
    public float z;
    public float w;

    public SerializableQuaternion(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    public static SerializableQuaternion From(Quaternion value)
    {
        return new SerializableQuaternion(value.x, value.y, value.z, value.w);
    }

    public Quaternion ToQuaternion()
    {
        return new Quaternion(x, y, z, w);
    }
}
