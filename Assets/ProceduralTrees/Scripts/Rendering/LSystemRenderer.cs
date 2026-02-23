using System;
using UnityEngine;

namespace NathanTazi
{
    [ExecuteAlways]
    public class LSystemRenderer : MonoBehaviour
    {
        [SerializeField]
        private LSystemGenerator generator;

        [Header("Scene References")]
        [SerializeField] private GameObject cube;
        [SerializeField] private MeshRenderer _meshRenderer;
    
        ComputeBuffer buffer;
        private void OnValidate() 
        {
            Setup();
        }

        private void Start()
        {
            Setup();
        }
        void OnLSystemRegenerated()
        {
            Setup();
        }
        void OnDestroy()
        {
            if(buffer.IsValid())
                buffer.Release();
        }

        [ContextMenu("Refresh")]
        public void Setup()
        {
            GenerateBoundingBox(out Tuple<Vector3, Vector3> worldBounds);
            UpdateSegmentsBuffer();
            UpdateMaterialValues(worldBounds);
        }

        /// <summary>
        /// génère un buffer contenant une liste de segments définis par deux points et un rayon.
        /// </summary>
        private void UpdateSegmentsBuffer()
        {
            if (buffer != null)
                buffer.Release();
            buffer = new ComputeBuffer(generator.Graph.segments.Count,Segment.Size );
            buffer.SetData(generator.Graph.segments);
        }
        
        /// <summary>
        /// fait spawn un cube en 3D qui représente la bounding box de la plante.
        /// </summary>
        /// <param name="worldBounds">bb center, bb size</param>
        void GenerateBoundingBox(out Tuple<Vector3, Vector3> worldBounds)
        {
            if (cube == null)
                cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

            Vector3 min = transform.TransformPoint(generator.BoundingBox.Item1);
            Vector3 max = transform.TransformPoint(generator.BoundingBox.Item2);
        
            cube.transform.parent = null;
            cube.name = "BoundingBox";
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = (max - min);
            cube.transform.localPosition = (min + max)*.5f;
            worldBounds = new Tuple<Vector3, Vector3>(cube.transform.localPosition,cube.transform.localScale);
            cube.transform.SetParent(transform,true);
        }
    
        /// <summary>
        /// Envoie toutes la structure de l'arbre au shader via un material property block.
        /// </summary>
        /// <param name="bounds"></param>
        void UpdateMaterialValues( Tuple<Vector3, Vector3> bounds)
        {
            MaterialPropertyBlock materialBlock = new MaterialPropertyBlock();
            materialBlock.SetVector("_boundingBoxCenter", bounds.Item1);
            materialBlock.SetVector("_boundingBoxSize", bounds.Item2);
            materialBlock.SetBuffer("_segments", buffer);
            materialBlock.SetMatrix("_treeTransform", transform.localToWorldMatrix);
            materialBlock.SetInteger("_segmentCount", generator.Graph.segments.Count);  
            _meshRenderer.SetPropertyBlock(materialBlock);
        }

    }
}
