using UnityEngine;

namespace NathanTazi
{
    public struct BoundingBox
    {
        public Vector3 min;
        public Vector3 max;
        public Vector3 size => max - min;
        public Vector3 halfSize => size * .5f;
        public Vector3 center => .5f * (max+min);

        public BoundingBox(Vector3 min, Vector3 max)
        {
            this.min = min;
            this.max = max;
        }
    }
}