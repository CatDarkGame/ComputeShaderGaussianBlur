using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComputeShaderRenderer
{
    public class ComputeShaderRenderer : MonoBehaviour
    {
        public ComputeShader computeShader;

        protected Renderer _renderer;
        protected Material _material;
        [HideInInspector][SerializeField] protected Material _sharedMaterial;

        protected RenderTexture _renderTexture;
        protected Vector2Int _textureSize = new Vector2Int(1024, 1024);
        protected RenderTextureFormat _renderTextureFormat = RenderTextureFormat.ARGB32;

        protected int _kernelIndex = -1;
        protected Vector2Int _threadGroups;

        protected string _kernelName = "CSRTTest";
        protected int _property_rwOutput = Shader.PropertyToID("_output");
        protected int _property_shaderTex = Shader.PropertyToID("_ComputeTex");


        protected virtual void OnEnable()
        {
            if (!computeShader) return;

            if (!_renderer) _renderer = GetComponent<Renderer>();
            if (_renderer && _material == null) _material = new Material(_renderer.sharedMaterial);
            if (_material) _material.hideFlags = HideFlags.HideAndDontSave;

            if (_renderTexture) _renderTexture.Release();
            _renderTexture = new RenderTexture(_textureSize.x, _textureSize.y, 0, _renderTextureFormat, RenderTextureReadWrite.Linear);
            _renderTexture.enableRandomWrite = true;
            _renderTexture.Create();

            _kernelIndex = computeShader.FindKernel(_kernelName);
            computeShader.GetKernelThreadGroupSizes(_kernelIndex, out uint threadGroupSizeX, out uint threadGroupSizeY, out _);
            _threadGroups = new Vector2Int(Mathf.CeilToInt(_renderTexture.width / threadGroupSizeX),
                                            Mathf.CeilToInt(_renderTexture.height / threadGroupSizeY));
            computeShader.SetTexture(_kernelIndex, _property_rwOutput, _renderTexture);
        }

        protected virtual void OnDisable()
        {
            if (_renderTexture) _renderTexture.Release();
            _renderTexture = null;
            _material = null;
        }

        protected virtual void Update()
        {
            if (!computeShader) return;
            if (!_renderTexture) return;

            if (computeShader) computeShader.Dispatch(_kernelIndex, _threadGroups.x, _threadGroups.y, 1);
            if (_material) _material.SetTexture(_property_shaderTex, _renderTexture);
            if (_renderer) _renderer.material = _material;
        }

    }
}