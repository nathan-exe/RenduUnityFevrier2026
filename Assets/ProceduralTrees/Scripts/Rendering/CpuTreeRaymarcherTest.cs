using System;
using _Scripts.Extensions;
using NathanTazi;
using UnityEngine;

public class CpuTreeRaymarcherTest : MonoBehaviour
{
    
    [SerializeField] private LSystemSparseOctreeGenerator _sparseOctreeGenerator;
    [SerializeField] private LSystemGenerator _lSystemGenerator;

    float sdfTree(SparseOctree<Segment> octree, Vector3 point,int nodeIndex, Vector3 bbMin, Vector3 bbMax, int recIndex = 0)
    {
        //todo :
        //find closest non empty octree node
        //if leaf, return sdf
        //else recursively call this method on the node
        
        //find closest non empty octree node
        Vector3 bbHalfSize = (bbMax - bbMin) * .5f;

        BoundingBox bestBB = new BoundingBox(Vector3.zero,Vector3.zero );
        ushort bestNodeIndex = 0;
        float smallestSdf = float.MaxValue;
        for (ushort i = 0; i < 8; i++)
        {
            if (octree.nodes[nodeIndex][i] != SparseOctree<Segment>.Node.NULL_INDEX_VALUE)
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

                float sdf = AabbSdf(point, subTreeBbMin + subTreeBbHalfSize, subTreeBbHalfSize);

                if (sdf < smallestSdf)
                {
                    smallestSdf = sdf;
                    bestNodeIndex =  octree.nodes[nodeIndex][i];
                    bestBB = new BoundingBox(subTreeBbMin, subTreeBbMax);
                }
            }
        }
        
        bool pointsToSubtree = SparseOctree<Segment>.Node.ChildIndexValuePointsToSubTree(bestNodeIndex);
        Gizmos.color = pointsToSubtree? Color.orange.WithAlpha(.5f) : Color.red;
        Gizmos.DrawWireCube(
            _lSystemGenerator.transform.TransformPoint( bestBB.center)
         ,_lSystemGenerator.transform.TransformVector(bestBB.size));

         
         //descend down the tree
         if (pointsToSubtree)
            sdfTree(octree, point, SparseOctree<Segment>.Node.GetArrayIndexForChildValue(bestNodeIndex), bestBB.min, bestBB.max, recIndex+1);
         else
         {//sdf segment array
             
             Gizmos.color = Color.red.WithAlpha(.2f);
             Gizmos.DrawCube(
                 _lSystemGenerator.transform.TransformPoint( bestBB.center)
                 ,_lSystemGenerator.transform.TransformVector(bestBB.size));
             
             int firstBranchIndex = SparseOctree<Segment>.Node.GetArrayIndexForChildValue(bestNodeIndex);
             //
             Gizmos.color = Color.red;
             for (int j = firstBranchIndex;
                  j < octree.leafDataIndexBuffer.Count && 
                  octree.leafDataIndexBuffer[j] != SparseOctree<Segment>.Node.NULL_INDEX_VALUE;
                  j++)
             {
                 Segment s = octree.data[octree.leafDataIndexBuffer[j]];

                 Vector3 wsA = _lSystemGenerator.transform.TransformPoint(s.a);
                 Vector3 wsB = _lSystemGenerator.transform.TransformPoint(s.b);

                 Gizmos.matrix = Matrix4x4.LookAt(wsA,wsB,Vector3.up);
                 Gizmos.DrawCube(Vector3.forward * Vector3.Distance(wsA,wsB) * .5f,new Vector3(.1f,.1f,Vector3.Distance(wsA,wsB)));
                 
                 // Gizmos.DrawLine(
                 //     _lSystemGenerator.transform.TransformPoint(s.a),
                 //     _lSystemGenerator.transform.TransformPoint(s.b));
             }
             Gizmos.matrix = Matrix4x4.identity;
             //
             // int branchCount = j - firstBranchIndex; //-1 ?
             // //print("fisrt branch index of leaf : " + firstBranchIndex);
             // Gizmos.color = Color.Lerp(Color.green, Color.red,
             //     (float)branchCount / _maxSegmentsPerLeaf);
             // Gizmos.color = Gizmos.color.WithAlpha((float)recIndex / MaxSubdivisionLevels/MaxSubdivisionLevels);
        }

        // //recursively try to draw subtrees
        // //print("child "+i+" value : "+octree.nodes[nodeIndex][i]);
        // if (pointsToSubtree)
        // {
        //     //print("subtree found !!!!");
        //     ushort childIndex = SparseOctree<Segment>.Node.GetArrayIndexForChildValue(octree.nodes[nodeIndex][i]);
        //     DrawOctreeNode(childIndex, subTreeBbMin, subTreeBbMax, recIndex + 1);
        // }

        return 0;
    }

    float AabbSdf(Vector3 point, Vector3 center, Vector3 halfSize)
    {
        point = point - center;
        Vector3 q = new Vector3(Mathf.Abs(point.x),Mathf.Abs(point.y),Mathf.Abs(point.z)) - halfSize;
        return  
            new Vector3 (Mathf.Max(q.x,0f),Mathf.Max(q.y,0f),Mathf.Max(q.z,0f)).magnitude
            + Mathf.Min(Mathf.Max(q.x,Mathf.Max(q.y,q.z)),0f);
    }

    private void OnDrawGizmos()
    {
        float sdf = sdfTree(_sparseOctreeGenerator.octree
            , _lSystemGenerator.transform.InverseTransformPoint(transform.position)
            , 0
            ,_lSystemGenerator.BoundingBoxLs.min
            ,_lSystemGenerator.BoundingBoxLs.max);
        //Gizmos.DrawWireSphere(transform.position,sdf);
    }
}
