using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ComputeShaderRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    public ComputeShader computeShader;
    public string kernelName = "CSRTTest";
    public string property_rwOutput = "_output";
    public RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32;

    private int _propertyID_rwOutput = -1;

    private ComputeShaderRenderPass _renderPass;
    
    public override void Create()
    {
        _propertyID_rwOutput = Shader.PropertyToID(property_rwOutput);
        _renderPass = new ComputeShaderRenderPass(passEvent);
    }
    
    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!computeShader) return;
        _renderPass.SetComputeShader(computeShader, kernelName, _propertyID_rwOutput, renderTextureFormat);
        _renderPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(_renderPass);
    }
}
