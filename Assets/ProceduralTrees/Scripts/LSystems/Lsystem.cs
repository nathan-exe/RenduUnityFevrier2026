using System;
using UnityEngine;

namespace NathanTazi
{
    
    /// <summary>
    /// https://en.wikipedia.org/wiki/L-system
    /// génère de manière récursive des structures auto-similaires
    /// avec un alphabet de symboles et des règles de réécriture.
    /// </summary>
    [Serializable]
    public abstract class Lsystem
    {
        public string Symbols { get; set; }

        [SerializeField] public Ruleset _rules;
    
        private void ApplyRules()
        {
            string newSymbols = "";
            foreach (char symbol in Symbols)
            {
                if (_rules.TryGetValue(symbol, out var ruleResult))
                {
                    if(ruleResult!="/") newSymbols+=ruleResult;
                }
                else
                {
                    newSymbols+=symbol;
                }
            }
            Symbols = newSymbols;
        }

        public string Simulate(int iterations)
        {
            Debug.Log("== Lsystem simulation ==");
            Debug.Log("  axiom :  "+Symbols);
            for (int i = 0; i < iterations; i++)
            {
                ApplyRules();
                Debug.Log("  i : "+i.ToString() + ", symbols : "+Symbols);
            }
            return Symbols;
        }

        public abstract PlantGraph GetGraph();

        //constructors
        public Lsystem(string axiom)
        {
            Symbols = axiom;
        }
    
        public Lsystem(string axiom,Ruleset rules)
        {
            Symbols = axiom;
            _rules = rules;
        }
    
    }
}
