using NathanTazi;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class ArduinoTreeGenerator : MonoBehaviour
{
    [Header("Debug values")]
    [SerializeField][Range(0,1)] private float _waterLevel01;
    [SerializeField][Range(0,360)] private float _lightAngleInDegrees;
    [SerializeField][Range(0,1)] private float _lightColorIndex01;
    [SerializeField][Range(0,2)] private int _seedIndex;

    [Header("Values")]
    [SerializeField] private Texture2D _gradient;
    
    [Header("References")]
    [SerializeField] private LSystemGenerator _generator;
    [SerializeField] private Light _light;
    [SerializeField] private Transform _lightParent;


    public void SetDataAndRefreshTree(float waterLevel01, float lightAngleInDegrees, float lightColorIndex01, int seedIndex)
    {
        //todo : seed
        
        //light color
        float h, s, v;
        Color.RGBToHSV(_light.color, out h, out s, out v);
        h=lightColorIndex01;
        _light.color = Color.HSVToRGB(h,s,v);
        
        //light angle
        _lightParent.transform.rotation = Quaternion.Euler(lightAngleInDegrees, 0, 0);
        
        //water level
        _generator.totalGrowth = waterLevel01;
        
        _generator.RefreshGraph();
    }
    
    //debug
    void OnValidate()
    {
        SetDataAndRefreshTree(_waterLevel01,_lightAngleInDegrees,_lightColorIndex01,_seedIndex);
    }
}
