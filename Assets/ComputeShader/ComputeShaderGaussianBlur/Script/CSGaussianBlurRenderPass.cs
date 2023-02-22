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
    private static readonly float[] BlurWeight = new float[9]
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


    public CSGaussianBlurRenderPass(RenderPassEvent renderPassEvent)
    {
        this.renderPassEvent = renderPassEvent;
    }
    
    public void Setup(RenderTargetIdentifier renderTargetDestination)
    {
        _destination = renderTargetDestination;
    }

    public void SetupComputeShader(ComputeShader computeShader, int blurStep)
    {
        _blurStep = blurStep;
        _computeShader = computeShader;
        _kernelIndex_Horizontal = computeShader.FindKernel("CSBlurHorizontal");
        _kernelIndex_Vertical = computeShader.FindKernel("CSBlurVertical");

        _weightBuffer = new ComputeBuffer(BlurRadius, sizeof(float));
#if UNITY_EDITOR
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += ClearBuffer;
#endif
        _weightBuffer.SetData(BlurWeight);

        _computeShader.SetBuffer(_kernelIndex_Horizontal, "Weights", _weightBuffer);
        _computeShader.SetBuffer(_kernelIndex_Vertical, "Weights", _weightBuffer);
        _computeShader.SetInt("blurRadius", BlurRadius);
    }
    
    public void ClearBuffer()
    {
        if (_weightBuffer != null) _weightBuffer.Release();
        _weightBuffer = null;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!_computeShader) return;

        CommandBuffer cmd = CommandBufferPool.Get(PASS_TAG);
        CameraData cameraData = renderingData.cameraData;
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(cameraData.camera.scaledPixelWidth, cameraData.camera.scaledPixelHeight);
        descriptor.colorFormat = RenderTextureFormat.RGB111110Float;
        descriptor.useMipMap = false;
        descriptor.enableRandomWrite = true;
        cmd.GetTemporaryRT(PROPERTY_TEMPBUFFER_1, descriptor, FilterMode.Bilinear);
        cmd.GetTemporaryRT(PROPERTY_TEMPBUFFER_2, descriptor, FilterMode.Bilinear);

        cmd.Blit(_destination, _tempBuffer_1);


        int threadGroupsX = Mathf.CeilToInt((float)descriptor.width / (float)ThreadCount);
        int threadGroupsY = Mathf.CeilToInt((float)descriptor.height / (float)ThreadCount);

        int threadGroupsX2 = Mathf.CeilToInt((float)descriptor.height / (float)32);
        int threadGroupsY2 = Mathf.CeilToInt((float)descriptor.height / (float)ThreadCount);

        cmd.SetComputeTextureParam(_computeShader, _kernelIndex_Horizontal, "Source", _destination);
        cmd.SetComputeTextureParam(_computeShader, _kernelIndex_Horizontal, "Output_Horizontal", _tempBuffer_1);

        cmd.SetComputeTextureParam(_computeShader, _kernelIndex_Vertical, "Source", _tempBuffer_1);
        cmd.SetComputeTextureParam(_computeShader, _kernelIndex_Vertical, "Output_Vertical", _tempBuffer_2);
       
        cmd.DispatchCompute(_computeShader, _kernelIndex_Horizontal, threadGroupsX, descriptor.height, 1);
        cmd.DispatchCompute(_computeShader, _kernelIndex_Vertical, descriptor.width, threadGroupsY, 1);


        for(int i=1; i < _blurStep; i++)
        {
            cmd.SetComputeTextureParam(_computeShader, _kernelIndex_Horizontal, "Source", _tempBuffer_2);
            cmd.SetComputeTextureParam(_computeShader, _kernelIndex_Horizontal, "Output_Horizontal", _tempBuffer_1);

            cmd.SetComputeTextureParam(_computeShader, _kernelIndex_Vertical, "Source", _tempBuffer_1);
            cmd.SetComputeTextureParam(_computeShader, _kernelIndex_Vertical, "Output_Vertical", _tempBuffer_2);

            cmd.DispatchCompute(_computeShader, _kernelIndex_Horizontal, threadGroupsX, descriptor.height, 1);
            cmd.DispatchCompute(_computeShader, _kernelIndex_Vertical, descriptor.width, threadGroupsY, 1);
        }

        cmd.Blit(_tempBuffer_2, _destination);
        
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
     
    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(PROPERTY_TEMPBUFFER_1);
        cmd.ReleaseTemporaryRT(PROPERTY_TEMPBUFFER_2);
    }
    
 
    
  

}
