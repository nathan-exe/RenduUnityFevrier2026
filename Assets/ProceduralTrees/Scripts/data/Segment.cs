using UnityEngine;

namespace NathanTazi
{
    /// <summary>
    /// un segment défini par deux points et un rayon.
    /// Ce struct est également présent dans le shader.
    /// </summary>
    public struct Segment
    {
        public const int Size = 9 * sizeof(float); 
    
        public Vector3 a,b;
        public float radiusA, radiusB;
        public float age;
        public Segment(Vector3 a, Vector3 b, float radiusA,float radiusB,float age)
        {
            this.a = a;
            this.b = b;
            this.radiusA = radiusA;
            this.radiusB = radiusB;
            this.age = age;
        }
    
    }
}
