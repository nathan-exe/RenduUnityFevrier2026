using System;
using System.Collections.Generic;
using UnityEngine;
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
        [SerializeField] public float baseRadius;
        [SerializeField][Range(0,1)] public float growth;
        
        [Header("Algorithm")]
        [SerializeField] public int seed;
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
            public Vector3 point;
        
            //angles
            public float roll;
            public float yaw;
            public float pitch;
            // public Vector3 direction => 
            //     Quaternion.AngleAxis( yaw, Vector3.up)
            //     *Quaternion.AngleAxis( pitch, Vector3.right) 
            //     *Quaternion.AngleAxis( roll, Vector3.forward) 
            //     * Vector3.up;
            
            Matrix4x4 transform;
            // public Vector3 direction => 
            //     Quaternion.AngleAxis( roll, Vector3.forward) 
            //     *Quaternion.AngleAxis( pitch, Vector3.right) 
            //     *Quaternion.AngleAxis( yaw, Vector3.up)
            //     * Vector3.up;
            public Vector3 direction => transform * Vector3.up;
            public float radius;
        }
        public override PlantGraph GetGraph()
        {
            Random.InitState(seed);
            Turtle turtle = new Turtle();
            turtle.radius = baseRadius;
        
            Stack<Turtle> stack = new Stack<Turtle>();
        
            PlantGraph plantGraph = new();
            bool isNewSegment = false;

            Vector3 plantStart = Vector3.zero;
        
            int i = 0;
            int lastSymbolID = Symbols.Length;
            foreach (char symbol in Symbols)
            {
                float actualStepSize = (StepSize.x + Random.value * StepSize.y ) * (isNewSegment ? growth : 1);
                float age = isNewSegment ? growth : 1f;
                bool foundSymbol = true;
                switch (symbol)
                {
                    // 'f' : go forward
                    case 'f': {
                        if (!enableBranchReduction||(i == lastSymbolID || i==0 || PreviousNonParenthesisCharacter(Symbols,i)!='f'))
                            plantStart = turtle.point;
                        turtle.yaw += Random.Range(-angleRandomness,angleRandomness);
                        turtle.pitch += Random.Range(-angleRandomness,angleRandomness);
                        turtle.roll += Random.Range(-angleRandomness,angleRandomness);
                        turtle.tra
                        turtle.point += turtle.direction * actualStepSize ;
                        if (!enableBranchReduction||(i == lastSymbolID || (NextNonParenthesisCharacter(Symbols,i) != 'f' )))
                            //&& !(i < lastSymbolID-1 && (Symbols[i + 1] == '(' || Symbols[i + 1] == '(')&&Symbols[i + 2] == 'f' ) ))
                        {
                            Vector3 b = turtle.point;
                            plantGraph.segments.Add(new Segment(plantStart, b, turtle.radius,age));
                        }
                    
                        break;}
                
                    // 'x' : go forward and draw leaf
                    case 'x': {
                        Vector3 a = turtle.point;
                        //turtle.angle += Random.Range(-angleRandomness,angleRandomness);
                        turtle.point += turtle.direction * actualStepSize;
                        Vector3 b = turtle.point;
                        plantGraph.segments.Add(new Segment(a, b, turtle.radius,age));
                        plantGraph.leaves.Add(turtle.point);
                        break; }
                
                    // '-' : turn down
                    case '-' :
                        turtle.pitch -= PitchAngleRange.x + Random.Range(-PitchAngleRange.y,PitchAngleRange.y);
                        break;
                
                    // '+' : turn up
                    case '+' :
                        turtle.pitch += PitchAngleRange.x + Random.Range(-PitchAngleRange.y,PitchAngleRange.y);
                        break;
                
                    //'<' : turn left
                    case '<' :
                        turtle.yaw -= YawAngleRange.x + Random.Range(-YawAngleRange.y,YawAngleRange.y);
                        break;
                
                    //'>' : turn right
                    case '>' :
                        turtle.yaw += YawAngleRange.x + Random.Range(-YawAngleRange.y,YawAngleRange.y);
                        break;
                
                    // '.' : turn down
                    case '.' :
                        turtle.roll -= RollAngleRange.x + Random.Range(-RollAngleRange.y,RollAngleRange.y);
                        break;
                
                    // ':' : turn up
                    case ':' :
                        turtle.roll += RollAngleRange.x + Random.Range(-RollAngleRange.y,RollAngleRange.y);
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
                    if(symbol>'0' && symbol<'9')
                    {
                        int divider = symbol-'0';
                        turtle.radius /= divider;
                    }
                }

                i++;
            }
        
            return plantGraph;
        }
    }
}
