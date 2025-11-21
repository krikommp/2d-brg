using System;
using System.Collections.Generic;
using BRGTest.DynamicObject;
using Unity.Collections;
using UnityEngine;

namespace BRGTest
{
    [ExecuteAlways]
    public class BRGController : MonoBehaviour
    {
        public Camera m_camera;
        public Material m_material;
        public Mesh m_mesh;
        public float m_testOffset = 1.0f;

        public List<Renderer> m_dynamicObjects;
        private List<BRGWorldArea> m_worldAreas = new List<BRGWorldArea>();
        private BRGAreaVisibilityDetector m_areaVisibilityDetector = new BRGAreaVisibilityDetector();
        private BRGAreaSorter m_areaSorter = new BRGAreaSorter();
        private BRGRenderManager m_renderManager = new BRGRenderManager();
        private BRGDynamicObjectInjector m_dynamicObjectInjector = new BRGDynamicObjectInjector();
        private static BRGController s_Instance;
        
        public static BRGController Instance => s_Instance;

        public Vector3 TestGetDynamicPos()
        {
            return m_dynamicObjects[0].transform.position;
        }

        public void RegisterWorldArea(BRGWorldArea area)
        {
            if (!m_worldAreas.Contains(area))
            {
                m_worldAreas.Remove(area);
            }
            m_worldAreas.Add(area);
            m_areaVisibilityDetector.NotifyAreasChanged();
        }

        private void OnEnable()
        {
            if (s_Instance != null)
            {
                DestroyImmediate(this);
            }
            s_Instance = this;
            
            var propRenderer = new BRGPropRenderer_Dynamic(m_mesh, m_material);
            m_renderManager.RegisterRenderer(propRenderer);
            
            m_renderManager.Initialize();

            m_dynamicObjectInjector = new BRGDynamicObjectInjector();
        }

        private void Update()
        {
            bool isDirty = m_areaVisibilityDetector.UpdateActiveAreas(m_camera, m_worldAreas);
            m_dynamicObjectInjector.UpdateDynamicObjects(m_dynamicObjects, m_camera);
            isDirty |= m_dynamicObjectInjector.IsDirty();
            
            if (isDirty)
            {
                var sortedProps = m_areaSorter.SortVisibleAreas(m_areaVisibilityDetector, m_worldAreas);
                m_renderManager.UpdateRenderData(sortedProps, m_dynamicObjectInjector);
            }
        }

        private void OnDisable()
        {
            m_areaSorter?.Dispose();
            m_renderManager?.Dispose();
            m_worldAreas.Clear();

            if (s_Instance == this)
                s_Instance = null;
        }
    }
}