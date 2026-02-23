using System;
using System.Collections.Generic;
using NathanTazi;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace NathanTazi
{
    [ExecuteAlways]
    public class LSystemLeafRenderer : MonoBehaviour
    {
        [SerializeField]
        private LSystemGenerator _generator;

        private List<Transform> _leaves = new();

        [SerializeField]
        [Tooltip("from a, to b, from b, to b")]
        private Vector4 leafSizeRemap;

        [SerializeField]
        private Mesh _leafMesh;

        [SerializeField]
        private Material _leafMaterial;

        [SerializeField] [Range(0,1)]
        private float _leafFlattening;

        void InstantiateLeaf()
        {
            GameObject go = new GameObject("Leaf");
            go.transform.parent = transform;
            go.AddComponent<MeshRenderer>().material = _leafMaterial;
            go.AddComponent<MeshFilter>().sharedMesh = _leafMesh;
            _leaves.Add(go.transform);
        }

        private void UpdateLeaf(Transform leaf,PlantGraph.Leaf info)
        {
            leaf.transform.position = transform.TransformPoint(info.position);
            leaf.transform.rotation = 
                Quaternion.Slerp(
                    Quaternion.LookRotation(info.branchTransform*Vector3.forward,info.branchTransform*Vector3.up),
                    Quaternion.LookRotation(-Vector3.up,info.branchTransform*Vector3.up),
                    _leafFlattening);
            leaf.transform.localScale = 
                Mathf.Lerp(leafSizeRemap.z, leafSizeRemap.w,
                Mathf.InverseLerp(leafSizeRemap.x, leafSizeRemap.y, info.size))
                * Vector3.one;
        }

        void RegenerateLeafList()
        {
            int childCount = transform.childCount;
            for (int i = 0; i<childCount;i++)
            {
                DestroyImmediate(transform.GetChild(0).gameObject);
            }
            
            _leaves.Clear();
            for (int i = 0; i < _generator.Graph.leaves.Count; i++)
            {
                InstantiateLeaf();
            }
        }
        
        
        public void UpdateLeaves()
        {
            RegenerateLeafList();
            //_generator.Graph.leaves
            //if(_generator.Graph.leaves.Count!=_leaves.Count || transform.childCount!=_leaves.Count)
                //RegenerateLeafList();

            for (var i = 0; i < _leaves.Count; i++)
            {
                UpdateLeaf(_leaves[i],_generator.Graph.leaves[i]);
            }
        }
        
        private void OnValidate() 
        {
            //UpdateLeaves();
        }
        
        void OnLSystemRegenerated()
        {
            //UpdateLeaves();
        }
        

    }
}


#if UNITY_EDITOR

[CustomEditor(typeof(LSystemLeafRenderer))]
public class LSystemLeafRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if(GUILayout.Button("Update Leaves")) ((LSystemLeafRenderer)target).UpdateLeaves();
        LSystemLeafRenderer t = (LSystemLeafRenderer)target;
        
        base.OnInspectorGUI();
    }
}

#endif
