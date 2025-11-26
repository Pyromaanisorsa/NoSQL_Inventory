using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;

public class EditItemWindow : EditorWindow
{
    private Dictionary<FieldInfo, object> baseFieldValues = new Dictionary<FieldInfo, object>();
    private Dictionary<FieldInfo, object> subclassFieldValues = new Dictionary<FieldInfo, object>();
    private ItemData itemData;
    private IconDatabase iconDatabase;
    private Vector2 scrollPosition;
    private GUIStyle buttonStyle;

    public static EditorWindow OpenWindow(ItemData item)
    {
        // Set window title and minSize
        EditItemWindow window = CreateInstance<EditItemWindow>();
        window.titleContent = new GUIContent("Edit Item");
        window.minSize = new Vector2(475, 400);
        window.maxSize = new Vector2(475, 800);

        // Make window a Utility to keep it focused
        window.ShowUtility();
        window.itemData = item;
        window.InitializeBaseFields();
        window.InitializeSubFields();
        return window;
    }

    private void OnEnable()
    {
        // Load all ScriptableObjects from Resources/Items folder
        string path = "Assets/Databases/IconDatabase.asset";
        iconDatabase = AssetDatabase.LoadAssetAtPath<IconDatabase>(path);

        // Create new button style
        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.padding = new RectOffset(2, 2, 2, 2);
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
            object defaultValue = field.GetValue(itemData);
            baseFieldValues.Add(field, defaultValue);
        }
    }

    private void InitializeSubFields()
    {
        Type derivedType = itemData.GetType();
        List<FieldInfo> fieldsOrdered = new List<FieldInfo>();
        Stack<Type> classHierachy = new Stack<Type>();

        while (derivedType != typeof(ItemData) && derivedType != null)
        {
            Debug.Log("Current Type: " + derivedType);
            classHierachy.Push(derivedType);
            derivedType = derivedType.BaseType;
        }

        while (classHierachy.Count > 0)
        {
            Type currentType = classHierachy.Pop();
            FieldInfo[] fields = currentType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => f.Name != "itemID").ToArray();

            foreach (var field in fields)
            {
                object defaultValue = field.GetValue(itemData);
                subclassFieldValues.Add(field, defaultValue);
            }
        }
    }

    private void OnGUI()
    {
        // Filename field always necessary / visible + show next itemID for run
        EditorGUILayout.LabelField("General Data", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("File", AssetDatabase.GetAssetPath(itemData), EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Item Class", itemData.GetType().Name, EditorStyles.boldLabel);

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

        // Button to Edit Item file with proper spacing
        EditorGUILayout.Space(10);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Edit Item", GUILayout.Width(250), GUILayout.Height(50)))
        {
            EditItem();
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
            if (itemData.IconID != null)
            {
                int iconID = (int)value;
                EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(field.Name), GUILayout.Width(148));
                if (GUILayout.Button(AssetPreview.GetAssetPreview(iconDatabase.iconList[iconID].icon.texture), buttonStyle, GUILayout.Width(32), GUILayout.Height(32)))
                {
                    IconSelectionWindow.OpenWindow(selectedIcon =>
                    {
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

    private void EditItem()
    {
        // Set base field values
        foreach (var field in baseFieldValues.Keys)
            field.SetValue(itemData, baseFieldValues[field]);

        // Set subclass-specific field values
        foreach (var field in subclassFieldValues.Keys)
            field.SetValue(itemData, subclassFieldValues[field]);

        Debug.Log($"File edited: {AssetDatabase.GetAssetPath(itemData)}");
        Close();
    }
}
