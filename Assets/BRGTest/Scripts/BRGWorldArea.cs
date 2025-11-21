using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace BRGTest
{
    [ExecuteAlways]
    public class BRGWorldArea : MonoBehaviour
    {
        [Header("区域信息")] public Vector2Int gridCoordinate;
        public Bounds areaBounds;
        public int objectCount;
        public bool isActiveArea = false;
        public bool isDisableRenderer = true;

        public List<BRGPackedData> sortedPropDatas = new List<BRGPackedData>();
        public List<GameObject> sortedPropObjects = new List<GameObject>();
        public NativeArray<BRGPackedData> nativePropDatas;

        [SerializeField, HideInInspector] private Color visiblegizmoColor = new Color(0.3f, 0.8f, 0.2f, 0.3f);
        [SerializeField, HideInInspector] private Color hiddengizmoColor = new Color(0.8f, 0.2f, 0.2f, 0.3f);

        private void OnEnable()
        {
            nativePropDatas = new NativeArray<BRGPackedData>(sortedPropObjects.Count, Allocator.Persistent);
            for (int i = 0; i < sortedPropDatas.Count; i++)
            {
                nativePropDatas[i] = sortedPropDatas[i];
            }

            foreach (var propObject in sortedPropObjects)
            {
                if (isDisableRenderer)
                {
                    var renderer = propObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = false;
                    }
                }
            }
            
            if (BRGController.Instance == null)
            {
                Debug.LogError("BRGWorldArea 启用失败：未找到 BRGController 实例，请确保场景中存在 BRGController 组件。");
                return;
            }
            
            BRGController.Instance.RegisterWorldArea(this);
        }

        private void OnDisable()
        {
            if (nativePropDatas.IsCreated)
            {
                nativePropDatas.Dispose();
            }
        }

        private Color gizmoColor => isActiveArea ? visiblegizmoColor : hiddengizmoColor;
        
        public void SetViewState(bool isActive)
        {
            isActiveArea = isActive;
        }

        private void OnDrawGizmos()
        {
            // 绘制区域边界
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(areaBounds.center, areaBounds.size);

            // 绘制填充区域（半透明）
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.1f);
            Gizmos.DrawCube(areaBounds.center, areaBounds.size);

            // 显示区域坐标标签
            GUIStyle style = new GUIStyle();
            style.normal.textColor = gizmoColor;
            style.alignment = TextAnchor.MiddleCenter;
#if UNITY_EDITOR
            UnityEditor.Handles.Label(areaBounds.center, $"Area {gridCoordinate.x},{gridCoordinate.y}", style);
#endif
        }

        public void UpdateObjectCount()
        {
            // 计算该区域内的对象数量
            Collider[] colliders = Physics.OverlapBox(areaBounds.center, areaBounds.extents);
            objectCount = colliders.Length;
        }
    }
}