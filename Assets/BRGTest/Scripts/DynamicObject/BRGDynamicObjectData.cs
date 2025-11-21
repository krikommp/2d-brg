using System;
using UnityEngine;

namespace BRGTest.DynamicObject
{
    public class BRGDynamicObjectData : IComparable<BRGPackedData>, IComparable<BRGDynamicObjectData>
    {
        public int m_instanceId;
        public int m_sortingHash;
        public float m_distance;
        public int m_layerAndOrder;
        public int m_insertionIndex;
        public Vector3 m_position;
        
        public int CompareTo(BRGPackedData other)
        {
            if (other.m_layerAndOrder != m_layerAndOrder)
            {
                return m_layerAndOrder.CompareTo(other.m_layerAndOrder);
            }
            
            return m_distance.CompareTo(other.m_distance);
        }

        public int CompareTo(BRGDynamicObjectData other)
        {
            if (other.m_layerAndOrder != m_layerAndOrder)
            {
                return m_layerAndOrder.CompareTo(other.m_layerAndOrder);
            }
            
            return m_distance.CompareTo(other.m_distance);
        }
    }
}