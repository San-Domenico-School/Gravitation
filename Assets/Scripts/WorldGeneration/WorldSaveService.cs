using System.IO;
using UnityEngine;

public static class WorldSaveService
{
    private const string SaveFolderName = "GravitationWorlds";

    private static string SaveRoot => Path.Combine(Application.persistentDataPath, SaveFolderName);

    public static string GetSavePath(string worldKey)
    {
        string safeKey = MakeSafeFileName(worldKey);
        return Path.Combine(SaveRoot, safeKey + ".json");
    }

    public static bool TryLoad(string worldKey, out WorldSaveData data)
    {
        string path = GetSavePath(worldKey);
        if (!File.Exists(path))
        {
            data = null;
            return false;
        }

        string json = File.ReadAllText(path);
        data = JsonUtility.FromJson<WorldSaveData>(json);
        return data != null;
    }

    public static void Save(WorldSaveData data)
    {
        if (data == null || string.IsNullOrEmpty(data.worldKey))
            return;

        Directory.CreateDirectory(SaveRoot);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetSavePath(data.worldKey), json);
    }

    private static string MakeSafeFileName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "world";

        char[] chars = value.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '-' && chars[i] != '_')
                chars[i] = '_';
        }

        return new string(chars);
    }
}
