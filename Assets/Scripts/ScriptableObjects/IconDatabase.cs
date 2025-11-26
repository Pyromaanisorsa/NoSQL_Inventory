using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IconDatabase", menuName = "Database/IconDatabase")]
public class IconDatabase : ScriptableObject
{
    public List<IconDataEntry> iconList = new List<IconDataEntry>();
}

[System.Serializable]
public class IconDataEntry
{
    public int iconID;
    public Sprite icon;

    public IconDataEntry(int id, Sprite sprite) 
    {
        iconID = id;
        icon = sprite;
    }
}

[System.Serializable]
public class IconExportEntry
{
    public int iconID;
    public string iconPath;

    public IconExportEntry(int id, string path)
    {
        iconID = id;
        iconPath = path;
    }
}

[System.Serializable]
public class IconDatabaseExport
{
    public List<IconExportEntry> icons;

    public IconDatabaseExport(List<IconExportEntry> icons)
    {
        this.icons = icons;
    }
}
