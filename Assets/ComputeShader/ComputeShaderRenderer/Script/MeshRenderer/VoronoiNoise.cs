using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComputeShaderRenderer
{
    public class VoronoiNoise : ComputeShaderRenderer
    {
        protected override void OnEnable()
        {
            _renderTextureFormat = RenderTextureFormat.R8;
            base.OnEnable();
        }
    }

}