using System;
using System.Collections.Generic;
using UnityEngine;

namespace NathanTazi
{
    
    [Serializable]
    public class Ruleset : Dictionary<char,string>
    {
        [SerializeField] private List<string> rules;

        public Dictionary<char,string> Refresh()
        {
            Debug.Log("modified disctionnary.");
            
            Clear();
            foreach (var input in rules)
            {
                try
                {
                    char key = input[0];
                    string value = input.Split(" -> ")[1];
                    Add(key,value);
                    Debug.Log("key : "+key+", value : "+value);
                }catch(System.IndexOutOfRangeException){}
            }
            return this;
        }
    }
}