using System;
using System.Collections.Generic;
using _scripts.Extensions;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;


namespace NathanTazi
{
    
    /// <summary>
    /// Un Lsystem permettant de générer des arbres en 3D avec les symboles suivant :
    /// f -> avancer
    /// x -> avancer et placer une feuille
    /// -+ -> tourner (pitch)
    /// <> -> tourner (yaw)
    /// .: -> tourner (roll)
    /// [] -> empiler, dépiler l'état de la tortue
    /// () -> déclarer le début ou la fin d'une nouvelle itération
    /// / -> enlever un symbole
    /// 1,2,3,4... -> pour diviser le rayon de la branche
    /// u -> pour faire monter la branche
    /// </summary>
    [Serializable]
    public class Lsystem3D : Lsystem
    {
        [Header("Shape")] 
        [SerializeField] public Vector2 StepSize;
        [SerializeField] public float angleRandomness;
        [SerializeField] public Vector2 YawAngleRange;
        [SerializeField] public Vector2 PitchAngleRange;
        [SerializeField] public Vector2 RollAngleRange;
        [SerializeField] [Range(0,1)] public float verticalAngleBiasStrength;
        [SerializeField] public float baseRadius;
        [SerializeField][Range(0,1)][FormerlySerializedAs("growth")] public float growthThisStep;
        public float totalGrowth;
        
        [Header("Algorithm")]
        [SerializeField] bool enableBranchReduction;
        
        //constructors
        public Lsystem3D(string axiom) : base(axiom)
        {
        }
        public Lsystem3D(string axiom, Ruleset rules) : base(axiom, rules)
        {
        }
        
        //string helpers
        char NextNonParenthesisCharacter(string s, int i)
        {
            for (int j = i+1; j < s.Length; j++)
            {
                if(s[j] == ')' || s[j] == '(') continue;
                else return s[j];
            }   
            return s[i];
        }
        char PreviousNonParenthesisCharacter(string s, int i)
        {
            for (int j = i-1; j > 0; j--)
            {
                if(s[j] == ')' || s[j] == '(') continue;
                else return s[j];
            }   
            return s[i];
        }
    
        //comme en python, mais en 3D
        private struct Turtle
        {
            public Vector3 point
            {
                get { return _point;}
                set
                {
                    traveledDistance += Vector3.Distance(_point, value);
                    _point = value;
                }
            }
            private Vector3 _point;

            public Matrix4x4 transform ;
            public float traveledDistance;
            public Turtle(float baseRadius) : this()
            {
                this.currentRadius = baseRadius;
                this.oldRadius = baseRadius;
            }

            public Vector3 direction => (transform * Vector3.up);
            
            public float currentRadius;
            public float oldRadius;
        }

        protected override bool SymbolUsesRandomValues(char s)
        {
            return s is 'f' or 'x' or '-' or '+' or ':' or '.' or '<' or '>';
        }

        /// <summary>
        /// computes a local space graph structure from the simulated set of symbols. 
        /// </summary>
        /// <returns></returns>
        public override PlantGraph ComputeGraph()
        {
            int randomIndex = 0;
            
            Turtle turtle = new Turtle(baseRadius);
            turtle.transform = Matrix4x4.identity;
        
            Stack<Turtle> stack = new Stack<Turtle>();
            PlantGraph plantGraph = new();

            Vector3 plantStart = Vector3.zero;
            bool isNewSegment = false;
        
            int i = 0;
            int lastSymbolID = Symbols.Length;
            RandomValueSet random = new();
            
            foreach (char symbol in Symbols)
            {
                if (SymbolUsesRandomValues(symbol))
                    random = RandomValues[randomIndex++];
                
                bool foundSymbol = true;
                //float symbolStrength = totalGrowth;//isNewSegment ? growthThisStep : 1;
                float symbolStrength = isNewSegment ? growthThisStep : 1;
                switch (symbol)
                {
                    // 'f' : go forward
                    case 'f': {
                        if (!enableBranchReduction||(i == lastSymbolID || i==0 || PreviousNonParenthesisCharacter(Symbols,i)!='f'))
                            plantStart = turtle.point;

                        turtle.transform = turtle.transform * Matrix4x4.Rotate(
                        Quaternion.Euler(new Vector3(
                            random.r0.RemapRange(-angleRandomness, angleRandomness),
                            random.r1.RemapRange(-angleRandomness, angleRandomness),
                            random.r2.RemapRange(-angleRandomness, angleRandomness)
                            ) * symbolStrength)
                        );
                        float actualStepSize = (StepSize.x + random.r3 * StepSize.y ) * symbolStrength;
                        
                        turtle.point += turtle.direction * actualStepSize ;
                        if (!enableBranchReduction||(i == lastSymbolID || (NextNonParenthesisCharacter(Symbols,i) != 'f' )))
                            //&& !(i < lastSymbolID-1 && (Symbols[i + 1] == '(' || Symbols[i + 1] == '(')&&Symbols[i + 2] == 'f' ) ))
                        {
                            Vector3 b = turtle.point;
                            plantGraph.segments.Add(new Segment(plantStart, b, turtle.oldRadius,turtle.currentRadius,turtle.traveledDistance));
                            turtle.oldRadius = turtle.currentRadius;
                        }
                    
                        break;}
                
                    // 'x' : go forward and draw leaf
                    case 'x': {
                        if (!enableBranchReduction||(i == lastSymbolID || i==0 || PreviousNonParenthesisCharacter(Symbols,i)!='f'))
                            plantStart = turtle.point;

                        
                        turtle.transform = turtle.transform * Matrix4x4.Rotate(
                            Quaternion.Euler(new Vector3(
                                random.r0.RemapRange(-angleRandomness, angleRandomness),
                                random.r1.RemapRange(-angleRandomness, angleRandomness),
                                random.r2.RemapRange(-angleRandomness, angleRandomness)
                            ) * symbolStrength)
                        );
                        float actualStepSize = (StepSize.x + random.r3 * StepSize.y ) * symbolStrength;
                        
                        turtle.point += turtle.direction * actualStepSize ;
                        if (!enableBranchReduction||(i == lastSymbolID || (NextNonParenthesisCharacter(Symbols,i) != 'f' )))
                            //&& !(i < lastSymbolID-1 && (Symbols[i + 1] == '(' || Symbols[i + 1] == '(')&&Symbols[i + 2] == 'f' ) ))
                        {
                            Vector3 b = turtle.point;
                            plantGraph.segments.Add(new Segment(plantStart, b, turtle.oldRadius,turtle.currentRadius,turtle.traveledDistance));
                            turtle.oldRadius = turtle.currentRadius;
                        }
                        
                        plantGraph.leaves.Add(new(
                            turtle.point,
                            turtle.transform,
                            turtle.currentRadius*symbolStrength));
                        
                        break; }
                
                    // '-' : turn down
                    case '-' :
                        turtle.transform = turtle.transform * Matrix4x4.Rotate( Quaternion.Euler(
                            new Vector3(-(PitchAngleRange.x + random.r0.RemapRange(-PitchAngleRange.y,PitchAngleRange.y)),0,0)
                            * symbolStrength));
                        break;
                
                    // '+' : turn up
                    case '+' :
                        turtle.transform = turtle.transform * Matrix4x4.Rotate(
                            Quaternion.Euler(
                                new Vector3(PitchAngleRange.x + random.r0.RemapRange(-PitchAngleRange.y,PitchAngleRange.y),0,0)
                                * symbolStrength));
                        break;
                
                    //'<' : turn left
                    case '<' :
                        turtle.transform = turtle.transform * Matrix4x4.Rotate( 
                            Quaternion.Euler(
                                new Vector3(0, -(YawAngleRange.x + random.r0.RemapRange(-YawAngleRange.y,YawAngleRange.y)),0)
                                * symbolStrength));
                        break;
                
                    //'>' : turn right
                    case '>' :
                        turtle.transform = turtle.transform * Matrix4x4.Rotate(
                            Quaternion.Euler(
                                new Vector3(0, YawAngleRange.x + random.r0.RemapRange(-YawAngleRange.y,YawAngleRange.y),0)
                                * symbolStrength));
                        break;
                
                    // '.' : turn down
                    case '.' :
                        turtle.transform = turtle.transform * Matrix4x4.Rotate(
                           Quaternion.Euler(
                               new Vector3(0, 0, -(RollAngleRange.x + random.r0.RemapRange(-RollAngleRange.y,RollAngleRange.y)))
                               * symbolStrength));
                        break;
                
                    // ':' : turn up
                    case ':' :
                        turtle.transform = turtle.transform * Matrix4x4.Rotate( 
                           Quaternion.Euler(
                               new Vector3(0, 0, RollAngleRange.x + random.r0.RemapRange(-RollAngleRange.y,RollAngleRange.y))
                               * symbolStrength));
                        break;
                    
                    case 'u' :
                        turtle.transform = Matrix4x4.Rotate( 
                            Quaternion.Slerp(
                                Quaternion.LookRotation(turtle.transform*Vector3.forward,turtle.direction),
                                Quaternion.LookRotation(Vector3.ProjectOnPlane( turtle.transform*Vector3.forward,Vector3.up).normalized,Vector3.up),
                                verticalAngleBiasStrength * symbolStrength)
                            );
                        break;
                
                    // '[' : push state
                    case '[' : 
                        stack.Push(turtle);
                        break;
                
                    // ']' : pop state
                    case ']' :
                        turtle = stack.Pop();
                        break;
                
                    case '(' :
                        isNewSegment = true;
                        break;
                
                    case ')' :
                        isNewSegment = false;
                        break;
                    default:
                        foundSymbol = false;
                        break;
                }

                if (!foundSymbol)
                {
                    //division du rayon de la branche
                    if(symbol>'0' && symbol<='9')
                    {
                        int number = symbol-'0';
                        turtle.currentRadius *= (1.0f - 1.0f/number);
                    }
                    else if(symbol>='a' && symbol<='f')
                    {
                        int number = symbol-'a'+ 10;
                        turtle.currentRadius *= (1.0f - 1.0f/number);
                    }
                }
                
                i++;
            }
        
            return plantGraph;
        }
    }
}
