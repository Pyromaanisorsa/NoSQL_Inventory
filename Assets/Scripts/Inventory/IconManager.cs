using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IconManager : MonoBehaviour
{
    public static IconManager Instance { get; private set; }
    [SerializeField] private IconDatabase database;
    private Dictionary<int, Sprite> iconDictionary;

    // Setup IconDictionary before any other script can run start functions
    private void Awake()
    {
        // Create instance and ensure that THERE CAN ONLY BE ONE!
        if (Instance == null)
        {
            Instance = this;
            LoadIcons();
        }
        // Instance already exists, you fool!
        else
        {
            Debug.Log("There can only be one!");
            Destroy(gameObject);
        }
    }


    void LoadIcons()
    {
        iconDictionary = database.iconList.ToDictionary(icon => icon.iconID, icon => icon.icon);
    }

    // Get a sprite with with iconID
    public Sprite GetIconByID(int iconID) 
    {
        return iconDictionary[iconID];
    }
}