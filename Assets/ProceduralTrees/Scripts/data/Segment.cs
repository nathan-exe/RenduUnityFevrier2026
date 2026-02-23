using UnityEngine;

namespace NathanTazi
{
    /// <summary>
    /// un segment défini par deux points et un rayon.
    /// Ce struct est également présent dans le shader.
    /// </summary>
    public struct Segment
    {
        public const int Size = 8 * sizeof(float); 
    
        public Vector3 a,b;
        public float radius;
        public float age;
        public Segment(Vector3 a, Vector3 b, float radius,float age)
        {
            this.a = a;
            this.b = b;
            this.radius = radius;
            this.age = age;
        }
    
    }
}
