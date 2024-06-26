using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public static class AbilityUtilities
{
    static int max_weight { get { return GameInfo.MAX_WEIGHT; } }

    public static List<Ability> RandomSelect()
    {
        List<Ability> random_abis = new List<Ability>();

        // Reorder all abilities randomly
        List<Ability> random_order = AbilityDatabase.I.abilities.RandomizeOrder().ToList();

        // Set abilities from the beginning until the max_weight is reached
        int current_weight = 0;
        foreach (Ability abi in random_order)
        {
            current_weight += abi.Weight;
            if (current_weight > max_weight)
            {
                break;
            }
            random_abis.Add(abi);
        }

        return random_abis;
    }
}
