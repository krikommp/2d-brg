using Unity.Collections;
using Unity.Mathematics;

namespace BRGTest
{
    public class BRGRenderData
    {
        public NativeArray<float3x4> m_objectToWorldMatrices;
        public NativeArray<float4> m_colors;
        public int m_visibleCount;

        public bool IsValid => m_objectToWorldMatrices.IsCreated && m_colors.IsCreated;

        public BRGRenderData(int maxInstances)
        {
            m_objectToWorldMatrices = new NativeArray<float3x4>(maxInstances, Allocator.Persistent);
            m_colors = new NativeArray<float4>(maxInstances, Allocator.Persistent);
            m_visibleCount = maxInstances;
        }
        
        public void Dispose()
        {
            if (m_objectToWorldMatrices.IsCreated)
            {
                m_objectToWorldMatrices.Dispose();
            }

            if (m_colors.IsCreated)
            {
                m_colors.Dispose();
            }
        }
    }
}