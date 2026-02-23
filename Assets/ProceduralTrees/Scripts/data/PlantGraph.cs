using System;
using System.Collections.Generic;
using UnityEngine;

namespace NathanTazi
{
    public class PlantGraph
    {
        public List<Segment> segments = new();
        public List<Leaf> leaves = new();

        public struct Leaf
        {
            public Vector3 position;
            public Matrix4x4 branchTransform;
            public float size;

            public Leaf(Vector3 position, Matrix4x4 branchTransform, float size)
            {
                this.position = position;
                this.branchTransform = branchTransform;
                this.size = size;
            }
        }
        
        public Tuple<Vector3, Vector3> GetBoundingBox(float margin=3f)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            foreach (Segment seg in segments)
            {
                min = Vector3.Min(min, Vector3.Min(seg.a,seg.b));
                max = Vector3.Max(max, Vector3.Max(seg.a,seg.b));
            }
            return new Tuple<Vector3, Vector3>(min-Vector3.one*margin, max+Vector3.one*margin);
        }
    }
}
