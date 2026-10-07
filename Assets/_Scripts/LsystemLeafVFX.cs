using System;
using NathanTazi;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.VFX;

public class LsystemLeafVFX : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private LSystemGenerator _generator;
    [SerializeField] private Light _light;

    [SerializeField] private VisualEffect _vfx;
    GraphicsBuffer _positionBuffer ;
    
    private void Update()
    {
        
        if (_positionBuffer == null || _positionBuffer.count< _generator.Graph.leaves.Count*2)
        {
            _positionBuffer?.Release();
            _positionBuffer = new(
                GraphicsBuffer.Target.Structured,
                _generator.Graph.leaves.Count*2, sizeof(float) * 3);
            _vfx.SetGraphicsBuffer("positionBuffer",_positionBuffer);
        }
        
        NativeArray<Vector3> data = new NativeArray<Vector3>(
            _generator.Graph.leaves.Count*2, Allocator.Temp);
        for (int i = 0; i < _generator.Graph.leaves.Count; i++)
        {
            Vector3 worldPos =  /*_generator.Graph.leaves[i].branchTransform **/transform.localToWorldMatrix * (Vector4)_generator.Graph.leaves[i].localPosition;
            Vector3 worldNormal =  /*_generator.Graph.leaves[i].branchTransform **/transform.localToWorldMatrix * _generator.Graph.leaves[i].branchTransform * new Vector4(0,1,0,1);
            data[i*2] = worldPos;
            data[i*2+1] = worldNormal;
            Debug.DrawRay(worldPos,worldNormal*.1f,Color.red);
        }
        
        _positionBuffer.SetData(data);
        _vfx.SetInt("LeafCount",_generator.Graph.leaves.Count);
        _vfx.SetFloat("leafSizeMultiplier",_generator.lsystem.totalGrowth);
        _vfx.SetVector3("LightDirection",_light.transform.forward);
        data.Dispose();
    }

    private void OnDestroy()
    {
        _positionBuffer.Dispose();
    }
    
}
