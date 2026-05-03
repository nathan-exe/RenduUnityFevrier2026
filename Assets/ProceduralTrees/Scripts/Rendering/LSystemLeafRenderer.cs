using System.Collections.Generic;
using System.Threading.Tasks;
using NathanTazi;
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
        private Transform _leafParent;
        [SerializeField]
        private Material _leafMaterial;

        [SerializeField] [Range(0,1)]
        private float _leafFlattening;

        void InstantiateLeaf()
        {
            GameObject go = new GameObject("Leaf");
            go.transform.parent = _leafParent;
            go.AddComponent<MeshRenderer>().material = _leafMaterial;
            go.AddComponent<MeshFilter>().sharedMesh = _leafMesh;
            _leaves.Add(go.transform);
        }

        private void UpdateLeaf(Transform leaf,PlantGraph.Leaf info)
        {
            leaf.transform.position = transform.TransformPoint(info.localPosition);
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
        
        
        
        public void UpdateLeaves()
        {
            _leaves.RemoveAll(t => !t);
            print(_generator.Graph);
            print(_generator.Graph.leaves);
            print(_leaves);
            //_generator.Graph.leaves
            if (_generator.Graph.leaves.Count != _leaves.Count)
            {
                int diff = _generator.Graph.leaves.Count - _leaves.Count;
                if (diff > 0)
                {
                    for (int i = 0; i < diff; i++)
                    {
                        InstantiateLeaf();
                    }
                }
                else
                {
                    int endIndex = _leaves.Count + diff;
                    for (int i = _leaves.Count - 1; i >= endIndex; i--)
                    {
                        DestroyImmediate(_leaves[i].gameObject);
                        _leaves.RemoveAt(i);
                    }
                }
            }

            for (int i = 0; i < _leaves.Count; i++)
            {
                UpdateLeaf(_leaves[i],_generator.Graph.leaves[i]);
            }
        }
        
        private async void OnValidate() 
        {
            _leafParent ??= transform;
            await Task.Yield();
            UpdateLeaves();
        }
        
        async void OnLSystemRegenerated()
        {
            await Task.Yield();
            UpdateLeaves();
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
