using System;
using UnityEngine;

namespace NathanTazi
{
    [ExecuteAlways]
    public class LSystemRenderer : MonoBehaviour
    {
        private static readonly int BoundingBoxCenterLsShaderProperty = Shader.PropertyToID("_boundingBoxCenter_ls");
        private static readonly int BoundingBoxSizeLsShaderProperty = Shader.PropertyToID("_boundingBoxSize_ls");
        private static readonly int SegmentsLsShaderProperty = Shader.PropertyToID("_segments_ls");
        private static readonly int TreeTransformLsToWsShaderProperty = Shader.PropertyToID("_treeTransform_ls_to_ws");
        private static readonly int SegmentCountShaderProperty = Shader.PropertyToID("_segmentCount");

        [SerializeField]
        private LSystemGenerator generator;

        [Header("Scene References")]
        [SerializeField] private GameObject cube;
        [SerializeField] private MeshRenderer _meshRenderer;
    
        ComputeBuffer buffer;
        
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
            GenerateBoundingBox();
            UpdateSegmentsBuffer();
            UpdateMaterialValues();
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
        void GenerateBoundingBox()
        {
            if (cube == null)
                cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            Vector3 min = transform.TransformPoint(generator.BoundingBoxLs.Item1);
            Vector3 max = transform.TransformPoint(generator.BoundingBoxLs.Item2);
        
            cube.transform.parent = null;
            cube.name = "BoundingBox";
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = (max - min);
            cube.transform.localPosition = (min + max)*.5f;
            cube.transform.SetParent(transform,true);
        }
    
        /// <summary>
        /// Envoie toutes la structure de l'arbre au shader via un material property block.
        /// </summary>
        /// <param name="bounds"></param>
        void UpdateMaterialValues()
        {
            MaterialPropertyBlock materialBlock = new MaterialPropertyBlock();
            materialBlock.SetVector(BoundingBoxCenterLsShaderProperty, generator.BoundingBoxLs.Item1);
            materialBlock.SetVector(BoundingBoxSizeLsShaderProperty, generator.BoundingBoxLs.Item2);
            materialBlock.SetBuffer(SegmentsLsShaderProperty, buffer);
            materialBlock.SetMatrix(TreeTransformLsToWsShaderProperty, transform.localToWorldMatrix);
            materialBlock.SetInteger(SegmentCountShaderProperty, generator.Graph.segments.Count);  
            _meshRenderer.SetPropertyBlock(materialBlock);
        }

    }
}
