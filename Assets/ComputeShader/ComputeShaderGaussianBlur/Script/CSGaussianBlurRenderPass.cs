using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CSGaussianBlurRenderPass : ScriptableRenderPass
{
    private static readonly string PASS_TAG = "CSGaussianBlurRenderPass";
    private static readonly int PROPERTY_TEMPBUFFER_1 = Shader.PropertyToID("_CSGaussianBlurRenderPassTempBuffer_1");
    private static readonly int PROPERTY_TEMPBUFFER_2 = Shader.PropertyToID("_CSGaussianBlurRenderPassTempBuffer_2");

    private static readonly int ThreadCount = 256;
    private static readonly int BlurRadius = 9;
    private   float[] BlurWeight = new float[9]
                                  { 0.01621622f, 0.05405405f, 0.12162162f,
                                    0.19459459f, 0.22702703f, 0.19459459f,
                                    0.12162162f, 0.05405405f, 0.01621622f };

    private RenderTargetIdentifier _destination; 
    private RenderTargetIdentifier _tempBuffer_1 = new RenderTargetIdentifier(PROPERTY_TEMPBUFFER_1); 
    private RenderTargetIdentifier _tempBuffer_2 = new RenderTargetIdentifier(PROPERTY_TEMPBUFFER_2); 

    private ComputeShader _computeShader;
    private int _blurStep = 8;

    private ComputeBuffer _weightBuffer;
    private int _kernelIndex_Horizontal = -1;
    private int _kernelIndex_Vertical = -1;

    private int _downSampling = 1;

    public CSGaussianBlurRenderPass(RenderPassEvent renderPassEvent)
    {
        this.renderPassEvent = renderPassEvent;
    }

    public void Setup(RenderTargetIdentifier renderTargetDestination)
    {
        _destination = renderTargetDestination;
    }

    public void SetupComputeShader(ComputeShader computeShader, int blurStep, int downSampling)
    {
        _blurStep = blurStep;
        _computeShader = computeShader;
        _downSampling = downSampling;
        _kernelIndex_Horizontal = computeShader.FindKernel("CSBlurHorizontal");
        _kernelIndex_Vertical = computeShader.FindKernel("CSBlurVertical");

        _weightBuffer = new ComputeBuffer(BlurRadius, sizeof(float));
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += ClearBuffer;
#endif

      
        _weightBuffer.SetData(BlurWeight);

        _computeShader.SetBuffer(_kernelIndex_Horizontal, "_Weights", _weightBuffer);
        _computeShader.SetBuffer(_kernelIndex_Vertical, "_Weights", _weightBuffer);
    }
    
    public void ClearBuffer()
    {
        if (_weightBuffer != null) _weightBuffer.Release();
        _weightBuffer = null;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!_computeShader) return;
        if (_blurStep == 0) return;

        CommandBuffer cmd = CommandBufferPool.Get(PASS_TAG);
        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.sRGB = false;
        descriptor.colorFormat = RenderTextureFormat.ARGB32;
        descriptor.depthBufferBits = 0;
        descriptor.width /= _downSampling;
        descriptor.height /= _downSampling;
        descriptor.useMipMap = false;
        descriptor.enableRandomWrite = true;

      
        cmd.GetTemporaryRT(PROPERTY_TEMPBUFFER_1, descriptor, FilterMode.Bilinear);
        cmd.GetTemporaryRT(PROPERTY_TEMPBUFFER_2, descriptor, FilterMode.Bilinear);

        cmd.Blit(_destination, _tempBuffer_1);

        int threadGroupsX = Mathf.CeilToInt((float)descriptor.width / (float)ThreadCount);
        int threadGroupsY = Mathf.CeilToInt((float)descriptor.height / (float)ThreadCount);

        cmd.SetComputeTextureParam(_computeShader, _kernelIndex_Horizontal, "_Source", _tempBuffer_1);
        cmd.SetComputeTextureParam(_computeShader, _kernelIndex_Horizontal, "_Output_Horizontal", _tempBuffer_2);
        cmd.SetComputeTextureParam(_computeShader, _kernelIndex_Vertical, "_Source", _tempBuffer_2);
        cmd.SetComputeTextureParam(_computeShader, _kernelIndex_Vertical, "_Output_Vertical", _tempBuffer_1);
       
        cmd.DispatchCompute(_computeShader, _kernelIndex_Horizontal, threadGroupsX, descriptor.height, 1);
        cmd.DispatchCompute(_computeShader, _kernelIndex_Vertical, descriptor.width, threadGroupsY, 1);

        
        for(int i=1; i < _blurStep; i++)
        {
            cmd.SetComputeTextureParam(_computeShader, _kernelIndex_Horizontal, "_Source", _tempBuffer_1);
            cmd.SetComputeTextureParam(_computeShader, _kernelIndex_Horizontal, "_Output_Horizontal", _tempBuffer_2);
            cmd.SetComputeTextureParam(_computeShader, _kernelIndex_Vertical, "_Source", _tempBuffer_2);
            cmd.SetComputeTextureParam(_computeShader, _kernelIndex_Vertical, "_Output_Vertical", _tempBuffer_1);

            cmd.DispatchCompute(_computeShader, _kernelIndex_Horizontal, threadGroupsX, descriptor.height, 1);
            cmd.DispatchCompute(_computeShader, _kernelIndex_Vertical, descriptor.width, threadGroupsY, 1);
        }

        cmd.Blit(_tempBuffer_1, _destination);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
     
    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(PROPERTY_TEMPBUFFER_1);
        cmd.ReleaseTemporaryRT(PROPERTY_TEMPBUFFER_2);
    }
    
 
    
  

}
