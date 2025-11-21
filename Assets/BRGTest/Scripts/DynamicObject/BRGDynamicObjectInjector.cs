using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace BRGTest.DynamicObject
{
    public class BRGInsertionPoint : IComparable<BRGInsertionPoint>
    {
        public int m_insertionPoint;
        public Vector3 m_insertionPosition;
        
        public int CompareTo(BRGInsertionPoint other)
        {
            return m_insertionPoint.CompareTo(other.m_insertionPoint);
        }
    }

    public class BRGDynamicObjectInjector
    {
        private List<BRGDynamicObjectData> m_dynamicObjects = new List<BRGDynamicObjectData>();
        private List<BRGInsertionPoint> m_insertionPoints = new List<BRGInsertionPoint>();
        private Dictionary<int, int> m_previousSortingHashes = new Dictionary<int, int>(); // 存储上一帧的排序哈希值
        private bool m_isDirty = true;
        
        public void UpdateDynamicObjects(List<Renderer> dynamicObjects, Camera camera)
        {
            bool hasChanges = CheckForChanges(dynamicObjects, camera);
            
            if (!hasChanges && m_dynamicObjects.Count == dynamicObjects.Count) 
            {
                m_isDirty = false;
                return;
            }
            
            UpdateDynamicObjectsData(dynamicObjects, camera);
            m_isDirty = true;
        }
        
        public List<BRGInsertionPoint> GetInsertionPoints()
        {
            return m_insertionPoints;
        }
        
        public List<BRGDynamicObjectData> GetDynamicObjectsBetween(int startIndex, int endIndex)
        {
            var result = new List<BRGDynamicObjectData>();
        
            for (int i = 0; i < m_dynamicObjects.Count; i++)
            {
                if (m_insertionPoints[i].m_insertionPoint > startIndex && m_insertionPoints[i].m_insertionPoint <= endIndex)
                {
                    result.Add(m_dynamicObjects[i]);
                }
            }
        
            return result;
        }
    
        public void MarkDirty()
        {
            m_isDirty = true;
        }
        
        public bool IsDirty()
        {
            return m_isDirty;
        }

        public List<BRGInsertionPoint> CalculateInsertionPoints(NativeArray<BRGPackedData> sortedData)
        {
            m_insertionPoints.Clear();
            
            foreach (var dynamicObj in m_dynamicObjects)
            {
                int insertIndex = FindInsertionIndex(sortedData, dynamicObj);
                m_insertionPoints.Add(new BRGInsertionPoint
                {
                    m_insertionPoint = insertIndex,
                    m_insertionPosition = dynamicObj.m_position
                });
                dynamicObj.m_insertionIndex = insertIndex;
            }

            return m_insertionPoints;
        }
        
        private void UpdateDynamicObjectsData(List<Renderer> dynamicObjects, Camera camera)
        {
            m_dynamicObjects.Clear();
            var newSortingHashes = new Dictionary<int, int>();
        
            foreach (var dynamicObject in dynamicObjects)
            {
                BRGRendererSortingComparer.EvaluateObjectDepth(dynamicObject, camera, out float distanceAlongView, out float distanceForSort);
                int layerAndOrder = BRGRendererSortingComparer.CalculateLayerAndOrder(dynamicObject);
                int instanceId = dynamicObject.GetInstanceID();
            
                var dynamicData = new BRGDynamicObjectData
                {
                    m_instanceId = instanceId,
                    m_distance = distanceForSort,
                    m_layerAndOrder = layerAndOrder,
                    m_position = dynamicObject.gameObject.transform.position,
                    m_sortingHash = CalculateSortingHash(distanceForSort, layerAndOrder)
                };
            
                m_dynamicObjects.Add(dynamicData);
                newSortingHashes[instanceId] = dynamicData.m_sortingHash;
            }
        
            // 更新哈希缓存
            m_previousSortingHashes = newSortingHashes;
        }
        
        private bool CheckForChanges(List<Renderer> dynamicObjects, Camera camera)
        {
            // 首先检查数量变化（最简单的情况）
            if (dynamicObjects.Count != m_dynamicObjects.Count)
                return true;
        
            // 检查每个物体的排序属性是否发生变化
            for (int i = 0; i < dynamicObjects.Count; i++)
            {
                var dynamicObject = dynamicObjects[i];
                int instanceId = dynamicObject.GetInstanceID();
            
                // 计算当前帧的排序属性
                BRGRendererSortingComparer.EvaluateObjectDepth(dynamicObject, camera, out float distanceAlongView, out float distanceForSort);
                int layerAndOrder = BRGRendererSortingComparer.CalculateLayerAndOrder(dynamicObject);
            
                // 计算当前排序属性的哈希值
                int currentHash = CalculateSortingHash(distanceForSort, layerAndOrder);
            
                // 检查是否是新增物体
                if (!m_previousSortingHashes.ContainsKey(instanceId))
                    return true;
            
                // 检查排序属性是否发生变化
                if (m_previousSortingHashes[instanceId] != currentHash)
                    return true;
            }
        
            return false;
        }
        
        private int CalculateSortingHash(float distance, int layerAndOrder)
        {
            // 使用简单的哈希组合，确保排序属性变化时哈希值一定不同
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + distance.GetHashCode();
                hash = hash * 31 + layerAndOrder.GetHashCode();
                return hash;
            }
        }

        private int FindInsertionIndex(NativeArray<BRGPackedData> sortedData, BRGDynamicObjectData dynamicObjectData)
        {
            int left = 0;
            int right = sortedData.Length - 1;
        
            while (left <= right)
            {
                int mid = (left + right) / 2;
            
                if (sortedData[mid].CompareTo(dynamicObjectData) < 0)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }
        
            return left;
        }
    }
}