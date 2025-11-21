using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace BRGTest.Jobs
{
    public struct BRGUpdateRenderDataJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<BRGPackedData> m_packedData;
        [WriteOnly] public NativeArray<float3x4> m_objectToWorldMatrices;
        [WriteOnly] public NativeArray<float4> m_colors; 
        public int m_visibleCount;
        
        public void Execute(int index)
        {
            if (index >= m_visibleCount) return;
        
            var data = m_packedData[index];
        
            var matrix = data.m_unityToWorld;
            m_objectToWorldMatrices[index] = new float3x4(
                matrix.c0.xyz, matrix.c1.xyz, matrix.c2.xyz, matrix.c3.xyz
            );
        
            m_colors[index] = data.m_color;
        }
    }
}