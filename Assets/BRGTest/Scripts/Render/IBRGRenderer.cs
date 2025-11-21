using System;
using System.Collections.Generic;
using BRGTest.DynamicObject;
using Unity.Collections;
using UnityEngine;

namespace BRGTest
{
    public interface IBRGRenderer : IDisposable
    {
        string RendererType { get; }
    
        void Initialize();
        void UpdateRenderData(NativeArray<BRGPackedData> renderData, BRGDynamicObjectInjector dynamicObjectInjector);
    }
}