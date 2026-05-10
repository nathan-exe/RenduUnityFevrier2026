using System;
using System.Collections.Generic;
using System.Linq;
using NathanTazi;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ProfilingSetup : MonoBehaviour
{
    
    [SerializeField] private List<Camera> _cameras;
    [SerializeField] private LSystemGenerator _generator;
    [SerializeField] private UniversalRenderPipelineAsset _pipelineAsset;
    [SerializeField] private TMP_Text _debugText, _fpsText;

    [SerializeField] private int _cameraID = 1, _generatorIterations = 3;
    [SerializeField] private float _renderScale = 1;

    private const int FPS_QUEUE_CAPACITY = 50;
    private Queue<float> _lastDt = new(FPS_QUEUE_CAPACITY);

    void Update()
    {
        //camera distance
        if (Input.GetKeyUp(KeyCode.W))
        {
            _cameras[_cameraID].enabled = false;
            _cameraID = Mathf.Clamp(_cameraID-1, 0, _cameras.Count-1);
            _cameras[_cameraID].enabled = true;
            UpdateUI();
        }else if (Input.GetKeyUp(KeyCode.S))
        {
            _cameras[_cameraID].enabled = false;
            _cameraID = Mathf.Clamp(_cameraID+1, 0, _cameras.Count-1);
            _cameras[_cameraID].enabled = true;
            UpdateUI();
        }
        
        //screen resolution
        if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            _renderScale -= 0.05f;
            _pipelineAsset.renderScale = _renderScale;
            UpdateUI();
        }else if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            _renderScale += 0.05f;
            _pipelineAsset.renderScale = _renderScale;
            UpdateUI();
        }
        
        //tree branches
        if (Input.GetKeyUp(KeyCode.A))
        {
            _generatorIterations = Mathf.Clamp(_generatorIterations-1, 0, 6);
            _generator.iterations = _generatorIterations;
            _generator.RefreshGraph();
            UpdateUI();
        }else if (Input.GetKeyUp(KeyCode.D))
        {
            _generatorIterations = Mathf.Clamp(_generatorIterations+1, 0, 6);
            _generator.iterations = _generatorIterations;
            _generator.RefreshGraph();
            UpdateUI();
        }
        
        if (Input.GetKeyUp(KeyCode.Space))
        {
            _generator.lsystem.enableBranchReduction = !_generator.lsystem.enableBranchReduction;
        }
        
        //fps counter
        _lastDt.Enqueue(Time.deltaTime);
        if (_lastDt.Count >= FPS_QUEUE_CAPACITY)
            _lastDt.Dequeue();
        float averageDt = _lastDt.Sum()/_lastDt.Count;
        float averageFps = 1.0f / averageDt;
        _fpsText.text = $"Average fps : {(int)averageFps}\n Average ms : {(int)(averageDt * 1000)}";
    }

    void UpdateUI()
    {
        _debugText.text = $"Cam : \"{_cameras[_cameraID].gameObject.name}\"\nRender scale : {_renderScale}\nBranch count : {_generator.Graph.segments.Count}\n";
    }
}
