using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CSGaussianBlurRenderFeature : ScriptableRendererFeature
{
    public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    public ComputeShader computeShader;
    [Range(1, 8)] public int blurStep = 8;

    private CSGaussianBlurRenderPass _renderPass;
    
    public override void Create()
    {
        if (!computeShader) return;
        _renderPass = new CSGaussianBlurRenderPass(passEvent);
        _renderPass.SetupComputeShader(computeShader, blurStep);
    }
    
    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!computeShader) return;
        _renderPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(_renderPass);
    }
}
