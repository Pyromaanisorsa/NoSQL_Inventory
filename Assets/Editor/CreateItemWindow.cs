using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

public class CreateItemWindow : EditorWindow
{
    private Type selectedType;
    private Dictionary<FieldInfo, object> baseFieldValues = new Dictionary<FieldInfo, object>();
    private Dictionary<FieldInfo, object> subclassFieldValues = new Dictionary<FieldInfo, object>();
    private ItemManagerSaveData itemIDSaveData;
    private ItemDatabase database;
    private Vector2 scrollPosition;
    private GUIStyle buttonStyle;

    private List<Type> itemTypes;
    private string[] itemTypeNames;
    private int selectedIndex;
    private string fileName;
    private IconDataEntry currentSelectedIcon;

    // Used to set custom defaultValues for fields based off types
    private static readonly Dictionary<Type, object> defaultValues = new Dictionary<Type, object> 
    {
        { typeof(int), 1 },
        { typeof(float), 1f }
    };

    public static EditorWindow OpenWindow()
    {
        // Set window title and minSize
        CreateItemWindow window = CreateInstance<CreateItemWindow>();
        window.titleContent = new GUIContent("Create Item");
        window.minSize = new Vector2(475, 400);
        window.maxSize = new Vector2(475, 800);

        // Make window a Utility to keep it focused
        window.ShowUtility();
        //window.ShowModalUtility();
        return window;
    }

    private void OnEnable()
    {
        // Get ItemDatabase file
        string path = "Assets/Databases/ItemDatabase.asset";
        database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(path);

        // Get ItemManagerSaveData file
        path = "Assets/Databases/ItemManagerSave.asset";
        itemIDSaveData = AssetDatabase.LoadAssetAtPath<ItemManagerSaveData>(path);

        // Populate Type-list of ItemData and it's subclasses
        itemTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ItemData)) || type == typeof(ItemData))
            .ToList();

        // Store class names to string array
        itemTypeNames = itemTypes.Select(type => type.Name).ToArray();

        // Default selection to ItemData
        selectedIndex = itemTypes.FindIndex(type => type == typeof(ItemData));

        // Backup just in case
        if (selectedIndex == -1) 
            selectedIndex = 0;

        // Create new button style
        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.padding = new RectOffset(2, 2, 2, 2);

        // Initialize ItemData variables
        InitializeBaseFields();
        SetSelectedType(itemTypes[selectedIndex]);
    }

    private void InitializeBaseFields()
    {
        // Clear list just in case
        baseFieldValues.Clear();

        // Collect all fields/variables from ItemData excluding itemID
        var baseFields = typeof(ItemData)
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null && f.Name != "itemID");

        // Add all fields to dictionary, that will be used to setup ItemData fields
        foreach (var field in baseFields)
        {
            Debug.Log(field);
            object defaultValue = defaultValues.ContainsKey(field.FieldType) ? defaultValues[field.FieldType] : field.FieldType.IsValueType ? Activator.CreateInstance(field.FieldType) : null;
            baseFieldValues.Add(field, defaultValue);
        }
    }

    private void OnGUI()
    {
        // Filename field always necessary / visible + show next itemID for run
        EditorGUILayout.LabelField("General Data", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Next ItemID", itemIDSaveData.GetNextItemID().ToString(), EditorStyles.boldLabel);
        fileName = EditorGUILayout.TextField("File Name", fileName, GUILayout.Width(400));

        // Dropdown to select an ItemData class/subclass
        int newIndex = EditorGUILayout.Popup("Item Type", selectedIndex, itemTypeNames, GUILayout.Width(350));

        // Check if value changed; no need to recreate fields if Type wasn't changed
        if (newIndex != selectedIndex)
        {
            selectedIndex = newIndex;
            SetSelectedType(itemTypes[selectedIndex]);
        }

        // Start scrolling
        EditorGUILayout.Space(10);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Display base ItemData fields
        EditorGUILayout.LabelField("Base ItemData Fields", EditorStyles.boldLabel);
        foreach (var field in baseFieldValues.Keys.ToList())
        {
            baseFieldValues[field] = DrawField(field, baseFieldValues[field]);
        }

        // Display subclass-specific fields if any
        if (subclassFieldValues.Count > 0)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Subclass-Specific Fields", EditorStyles.boldLabel);
            foreach (var field in subclassFieldValues.Keys.ToList())
            {
                subclassFieldValues[field] = DrawField(field, subclassFieldValues[field]);
            }
        }

        // End scrolling
        EditorGUILayout.EndScrollView();

        // Button to create Item file with proper spacing
        EditorGUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create", GUILayout.Width(250), GUILayout.Height(50)))
        {
            CreateItem();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        EditorGUILayout.Space(10);
    }

    private object DrawField(FieldInfo field, object value)
    {
        EditorGUILayout.BeginHorizontal();
        //EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(field.Name), GUILayout.Width(100));

        // Create right field for right type
        if (field.FieldType == typeof(int) && field.Name == "iconID")
        {
            if(currentSelectedIcon != null) 
            {
                EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(field.Name), GUILayout.Width(148));
                if (GUILayout.Button(AssetPreview.GetAssetPreview(currentSelectedIcon.icon.texture), buttonStyle, GUILayout.Width(32), GUILayout.Height(32)))
                {
                    IconSelectionWindow.OpenWindow(selectedIcon =>
                    {
                        currentSelectedIcon = selectedIcon;
                        baseFieldValues[field] = selectedIcon.iconID;
                        Repaint();
                    });
                }
            }
            else 
            {
                if (GUILayout.Button("Select Icon", GUILayout.Width(100), GUILayout.Height(32))) 
                {
                    IconSelectionWindow.OpenWindow(selectedIcon =>
                    {
                        currentSelectedIcon = selectedIcon;
                        baseFieldValues[field] = selectedIcon.iconID;
                        Repaint();
                    });
                }
            }
        }
        else if (field.FieldType == typeof(int))
        {
            value = EditorGUILayout.IntField(ObjectNames.NicifyVariableName(field.Name), (int)(value ?? 0), GUILayout.Width(200));
        }
        else if (field.FieldType == typeof(float))
        {
            value = EditorGUILayout.FloatField(ObjectNames.NicifyVariableName(field.Name), (float)(value ?? 0f), GUILayout.Width(200));
        }
        else if (field.FieldType == typeof(bool))
        {
            value = EditorGUILayout.Toggle(ObjectNames.NicifyVariableName(field.Name), (bool)(value ?? false));
        }
        else if (field.FieldType == typeof(string))
        {
            value = EditorGUILayout.TextField(ObjectNames.NicifyVariableName(field.Name), (string)(value ?? ""), GUILayout.Width(400));
        }
        else if (field.FieldType == typeof(Sprite))
        {
            value = (Sprite)EditorGUILayout.ObjectField(ObjectNames.NicifyVariableName(field.Name), (Sprite)value, typeof(Sprite), false);
        }
        else if (field.FieldType.IsEnum)
        {
            value = EditorGUILayout.EnumPopup(ObjectNames.NicifyVariableName(field.Name), (Enum)(value ?? Enum.GetValues(field.FieldType).GetValue(0)), GUILayout.Width(350));
        }
        else if (field.FieldType == typeof(GameObject))
        {
            value = EditorGUILayout.ObjectField(ObjectNames.NicifyVariableName(field.Name), (GameObject)value, typeof(GameObject), false, GUILayout.Width(400));
        }
        else
        {
            EditorGUILayout.LabelField($"Unsupported type: {field.FieldType}");
        }
        EditorGUILayout.EndHorizontal();
        return value;
    }

    private void SetSelectedType(Type type)
    {
        selectedType = type;
        
        // Clear subclass values on class type change
        subclassFieldValues.Clear();

        // Get fields specific to the selected type and its inheritance chain (excluding ItemData)
        var subclassFields = GetFieldsFromHierarchy(type)
            .Where(f => !baseFieldValues.ContainsKey(f));

        // Add all subclass & their non-ItemData inheritance fields to dictionary, that will be used to setup Subclass specific fields
        foreach (var field in subclassFields)
        {
            object defaultValue = field.FieldType.IsValueType ? Activator.CreateInstance(field.FieldType) : null;
            subclassFieldValues.Add(field, defaultValue);
        }

        // Recursively collect fields from the entire inheritance hierarchy of the type
        IEnumerable<FieldInfo> GetFieldsFromHierarchy(Type type)
        {
            var fields = new List<FieldInfo>();

            // Check all fields from subclass hierarchy till ItemData
            while (type != null && type != typeof(ItemData))
            {
                fields.AddRange(
                    type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(f => f.IsPublic || f.GetCustomAttribute<SerializeField>() != null)
                );

                // Move up the inheritance hierarchy
                type = type.BaseType;
            }
            // Return IEnumerable of FieldInfo objects.
            return fields;
        }
    }

    private void CreateItem()
    {
        // Null-check, just in case
        if (selectedType == null) 
            return;

        // Check if fileName is empty
        if (fileName == "" || fileName == null)
        {
            Debug.Log("FILENAME CAN'T BE EMPTY!");
            return;
        }

        // Create file save location
        string path = $"Assets/Resources/Items/{fileName}.asset";

        // Check if file already exists
        if (System.IO.File.Exists(path)) 
        {
            Debug.Log("FILE ALREADY EXISTS");
            return;
        }

        // Create an instance of the selected type
        var newItem = ScriptableObject.CreateInstance(selectedType);

        // Get ItemID field so we can set the right ID for new item
        FieldInfo itemID = typeof(ItemData).GetField("itemID", BindingFlags.NonPublic | BindingFlags.Instance);

        // Use dropped ID if there are any available; else use nextID
        int storedID; bool usedDroppedID = false;
        if (itemIDSaveData.droppedID.Count > 0)
        {
            Debug.Log("Used dropped ID");
            storedID = itemIDSaveData.droppedID[0];
            itemID.SetValue(newItem, storedID);
            usedDroppedID = true;
            itemIDSaveData.droppedID.RemoveAt(0);
        }
        else
        {
            Debug.Log("Used next ID");
            storedID = itemIDSaveData.nextID;
            itemID.SetValue(newItem, storedID);
            usedDroppedID = false;
            itemIDSaveData.nextID++;
        }

        // Set base field values
        foreach (var field in baseFieldValues.Keys)
            field.SetValue(newItem, baseFieldValues[field]);

        // Set subclass-specific field values
        foreach (var field in subclassFieldValues.Keys)
            field.SetValue(newItem, subclassFieldValues[field]);

        // Add new Item as ItemDataEntry in Itemdatabase and resort ItemList if droppedID was used
        database.itemList.Add(new ItemDataEntry(storedID, newItem as ItemData));
        if (usedDroppedID)
            database.itemList.Sort((a, b) => a.itemID.CompareTo(b.itemID));

        // Save the instance as asset/file
        EditorUtility.SetDirty(database);
        EditorUtility.SetDirty(itemIDSaveData);
        AssetDatabase.CreateAsset(newItem, path);
        AssetDatabase.SaveAssets();

        AssetDatabase.Refresh();
        Debug.Log($"File created at: {path}");
        Close();
    }
}