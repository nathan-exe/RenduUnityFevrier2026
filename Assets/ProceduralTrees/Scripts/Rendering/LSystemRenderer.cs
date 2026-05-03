using System;
using UnityEngine;

namespace NathanTazi
{
    [ExecuteAlways]
    public class LSystemRenderer : MonoBehaviour
    {
        private static readonly int BoundingBoxMinLsShaderProperty = Shader.PropertyToID("_boundingBoxMin_ls");
        private static readonly int BoundingBoxMaxLsShaderProperty = Shader.PropertyToID("_boundingBoxMax_ls");
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
            buffer = new ComputeBuffer(generator.Graph.segments.Count,Segment.Size,ComputeBufferType.Constant,ComputeBufferMode.Dynamic );
            
            //on trie les capsules selon leur rayon pour tomber plus vite sur les grosses capsules dans le fragment shader.
            //todo : SortedSet<Segment>
            generator.Graph.segments
                .Sort((seg1, seg2) => seg2.radiusA.CompareTo(seg1.radiusA));
            buffer.SetData(generator.Graph.segments);
        }
        
        /// <summary>
        /// fait spawn un cube en 3D qui représente la bounding box de la plante.
        /// </summary>
        void GenerateBoundingBox()
        {
            if (cube == null)
                cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(transform,true);
            
            Vector3 min = (generator.BoundingBoxLs.min);
            Vector3 max = (generator.BoundingBoxLs.max);
        
            //cube.transform.parent = null;
            cube.name = "BoundingBox";
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = (max - min);
            cube.transform.localPosition = (min + max)*.5f;
            //cube.transform.SetParent(transform,true);
        }
    
        /// <summary>
        /// Envoie toutes la structure de l'arbre au shader via un material property block.
        /// </summary>
        /// <param name="bounds"></param>
        void UpdateMaterialValues()
        {
            MaterialPropertyBlock materialBlock = new MaterialPropertyBlock();
            materialBlock.SetVector(BoundingBoxMinLsShaderProperty, generator.BoundingBoxLs.min);
            materialBlock.SetVector(BoundingBoxMaxLsShaderProperty, generator.BoundingBoxLs.max);
            materialBlock.SetBuffer(SegmentsLsShaderProperty, buffer);
            materialBlock.SetMatrix(TreeTransformLsToWsShaderProperty, transform.localToWorldMatrix);
            materialBlock.SetInteger(SegmentCountShaderProperty, generator.Graph.segments.Count);  
            _meshRenderer.SetPropertyBlock(materialBlock);
        }

    }
}
