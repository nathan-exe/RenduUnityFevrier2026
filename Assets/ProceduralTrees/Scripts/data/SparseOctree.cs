using System;
using System.Collections.Generic;

namespace NathanTazi
{

    /// <summary>
    /// un sparse octree où chaque feuille pointe vers un segment d'une liste de data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SparseOctree<T>
    {

        /// <summary>
        /// raw, unordered data
        /// </summary>
        public List<T> data = new();

        /// <summary>
        /// Composé d'indices de la donnée contenue par chaque feuille de l'octree, séparés par des ffffffff.
        /// pointe vers un élément de data, ou ffffffff pour indiquer la fin d'une feuille de l'octree.
        /// |seg021, seg115, seg237, ffffffff, seg005, seg108, ffffffff, seg044, seg142, seg238, seg311, ffffffff, ... |
        /// |          octree leaf 0         |      octree leaf 1      |              octree leaf 2              | ... |
        /// </summary>
        public List<ushort> indexBuffer = new();

        public List<Node> nodes = new();

        public int Count => nodes.Count;

        public void Clear()
        {
            data.Clear();
            indexBuffer.Clear();
            nodes.Clear();
        }

        public class Node
        {
            public const ushort NULL_INDEX_VALUE = 0xffff;

            /// <summary>
            /// pour chaque node, le lsb indique si il pointe vers un autre node de la liste ou vers un sous segment de l'index buffer (feuilles de l'arbre)
            /// </summary>
            // x,y,z
            public ushort Child0Index = NULL_INDEX_VALUE, // 0,0,0
                Child1Index = NULL_INDEX_VALUE, // 0,0,1
                Child2Index = NULL_INDEX_VALUE, // 0,1,0
                Child3Index = NULL_INDEX_VALUE, // 0,1,1
                Child4Index = NULL_INDEX_VALUE, // 1,0,0
                Child5Index = NULL_INDEX_VALUE, // 1,0,1
                Child6Index = NULL_INDEX_VALUE, // 1,1,0
                Child7Index = NULL_INDEX_VALUE; // 1,1,1

            /// <returns>
            /// true : the child points to a subtree
            /// false : the child points to an element in the index buffer
            /// </returns>
            public static bool ChildIndexValuePointsToSubTree(ushort child)
            {
                return (child & 1u) == 1;
            }

            public static ushort GetArrayIndexForChildValue(ushort child)
            {
                return (ushort)(child >> 1);
            }

            //custom indexer
            public ushort this[ushort key]
            {
                get
                {
                    return key switch
                    {

                        0 => Child0Index,
                        1 => Child1Index,
                        2 => Child2Index,
                        3 => Child3Index,
                        4 => Child4Index,
                        5 => Child5Index,
                        6 => Child6Index,
                        7 => Child7Index,
                        _ => throw new IndexOutOfRangeException()
                    };
                }

                set
                {
                    switch (key)
                    {
                        case 0:
                            Child0Index = value;
                            break;
                        case 1:
                            Child1Index = value;
                            break;
                        case 2:
                            Child2Index = value;
                            break;
                        case 3:
                            Child3Index = value;
                            break;
                        case 4:
                            Child4Index = value;
                            break;
                        case 5:
                            Child5Index = value;
                            break;
                        case 6:
                            Child6Index = value;
                            break;
                        case 7:
                            Child7Index = value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

        }
    }
}