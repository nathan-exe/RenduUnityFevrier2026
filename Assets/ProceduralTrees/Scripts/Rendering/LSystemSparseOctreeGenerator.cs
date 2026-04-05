using System;
using System.Collections.Generic;
using NathanTazi;
using UnityEngine;

public class LSystemSparseOctreeGenerator : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private LSystemGenerator _generator;

    [Header("Parameters")]
    [SerializeField]
    public int MaxSubdivisionLevels = 5;

    SparseOctree<Segment> octree;
    
    [ContextMenu("UpdateOctree")]
    void UpdateOctree()
    {
        octree.Clear();
        octree.data = _generator.Graph.segments;
    }
    void AddNodeAndSplitOverlappingChildrenIfTheyOverlapAnySegment(Vector3 bbMin, Vector3 bbMax, int recIndex, List<int> segmentIndices)
    {
        bool newSubtreeWasAdded = false;
        for (int i = 0; i < 8; i++)
        {
            //todo : compute child bounding box
            //todo : find overlapping segments
            // segmentIndices = findOverlappingSegments()
            if (segmentIndices.Count > 0)
            {
                newSubtreeWasAdded = true;
                //todo : link child index to next node index
                
                // ptet qu'il y'aurait besoin d'executer ça que après le dernier if de la fonction, après cette boucle (utiliser une pile / file ?)
                AddNodeAndSplitOverlappingChildrenIfTheyOverlapAnySegment(bbMin, bbMax,  recIndex + 1,segmentIndices);
            }
        }

        if ((recIndex >= MaxSubdivisionLevels) || !newSubtreeWasAdded)
        {
            //todo : add segment indices to index buffer
            //todo : link parent's corresponding child index to first element of added indices if no subtree was created
        }
        
        
    }
    private void OnDrawGizmos()
    {
        //draw octree
    }
}
