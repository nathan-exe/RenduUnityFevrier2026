using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NathanTazi
{
    
    /// <summary>
    /// https://en.wikipedia.org/wiki/L-system
    /// génère de manière récursive des structures auto-similaires
    /// avec un alphabet de symboles et des règles de réécriture.
    /// </summary>
    public abstract class Lsystem
    {
        public string Symbols { get; set; }

        [SerializeField] public Ruleset _rules;

        [Serializable]
        public class RandomValueSet
        {
            public float r0;
            [HideInInspector] public float r1, r2, r3, r4;

            public RandomValueSet()
            {
                r0 = Random.value;
                r1 = Random.value;
                r2 = Random.value;
                r3 = Random.value;
                r4 = Random.value;
            } 
        }
        
        protected List<RandomValueSet> RandomValues = new List<RandomValueSet>();
        public int seed;
         
        private void ApplyRules()
        {
            string newSymbols = "";
            foreach (char symbol in Symbols)
            {
                if (_rules.TryGetValue(symbol, out var ruleResult))
                {
                    if (ruleResult == "/") continue;
                    
                    //write rules result into symbols string
                    newSymbols+=ruleResult;
                }
                else
                {
                    newSymbols+=symbol;
                }
            }
            Symbols = newSymbols;
        }

        protected abstract bool SymbolUsesRandomValues(char s);

        public string Simulate(int iterations,bool resetRandomValues = true)
        {

            if (resetRandomValues)
            {
                if(RandomValues == null)
                    RandomValues = new();
                RandomValues.Clear();
                Random.InitState(seed);
                foreach (char symbol in Symbols)
                {
                    RandomValues.Add(new());
                }
            }
            
            //Debug.Log("== Lsystem simulation ==");
            //Debug.Log("  axiom :  "+Symbols);
            for (int i = 0; i < iterations; i++)
            {
                ApplyRules();
                
                //generate random data for newly added nodes
                int indexInRandomArray = 0;
                bool isNewSymbol = false;
                foreach (char s in Symbols)
                {
                    isNewSymbol = s switch { '(' => true, ')' => false, _ => isNewSymbol }; 
                    if(SymbolUsesRandomValues(s))
                    {
                        if(isNewSymbol)
                            RandomValues.Insert(indexInRandomArray,new());
                        indexInRandomArray++;
                    }
                }
                
                //Debug.Log("  i : "+i.ToString() + ", symbols : "+Symbols);
            }
            return Symbols;
        }

        public abstract PlantGraph ComputeGraph();

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
