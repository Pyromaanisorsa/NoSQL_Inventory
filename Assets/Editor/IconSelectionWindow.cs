using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class IconSelectionWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private IconDatabase database;
    private GUIStyle buttonStyle;
    private System.Action<IconDataEntry> onIconSelected;

    public static void OpenWindow(System.Action<IconDataEntry> callback)
    {
        // Set window title and minSize
        IconSelectionWindow window = CreateInstance<IconSelectionWindow>();
        window.titleContent = new GUIContent("Icon Selection");
        window.minSize = new Vector2(500, 400);
        window.onIconSelected = callback;

        // Make window a Utility to keep it focused
        window.ShowUtility();
        //window.ShowModalUtility();
    }

    private void OnEnable()
    {
        // Load all ScriptableObjects from Resources/Items folder
        string path = "Assets/Databases/IconDatabase.asset";
        database = AssetDatabase.LoadAssetAtPath<IconDatabase>(path);

        // Create new button style
        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.border = new RectOffset(0, 0, 0, 0);
        buttonStyle.padding = new RectOffset(0, 0, 0, 0);
        buttonStyle.margin = new RectOffset(2, 0, 2, 0);

    }

    private void OnGUI()
    {
        if (database != null)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(position.height - 2));
            GUILayout.BeginVertical();

            // Iterate over all the items and display their data in rows
            for (int i = 0; i < database.iconList.Count;) 
            {
                EditorGUILayout.BeginHorizontal();
                for(int k = 0; k < 5; k++)
                {
                    if (GUILayout.Button(new GUIContent(AssetPreview.GetAssetPreview(database.iconList[i].icon.texture),
                        $"{database.iconList[i].icon.name}\nIconID: {database.iconList[i].iconID}"),
                        buttonStyle, GUILayout.Width(64), GUILayout.Height(64)))
                    {
                        onIconSelected?.Invoke(database.iconList[i]);
                        Close();
                    }
                    i++;
                    if (i >= database.iconList.Count)
                        break;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
        }
        else
            GUILayout.Label("No Icon Database found.");
        
    }

    private void PingItem(IconDataEntry iconData) 
    {
        
        onIconSelected.Invoke(iconData);
        Close();
    }
}