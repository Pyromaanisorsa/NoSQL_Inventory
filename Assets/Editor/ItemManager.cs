using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Reflection;
using System.Collections.Generic;
using System;

public class ItemManager : EditorWindow
{
    private ItemDatabase database;
    private ItemManagerSaveData itemIDSaveData;
    private IconDatabase iconDatabase;
    private Vector2 scrollPosition;
    private int selectedRowIndex = -1;
    private ItemData selectedItemData;
    private EditorWindow secondWindow;

    private ScrollView scrollView; //TESTING CREATEGUI

    [MenuItem("Window/UI Toolkit/ItemManager")]
    public static void ShowWindow()
    {
        GetWindow<ItemManager>("Item Manager");
    }

    private void OnEnable()
    {
        // Load all ScriptableObjects from Resources/Items folder
        string path = "Assets/Databases/ItemDatabase.asset";
        database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);

        // Get ItemManagerSaveData file
        path = "Assets/Databases/ItemManagerSave.asset";
        itemIDSaveData = AssetDatabase.LoadAssetAtPath<ItemManagerSaveData>(path);

        path = "Assets/Databases/IconDatabase.asset";
        iconDatabase = AssetDatabase.LoadAssetAtPath<IconDatabase>(path);
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal(EditorStyles.toolbar);

        DrawToolStrip();

        GUILayout.EndHorizontal();

        GUILayout.Label("Items in ItemDataBase", EditorStyles.boldLabel);

        // Display column headers
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("ID", GUILayout.Width(55));
        GUILayout.Label("Name", GUILayout.Width(150));
        GUILayout.Label("Description", GUILayout.Width(300));
        GUILayout.Label("Stackable", GUILayout.Width(70));
        GUILayout.Label("Max Stack", GUILayout.Width(70));
        GUILayout.Label("Weight", GUILayout.Width(70));
        GUILayout.Label("Icon", GUILayout.Width(40));
        EditorGUILayout.EndHorizontal();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, true, true, GUILayout.Width(position.width), GUILayout.Height(position.height - 64));

        GUILayout.BeginVertical();
        for(int i = 0; i < database.itemList.Count; i++) 
        {
            ItemDataEntry item = database.itemList[i];

            // Reserve space for the row
            Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.Height(40));

            // Determine the background style based on selection
            GUIStyle rowStyle = new GUIStyle(GUI.skin.box);
            rowStyle.normal.background = selectedRowIndex == i
                ? MakeTex(1, 1, new Color(0.3f, 0.5f, 1f, 0.3f)) // Highlight color
                : MakeTex(1, 1, new Color(0.8f, 0.8f, 0.8f, 0.2f)); // Default color

            // Draw the clickable row button
            if (GUI.Button(rowRect, GUIContent.none, rowStyle))
            {
                selectedItemData = item.itemData;
                selectedRowIndex = i;
                OnRowSelected(item);
            }

            // Render the labels and icon over the row button
            Rect labelRect = rowRect;
            labelRect.x += 5; // Padding from the left edge
            labelRect.width = 50;
            GUI.Label(labelRect, item.itemData.ItemID.ToString());

            labelRect.x += labelRect.width + 5; // Adjust for next column
            labelRect.width = 150;
            GUI.Label(labelRect, item.itemData.ItemName);

            labelRect.x += labelRect.width + 5;
            labelRect.width = 300;
            GUI.Label(labelRect, item.itemData.ItemDescription);

            labelRect.x += labelRect.width + 5;
            labelRect.width = 70;
            GUI.Label(labelRect, item.itemData.Stackable.ToString());

            labelRect.x += labelRect.width + 5;
            labelRect.width = 70;
            GUI.Label(labelRect, item.itemData.MaxStack.ToString());

            labelRect.x += labelRect.width + 5;
            labelRect.width = 70;
            GUI.Label(labelRect, item.itemData.Weight.ToString());

            labelRect.x += labelRect.width + 5;
            labelRect.width = 32;
            var icon = AssetPreview.GetAssetPreview(iconDatabase.iconList[item.itemData.IconID].icon.texture);
            GUI.DrawTexture(labelRect, icon, ScaleMode.ScaleToFit);
        }

        EditorGUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    // Action triggered on row selection
    private void OnRowSelected(ItemDataEntry selectedItem)
    {
        Debug.Log($"Selected Item: {selectedItem.itemData.ItemName} (ID: {selectedItem.itemData.ItemID})");
    }

    // Utility function to create a texture for the selected row highlight
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }   

    void DrawToolStrip()
    {
        if (GUILayout.Button("Create", EditorStyles.toolbarButton))
        {
            OnMenu_Create();
            GUIUtility.ExitGUI();
        }

        if(selectedRowIndex != -1) 
        {
            if(GUILayout.Button("Edit", EditorStyles.toolbarButton))
            {
                if(secondWindow == null)
                    secondWindow = EditItemWindow.OpenWindow(selectedItemData);
            }
            if (GUILayout.Button("Delete", EditorStyles.toolbarButton))
            {
                ItemData item = database.itemList[selectedRowIndex].itemData;
                bool confirmDelete = EditorUtility.DisplayDialog("Delete Item",
                    $"Are you sure you want to delete?\nItem Name: {item.ItemName} | ItemID: {item.ItemID}\n{AssetDatabase.GetAssetPath(item)}",
                    "Yes", "No");

                if (confirmDelete)
                    DeleteItem();
                else
                    Debug.Log("I CHANGED MY MIND....");
            }
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Tools", EditorStyles.toolbarDropDown))
        {
            GenericMenu toolsMenu = new GenericMenu();

            if (Selection.activeGameObject != null)
                toolsMenu.AddItem(new GUIContent("Optimize Selected"), false, Test_Ping);
            else
                toolsMenu.AddDisabledItem(new GUIContent("Optimize Selected"));

            toolsMenu.AddSeparator("");

            toolsMenu.AddItem(new GUIContent("Help"), false, OnTools_Help);
            toolsMenu.AddSeparator("");
            toolsMenu.AddItem(new GUIContent("Delete"), false, Test_Ping);

            // Offset menu from right of editor window
            toolsMenu.DropDown(new Rect(Screen.width - 216 - 40, 0, 0, 16));
            GUIUtility.ExitGUI();
        }
    }

    void DeleteItem() 
    {
        // Find the location of item about to be deleted in the iconDatabase
        int itemID = selectedItemData.ItemID;
        int dbID = 0;
        for (int i = 0; i < database.itemList.Count; i++) 
        {
            if (itemID == database.itemList[i].itemID) 
            {
                dbID = i;
                break;
            }
        }
        Debug.Log("ITEMDB Index: " + dbID);
        string path = AssetDatabase.GetAssetPath(selectedItemData);

        // If delete successful; update itemDatabase and add the droppedID to ItemManagerSave droppedID list
        if (AssetDatabase.DeleteAsset(path))
        {
            database.itemList.RemoveAt(dbID);
            itemIDSaveData.droppedID.Add(itemID);

            EditorUtility.SetDirty(database);
            EditorUtility.SetDirty(itemIDSaveData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            selectedItemData = null;
            selectedRowIndex = 1;
            Repaint();
        }
        else
            Debug.Log("FAILED TO DELETE FILE");
    }

    void Buying(string text) 
    {
        Debug.Log(text);
    }

    void Test_Ping() 
    {
        Debug.Log("PINGERS");
    }

    // Current test; create ItemData instance
    void OnMenu_Create()
    {
        if (secondWindow == null)
            secondWindow = CreateItemWindow.OpenWindow();
    }

    void OnTools_Help()
    {
        Help.BrowseURL("http://example.com/product/help");
    }
}
