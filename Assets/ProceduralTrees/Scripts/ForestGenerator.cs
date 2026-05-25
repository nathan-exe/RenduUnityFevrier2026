using System;
using System.Collections.Generic;
using NathanTazi;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class ForestGenerator : MonoBehaviour
{
    public List<LSystemGenerator> _trees;
    public Vector2 _growthRemap;
    public Vector2 _radiusRangeOverLife;
    public string actiom = "x";

    private void OnValidate()
    {
        foreach (LSystemGenerator tree in _trees)
        {
            tree.totalGrowth = Random.Range(_growthRemap.x, _growthRemap.y);
            tree.radiusRangeOverLife = _radiusRangeOverLife;
            tree._axiom = actiom;
            tree.RefreshGraph();
        }
    }

    public void Reseed()
    {
        foreach (LSystemGenerator tree in _trees)
        {
            tree.lsystem.seed = Random.Range(0, 510501684);
            tree.RefreshGraph();
        }
    }
}


#if UNITY_EDITOR

[CustomEditor(typeof(ForestGenerator))]
public class ForestGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ForestGenerator t = (ForestGenerator)target;
        base.OnInspectorGUI();
        
        if(GUILayout.Button("reseed"))
            t.Reseed();
    }
}

#endif
