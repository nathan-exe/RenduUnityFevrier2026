using System;
using NathanTazi;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Experimental.Rendering;

public class ArduinoTreeController : MonoBehaviour
{
    private static readonly int HUE_MAP_OVER_AGE_SHADER_PROPERTY_INDEX = Shader.PropertyToID("_hueMapOverAge");

    [Header("Debug values")]
    [SerializeField][Range(0,1)] private float _waterLevel01;
    [SerializeField][Range(0,360)] private float _lightAngleInDegrees;
    [SerializeField][Range(0,1)] private float _lightHueShift01;
    [SerializeField][Range(0,2)] private int _seedIndex;

    [Header("Settings")]
    [SerializeField] private float _growthSmoothTime;
    
    [Header("References")]
    [SerializeField] private LSystemGenerator _generator;
    [SerializeField] private Light _light;
    [SerializeField] private Transform _lightParent;
    [SerializeField] private Material _treeMaterial;
    
    [SerializeField] private Texture2D _hueMapOverAge;

    [SerializeField] private float _treeGrowthLevel = 0;
    [SerializeField] private float _smoothedTreeGrowthLevel = 0;
    
    private const int COLOR_HUE_MAP_WIDTH_WHEN_TREE_IS_GROWN = 128;
    private float[] pixelValues = new float[COLOR_HUE_MAP_WIDTH_WHEN_TREE_IS_GROWN];
    
    
    private float _growthRate;


    void Reset()
    {
        _hueMapOverAge.Reinitialize(1,1);
        _generator.totalGrowth = 0;
    }

    private void Update()
    {
        SetData(_waterLevel01,_lightAngleInDegrees,_lightHueShift01,_seedIndex);
        
        _smoothedTreeGrowthLevel = Mathf.SmoothDamp(_smoothedTreeGrowthLevel, _treeGrowthLevel, ref _growthRate,_growthSmoothTime);
        _generator.totalGrowth = _smoothedTreeGrowthLevel;
    }
    
    
    private void Awake()
    {
        _hueMapOverAge = new Texture2D(1,1,DefaultFormat.LDR,TextureCreationFlags.None );
        _hueMapOverAge.wrapMode = TextureWrapMode.Clamp;
        _hueMapOverAge.filterMode = FilterMode.Bilinear;
        _hueMapOverAge.anisoLevel = 0;
        _hueMapOverAge.Apply();
        _treeMaterial.SetTexture(HUE_MAP_OVER_AGE_SHADER_PROPERTY_INDEX,_hueMapOverAge);
        
        Reset();
        
    }

    private int previousTextureWidth = 1;
    public void SetData(float waterLevel01, float lightAngleInDegrees, float lightHueShift01, int seedIndex)
    {
        //todo : voice -> sinusoide Pitch angle
        
        //todo : seed, reset if changed and return
        
        //light color
        float h, s, v;
        Color.RGBToHSV(_light.color, out h, out s, out v);
        h=lightHueShift01;
        _light.color = Color.HSVToRGB(h,s,v);
        
        //light angle
        _lightParent.transform.rotation = Quaternion.Euler(lightAngleInDegrees, 0, 0);
        
        //water level
        
        int newTextureWidth = Mathf.CeilToInt(_smoothedTreeGrowthLevel*COLOR_HUE_MAP_WIDTH_WHEN_TREE_IS_GROWN);
        
        //set hue shift over age
        _hueMapOverAge.Reinitialize(newTextureWidth,1);
        for (int x = 0; x < newTextureWidth; x++)
        {
            if( x >= previousTextureWidth)
                pixelValues[x] = lightHueShift01;
            _hueMapOverAge.SetPixel(x, 0,new Color(pixelValues[x],pixelValues[x],pixelValues[x]));
        }
        _hueMapOverAge.Apply();
        _treeMaterial.SetTexture(HUE_MAP_OVER_AGE_SHADER_PROPERTY_INDEX,_hueMapOverAge);
        
        _treeGrowthLevel = Mathf.Pow(waterLevel01,.5f);
        _generator.totalGrowth = _smoothedTreeGrowthLevel;

        previousTextureWidth = newTextureWidth;
        
        
    }
    

}
