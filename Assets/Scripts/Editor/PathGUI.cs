using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class PathGUI
{
    public static void OpenFileField(string label, ref string path)
    {
        string directory = path;
        PathField(label, ref path, () => EditorUtility.OpenFilePanel(label, directory, null));
    }

    public static void OpenFolderField(string label, ref string path)
    {
        string folder = path;
        PathField(label, ref path, () => EditorUtility.OpenFolderPanel(label, folder, null));
    }

    public static void SaveFileField(string label, ref string path)
    {
        string directory;
        string name;
        try
        {
            directory = Path.GetDirectoryName(path);
            name = Path.GetFileName(path);
        }
        catch (ArgumentException)
        {
            directory = path;
            name = null;
        }
        PathField(label, ref path, () => EditorUtility.SaveFilePanel(label, directory, name, null));
    }

    public static void SaveFolderField(string label, ref string path)
    {
        string folder = path;
        PathField(label, ref path, () => EditorUtility.SaveFolderPanel(label, folder, null));
    }

    private static void PathField(string label, ref string path, Func<string> browse)
    {
        GUILayout.BeginHorizontal();

        GUILayout.Label(label, EditorStyles.label, GUILayout.Width(EditorGUIUtility.labelWidth - 1),
            GUILayout.Height(EditorGUIUtility.singleLineHeight));

        //fix text field width
        Rect rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.textField);
        rect.width = EditorGUIUtility.currentViewWidth - EditorGUIUtility.labelWidth - 80;
        rect.height = EditorGUIUtility.singleLineHeight;
        rect.x += 1;
        rect.y += 1;
        path = EditorGUI.TextField(rect, path);
        

        if (GUILayout.Button("Browse...", GUILayout.ExpandWidth(false),
            GUILayout.Height(EditorGUIUtility.singleLineHeight)))
        {
            string newPath = browse();
            if (!string.IsNullOrEmpty(newPath))
                path = newPath;
        }

        GUILayout.EndHorizontal();
    }
}