using System.Collections.Generic;

/// <summary>
/// un sparse octree où chaque feuille pointe vers un segment d'une liste de data.
/// </summary>
/// <typeparam name="T"></typeparam>
struct SparseOctree<T>
{
    /// <summary>
    /// raw, unordered data
    /// </summary>
    public List<T> data;
        
    /// <summary>
    /// Composé d'indices de la donnée contenue par chaque feuille de l'octree, séparés par des ffffffff.
    /// pointe vers un élément de data, ou ffffffff pour indiquer la fin d'une feuille de l'octree.
    /// |seg021, seg115, seg237, ffffffff, seg005, seg108, ffffffff, seg044, seg142, seg238, seg311, ffffffff, ... |
    /// |          octree leaf 0         |      octree leaf 1      |              octree leaf 2              | ... |
    /// </summary>
    public List<uint> indexBuffer;
        
    public List<Node> nodes;

    public void Clear()
    {
        data.Clear();
        indexBuffer.Clear();
        nodes.Clear();
    }
    
    public struct Node
    {
        /// <summary>
        /// pour chaque node, le lsb indique si il pointe vers un autre node de la liste ou vers un sous segment de l'index buffer (feuilles de l'arbre)
        /// </summary>
        // x,y,z
        public uint Child0, // 0,0,0
                    Child1, // 0,0,1
                    Child2, // 0,1,0
                    Child3, // 0,1,1
                    Child4, // 1,0,0
                    Child5, // 1,0,1
                    Child6, // 1,1,0
                    Child7; // 1,1,1
        
        /// <returns>
        /// true : the child points to a subtree
        /// false : the child points to an element in the index buffer
        /// </returns>
        public bool ChildPointsToSubTree(uint child)
        {
            return (child & 1u) == 1;
        }

        public uint GetArrayIndexForChild(uint child)
        {
            return child >> 1;
        }
    }
}