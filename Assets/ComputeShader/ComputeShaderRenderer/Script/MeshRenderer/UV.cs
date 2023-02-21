using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ComputeShaderRenderer
{
    public class UV : ComputeShaderRenderer
    {
        protected override void OnEnable()
        {
            _renderTextureFormat = RenderTextureFormat.RG16;
            base.OnEnable();
        }
    }

}