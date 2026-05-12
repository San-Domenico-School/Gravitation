#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for SaveManager.
/// Adds a clearly-visible "Clear Save File" button so you can wipe save.json
/// and reset in-memory state without entering a hotkey or hunting for the file.
/// </summary>
[CustomEditor(typeof(SaveManager))]
public class SaveManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw all the normal fields first
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Debug Tools", EditorStyles.boldLabel);

        var manager = (SaveManager)target;
        bool hasSave = manager.HasSaveFile;

        // Show where the file lives so it's easy to find manually too
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.TextField("Save Path", manager.SaveFilePath ?? "(not initialised — enter Play mode first)");
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(4);

        // Tint the button red to make it obvious it's destructive
        var prev = GUI.backgroundColor;
        GUI.backgroundColor = hasSave ? new Color(1f, 0.35f, 0.35f) : Color.grey;

        EditorGUI.BeginDisabledGroup(!hasSave);
        if (GUILayout.Button(hasSave ? "🗑  Clear Save File (fresh start)" : "No save file found", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                    "Clear Save File",
                    $"This will permanently delete:\n{manager.SaveFilePath}\n\nAnd reset all in-memory save data. Are you sure?",
                    "Yes, delete it",
                    "Cancel"))
            {
                manager.ClearSaveAndReset();
            }
        }
        EditorGUI.EndDisabledGroup();

        GUI.backgroundColor = prev;
    }
}
#endif
