using System;
using NathanTazi;
using Unity.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class LsystemLeafVFX : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private LSystemGenerator _generator;

    [SerializeField] private VisualEffect _vfx;
    GraphicsBuffer buffer ;
    
    private void Update()
    {
        
        if (buffer == null || buffer.count< _generator.Graph.leaves.Count)
        {
            buffer?.Release();
            buffer = new(
                GraphicsBuffer.Target.Structured,
                _generator.Graph.leaves.Count, sizeof(float) * 3);
            _vfx.SetGraphicsBuffer("positionBuffer",buffer);
        }
        
        
        NativeArray<Vector3> data = new NativeArray<Vector3>(
            _generator.Graph.leaves.Count, Allocator.Temp);
        for (int i = 0; i < _generator.Graph.leaves.Count; i++)
        {
            Vector3 worldPos =  /*_generator.Graph.leaves[i].branchTransform **/transform.localToWorldMatrix * (Vector4)_generator.Graph.leaves[i].localPosition;
            data[i] = worldPos;
            Debug.DrawRay(worldPos,Vector3.up*.1f,Color.red);
        }
            
        buffer.SetData(data);
        _vfx.SetInt("LeafCount",_generator.Graph.leaves.Count);
        data.Dispose();
    }

    private void OnDestroy()
    {
        buffer.Dispose();
    }
    
}
