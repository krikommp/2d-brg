using System.Collections.Generic;
using BRGTest.DynamicObject;
using Unity.Collections;
using UnityEngine;

namespace BRGTest
{
    public class BRGRenderManager
    {
        private List<IBRGRenderer> m_renderers = new List<IBRGRenderer>();
        private bool m_isInitialized = false;

        public void Initialize()
        {
            if (m_isInitialized) return;
            
            foreach (var renderer in m_renderers)
            {
                renderer.Initialize();
            }
        
            m_isInitialized = true;
            Debug.Log("BRG渲染管理器初始化完成");
        }
        
        public void RegisterRenderer(IBRGRenderer renderer)
        {
            if (renderer == null) return;
        
            if (!m_renderers.Contains(renderer))
            {
                m_renderers.Add(renderer);
            
                if (m_isInitialized)
                {
                    renderer.Initialize();
                }
            }
        }
    
        public void UnregisterRenderer(IBRGRenderer renderer)
        {
            if (renderer != null)
            {
                m_renderers.Remove(renderer);
                renderer.Dispose();
            }
        }
    
        public void UpdateRenderData(NativeArray<BRGPackedData> sortedData, BRGDynamicObjectInjector dynamicObjectInjector)
        {
            if (!m_isInitialized) return;
        
            foreach (var renderer in m_renderers)
            {
                renderer.UpdateRenderData(sortedData, dynamicObjectInjector);
            }
        }

        public void Dispose()
        {
            foreach (var renderer in m_renderers)
            {
                renderer.Dispose();
            }
            m_renderers.Clear();
            m_isInitialized = false;
        }
    }
}