using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(menuName = "Ability/Create AbilityDatabase", fileName = "AbilityDatabase")]
public class AbilityDatabase : ScriptableObject
{
    static AbilityDatabase instance;
    public static AbilityDatabase I
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<AbilityDatabase>("AbilityDatabase");
                if (instance == null)
                {
                    Debug.LogError("AbilityDatabaseが見つかりませんでした");
                }
            }
            return instance;
        }
    }

    [ReorderableList] public List<Ability> abilities;
    public int ability_count { get { return abilities.Count; } }

    public Ability GetAbilityById(int id)
    {
        if (id < 0 || ability_count <= id)
        {
            return null;
        }
        return abilities[id];
    }

    ///<summary> return -1 when ability was not found. </summary>
    public int GetIdFromAbility(Ability ability)
    {
        return abilities.IndexOf(ability);
    }
}
