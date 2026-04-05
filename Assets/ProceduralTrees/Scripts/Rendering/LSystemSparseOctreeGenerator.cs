using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NathanTazi;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


namespace NathanTazi
{
    using Node = NathanTazi.SparseOctree<NathanTazi.Segment>.Node;
    using Random = UnityEngine.Random;
    using Debug = UnityEngine.Debug;
    
    public class LSystemSparseOctreeGenerator : MonoBehaviour
    {

        [Header("Scene References")]
        [SerializeField]
        private LSystemGenerator _generator;

        [Header("Parameters")]
        [SerializeField]
        public int MaxSubdivisionLevels = 5;

        [SerializeField]
        public int _maxSegmentsPerLeaf = 1;

        public SparseOctree<Segment> octree = new();

        public void OnLSystemRegenerated()
        {
            UpdateOctree();
        }
        
        [ContextMenu("UpdateOctree")]
        void UpdateOctree()
        {
            UnityEngine.Debug.Log("update octree");
            octree.Clear();
            octree.data = _generator.Graph.segments;
            octree.nodes.Add(new());

            List<ushort> indices = new(octree.data.Count);
            for (ushort i = 0; i < octree.data.Count; i++)
                indices.Add(i);
            SplitNodeIntoSubtrees(0, indices, _generator.BoundingBox.Item1, _generator.BoundingBox.Item2);
        }

        void DrawOctreeNode(int nodeIndex, Vector3 bbMin, Vector3 bbMax, int recIndex = 0)
        {
            //print("== "+recIndex);
            Vector3 bbHalfSize = (bbMax - bbMin) * .5f;
            for (ushort i = 0; i < 8; i++)
            {
                if (octree.nodes[nodeIndex][i] != Node.NULL_INDEX_VALUE)
                {
                    //compute child bounding box
                    Vector3 subTreeBbMin = i switch
                    {
                        0 => bbMin,
                        1 => bbMin + new Vector3(0, 0, bbHalfSize.z),
                        2 => bbMin + new Vector3(0, bbHalfSize.y, 0),
                        3 => bbMin + new Vector3(0, bbHalfSize.y, bbHalfSize.z),
                        4 => bbMin + new Vector3(bbHalfSize.x, 0, 0),
                        5 => bbMin + new Vector3(bbHalfSize.x, 0, bbHalfSize.z),
                        6 => bbMin + new Vector3(bbHalfSize.x, bbHalfSize.y, 0),
                        7 => bbMin + new Vector3(bbHalfSize.x, bbHalfSize.y, bbHalfSize.z),
                    };
                    Vector3 subTreeBbMax = subTreeBbMin + bbHalfSize;
                    Vector3 subTreeBbHalfSize = (subTreeBbMax - subTreeBbMin) * .5f;

                    bool pointsToSubtree = Node.ChildIndexValuePointsToSubTree(octree.nodes[nodeIndex][i]);

                    if (pointsToSubtree)
                        Gizmos.color = new Color(1, 1, 1,  (float)recIndex / MaxSubdivisionLevels/MaxSubdivisionLevels);
                    else
                    {
                        int j;
                        int firstBranchIndex = Node.GetArrayIndexForChildValue(octree.nodes[nodeIndex][i]);
                        
                        for (j = firstBranchIndex; octree.indexBuffer[j] != Node.NULL_INDEX_VALUE && i < octree.indexBuffer.Count; j++) ;
                        
                        int branchCount = j - firstBranchIndex; //-1 ?
                        //print("fisrt branch index of leaf : " + firstBranchIndex);
                        Gizmos.color = Color.Lerp(Color.green, Color.red,
                            (float)branchCount / _maxSegmentsPerLeaf);
                        Gizmos.color = Gizmos.color.WithAlpha((float)recIndex / MaxSubdivisionLevels/MaxSubdivisionLevels);

                    }
                    
                    //draw bb
                    Gizmos.DrawWireCube(
                        transform.TransformPoint(subTreeBbMin + subTreeBbHalfSize),
                        transform.TransformVector( subTreeBbHalfSize * 2)-Vector3.one*.01f);

                    //recursively try to draw subtrees
                    //print("child "+i+" value : "+octree.nodes[nodeIndex][i]);
                    if (pointsToSubtree)
                    {
                        //print("subtree found !!!!");
                        ushort childIndex = Node.GetArrayIndexForChildValue(octree.nodes[nodeIndex][i]);
                        DrawOctreeNode(childIndex, subTreeBbMin, subTreeBbMax, recIndex + 1);
                    }

                }
            }
        }

        void SplitNodeIntoSubtrees(int nodeIndex, List<ushort> segmentIndices, Vector3 bbMin, Vector3 bbMax,
            int currentRecursiveDepth = 0)
        {
            Vector3 bbHalfSize = (bbMax - bbMin) * .5f;

            //== try to recursively split current node into at most 8 subtrees ==

            //for each child of the current node
            for (ushort i = 0; i < 8; i++)
            {
                //compute child bounding box
                Vector3 subTreeBbMin = i switch
                {
                    0 => bbMin,
                    1 => bbMin + new Vector3(0, 0, bbHalfSize.z),
                    2 => bbMin + new Vector3(0, bbHalfSize.y, 0),
                    3 => bbMin + new Vector3(0, bbHalfSize.y, bbHalfSize.z),
                    4 => bbMin + new Vector3(bbHalfSize.x, 0, 0),
                    5 => bbMin + new Vector3(bbHalfSize.x, 0, bbHalfSize.z),
                    6 => bbMin + new Vector3(bbHalfSize.x, bbHalfSize.y, 0),
                    7 => bbMin + new Vector3(bbHalfSize.x, bbHalfSize.y, bbHalfSize.z)
                };
                Vector3 subTreeBbMax = subTreeBbMin + bbHalfSize;

                //find the number of segments it overlaps
                List<ushort> subtreeIndices =
                    FindBBOverlappingSegmentIndices(segmentIndices, ref octree, subTreeBbMin, subTreeBbMax);
                //print("subtree "+i+" indices count : " + subtreeIndices.Count+" at recursive depth : "+currentRecursiveDepth);
                if (subtreeIndices.Count > 0)
                {
                    //if the child node has reached the max subdivision level or only contains a small number of segments
                    if (currentRecursiveDepth >= MaxSubdivisionLevels || subtreeIndices.Count <= _maxSegmentsPerLeaf)
                    {
                        //print("ahhhhh bouuuhh thatcher");
                        //make it a leaf that points to a segment of data indices in the index buffer :

                        //populate index buffer and add terminaison value
                        ushort indexSegmentStart = (ushort)octree.indexBuffer.Count;
                        foreach (ushort index in subtreeIndices)
                            octree.indexBuffer.Add(index);
                        octree.indexBuffer.Add(Node.NULL_INDEX_VALUE);

                        //set child value to the segment's start index

                        //print("node index : "+nodeIndex+". node count : "+octree.nodes.Count);
                        octree.nodes[nodeIndex][i] = (ushort)(indexSegmentStart << 1);
                    }
                    else
                    {
                        //else, make it a subtree 
                        octree.nodes.Add(new());
                        int childIndex = octree.nodes.Count - 1;
                        octree.nodes[nodeIndex][i] = (ushort)((ushort)(childIndex << 1) + 1);

                        //and recursively split it again
                        //print("ici child index : "+childIndex+". octree nodes count : "+octree.nodes.Count );
                        SplitNodeIntoSubtrees(childIndex, subtreeIndices
                            , subTreeBbMin, subTreeBbMax,
                            currentRecursiveDepth + 1);
                    }
                }
                else
                {
                    //add null subtree
                    octree.nodes.Add(new());
                    octree.nodes[nodeIndex][i] = Node.NULL_INDEX_VALUE;
                }

            }
        }

        // bool SegmentOverlapsBox(Segment seg, Vector3 bbMin, Vector3 bbMax)
        // {
        //     bbMin -= Vector3.one * seg.radius;
        //     bbMax += Vector3.one * seg.radius;
        //     
        //     Vector3 direction = seg.b-seg.a;
        //     
        //     Vector3 T_1= Vector3.zero, T_2 = Vector3.zero; // vectors to hold the T-values for every direction
        //     float t_near = -float.MaxValue; 
        //     float t_far = float.MaxValue;
        //  
        //     UnityEngine.Debug.DrawLine(seg.a,seg.b,Color.red,5);
        //     
        //     for (int i = 0; i < 3; i++)
        //     { //we test slabs in every direction
        //         if (direction[i] == 0)
        //         { // ray parallel to planes in this direction
        //             if ((seg.a[i] < bbMin[i]) || (seg.a[i] > bbMax[i]))
        //             {
        //                 print("a");
        //                 return false; // parallel AND outside box : no intersection possible
        //             }
        //         }
        //         else
        //         { // ray not parallel to planes in this direction
        //             T_1[i] = (bbMin[i] - seg.a[i]) / direction[i];
        //             T_2[i] = (bbMin[i] - seg.a[i]) / direction[i];
        //
        //             if(T_1[i] > T_2[i]){ // we want T_1 to hold values for intersection with near plane
        //                 (T_1[i], T_2[i]) = (T_2[i], T_1[i]);
        //             }
        //             if (T_1[i] > t_near){
        //                 t_near = T_1[i];
        //             }
        //             if (T_2[i] < t_far){
        //                 t_far = T_2[i];
        //             }
        //             if( (t_near > t_far) || t_near<0){
        //                 print("b");
        //                 return false;
        //             }
        //         }
        //     }
        //     
        //     print("c");
        //     return true; // if we made it here, there was an intersection - YAY
        //
        // }

        static bool SegmentOverlapsBox(Segment seg, Vector3 bbMin, Vector3 bbMax)
        {
            const float EPSILON = .001f;

            bbMin -= Vector3.one * seg.radius;
            bbMax += Vector3.one * seg.radius;

            // Compute box center-point and half-length extents
            Vector3 c = (bbMin + bbMax) * 0.5f; // Box center
            Vector3 e = bbMax - c; // Box half-extent

            // Segment midpoint and halflength vector
            Vector3 m = (seg.a + seg.b) * 0.5f; // Segment midpoint
            Vector3 d = seg.b - m; // Segment halflength vector
            m = m - c; // Translate box and segment to the origin

            // Test world coordinate axes as separating axes
            float adx = Mathf.Abs(d.x);
            if (Mathf.Abs(m.x) > e.x + adx) return false;
            float ady = Mathf.Abs(d.y);
            if (Mathf.Abs(m.y) > e.y + ady) return false;
            float adz = Mathf.Abs(d.z);
            if (Mathf.Abs(m.z) > e.z + adz) return false;

            // Add a small epsilon to counteract potential arithmetic errors when the segment is
            // near-parallel to one of the coordinate axes
            adx += EPSILON;
            ady += EPSILON;
            adz += EPSILON;

            // Test cross products of segment direction vector with coordinate axes
            if (Mathf.Abs(m.y * d.z - m.z * d.y) > e.y * adz + e.z * ady) return false; // Cross with X-axis
            if (Mathf.Abs(m.z * d.x - m.x * d.z) > e.x * adz + e.z * adx) return false; // Cross with Y-axis
            if (Mathf.Abs(m.x * d.y - m.y * d.x) > e.x * ady + e.y * adx) return false; // Cross with Z-axis

            // No separating axis found; segment overlaps the AABB
            Random.InitState((int)((bbMin.x + bbMin.y + bbMin.z) * 10000));
            ;
            Debug.DrawLine(seg.a, seg.b, Random.ColorHSV(), 1000);
            return true;
        }


        /// <summary>
        /// Trouve les indices de tous les segments de la data de l'octree touchant une bounding box donnée.
        /// </summary>
        private List<ushort> FindBBOverlappingSegmentIndices(List<ushort> searchIndices,
            ref SparseOctree<Segment> octree, Vector3 bbMin, Vector3 bbMax)
        {
            List<ushort> output = new();
            foreach (ushort index in searchIndices)
            {
                //print("search indices length : "+searchIndices.Count);
                //print("octree data length : "+octree.data.Count);
                //print("Segment overlaps box : " + SegmentOverlapsBox(octree.data[index], bbMin, bbMax));
                if (SegmentOverlapsBox(octree.data[index], bbMin, bbMax))
                {
                    output.Add(index);
                }
            }

            return output;
        }

        private void OnDrawGizmos()
        {
            if (octree.Count > 0)
                DrawOctreeNode(0, _generator.BoundingBox.Item1, _generator.BoundingBox.Item2);
        }

        [ContextMenu("Print Index Buffer")]
        public void PrintIndexBuffer()
        {
            string s = "";
            foreach (ushort index in octree.indexBuffer)
            {
                s += index + " \n";
            }
            print(s);
        }
    }


#if UNITY_EDITOR

    [CustomEditor(typeof(LSystemSparseOctreeGenerator))]
    public class LSystemSparseOctreeGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            LSystemSparseOctreeGenerator t = (LSystemSparseOctreeGenerator)target;
            base.OnInspectorGUI();
            GUILayout.Label("index buffer length : " + t.octree.indexBuffer.Count);
            GUILayout.Label("data length : " + t.octree.data.Count);
            GUILayout.Label("node count : " + t.octree.nodes.Count);
            GUILayout.Space(2);
            GUILayout.Label("total size in bytes : " +
                            (t.octree.nodes.Count * sizeof(ushort)*8
                            + t.octree.indexBuffer.Count * sizeof(ushort)
                            + t.octree.data.Count * sizeof(float)*5));
        }
    }

#endif

}
