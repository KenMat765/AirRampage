using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(menuName = "Help/Create HelpDatabase", fileName = "HelpDatabase")]
public class HelpDatabase : ScriptableObject
{
    static HelpDatabase instance;
    public static HelpDatabase I
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<HelpDatabase>("HelpDatabase");
                if (instance == null)
                {
                    Debug.LogError("HelpDatabaseが見つかりませんでした");
                }
            }
            return instance;
        }
    }

    [ReorderableList] public List<HelpData> helps;
    public int help_count { get { return helps.Count; } }

    public HelpData GetHelpDataById(int id)
    {
        if (id < 0 || help_count <= id)
        {
            return null;
        }
        return helps[id];
    }
}
