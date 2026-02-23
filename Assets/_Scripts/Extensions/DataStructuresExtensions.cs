using System.Collections.Generic;
using UnityEngine;

public static class DataStructuresExtensions
{
    public static T PickRandom<T>(this List<T> list)
    {
        return list[ Random.Range(0, list.Count)];
    }
    
    public static T PickRandom<T>(this T[] array)
    {
        return array[Random.Range(0, array.Length)];
    }
    
    public static T1 GetKeyFromValue<T1, T2>(this Dictionary<T1, T2> dict, T2 value)
    {
        foreach (KeyValuePair<T1,T2> pair in dict)
        {
            if (pair.Value.Equals(value))
            {
                return pair.Key; // Retourne la cl�
            }
        }
        return default;
    }
}
