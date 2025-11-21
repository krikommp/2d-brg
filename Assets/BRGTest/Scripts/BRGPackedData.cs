using System;
using BRGTest.DynamicObject;
using Unity.Mathematics;

namespace BRGTest
{
    [Serializable]
    public struct BRGPackedData : IComparable<BRGPackedData>, IComparable<BRGDynamicObjectData>
    {
        public float4x4 m_unityToWorld;
        public float4 m_color;
        public float3 m_worldPos;

        public float m_distance;
        public int m_layerAndOrder;
        
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