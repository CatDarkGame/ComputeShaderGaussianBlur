using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ComputeShaderRenderPass : ScriptableRenderPass
{
    private static readonly string PASS_TAG = "ComputeShaderRenderPass";
    private static readonly int PROPERTY_TEMPBUFFER = Shader.PropertyToID("_TempBuffer"); // 임시렌더텍스처 변수명
    
    private RenderTargetIdentifier _destination;  // 화면렌더텍스처(카메라)
    private RenderTargetIdentifier _tempBuffer = new RenderTargetIdentifier(PROPERTY_TEMPBUFFER); // 임시렌더텍스처
    private ComputeShader _computeShader;
    private Vector2Int _threadGroups;
    private int _kernelIndex = -1;
    private RenderTextureFormat _format;
    private int _rwTexturePropertyID = -1;

    public ComputeShaderRenderPass(RenderPassEvent renderPassEvent)
    {
        this.renderPassEvent = renderPassEvent;
    }
    
    public void Setup(RenderTargetIdentifier renderTargetDestination)
    {
        _destination = renderTargetDestination;
    }

    public void SetComputeShader(ComputeShader computeShader, string kernelName, int rwTexturePropertyID, RenderTextureFormat format)
    {
        _computeShader = computeShader;
        _format = format;
        _kernelIndex = computeShader.FindKernel(kernelName);
        _rwTexturePropertyID = rwTexturePropertyID;
       /* _threadGroups = new Vector2Int(Mathf.CeilToInt(_renderTexture.width / threadGroupSizeX),
                                        Mathf.CeilToInt(_renderTexture.height / threadGroupSizeY));
        computeShader.SetTexture(_kernelIndex, _property_rwOutput, _renderTexture);*/
    }
    
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (!_computeShader) return;

        CommandBuffer cmd = CommandBufferPool.Get(PASS_TAG);
        CameraData cameraData = renderingData.cameraData;
        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(cameraData.camera.scaledPixelWidth, cameraData.camera.scaledPixelHeight);
        descriptor.colorFormat = _format;
        descriptor.useMipMap = false;
        descriptor.enableRandomWrite = true;    

        cmd.GetTemporaryRT(PROPERTY_TEMPBUFFER, descriptor, FilterMode.Bilinear);
        cmd.Blit(_destination, _tempBuffer);


        _computeShader.GetKernelThreadGroupSizes(_kernelIndex, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
        int threadGroupsX = Mathf.CeilToInt(descriptor.width / threadGroupSizeX);
        int threadGroupsY = Mathf.CeilToInt(descriptor.height / threadGroupSizeY);
        cmd.SetComputeTextureParam(_computeShader, _kernelIndex, _rwTexturePropertyID, _tempBuffer);
        cmd.DispatchCompute(_computeShader, _kernelIndex, threadGroupsX, threadGroupsY, 1);
        

        cmd.Blit(_tempBuffer, _destination);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    
    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(PROPERTY_TEMPBUFFER);
    }
    
 
    
  

}
