using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace BRGTest.Jobs
{
    public struct BRGUpdateBatchDataJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<BRGPackedData> m_packedData;
        [ReadOnly] public int m_sourceStartIndex;
        [ReadOnly] public int m_count;
        [ReadOnly] public int m_capacity;
        [NativeDisableContainerSafetyRestriction] [WriteOnly] public NativeArray<float4> m_systemBuffer;
        
        public void Execute(int index)
        {
            if (index >= m_count) return;

            int sourceIndex = m_sourceStartIndex + index;
            var data = m_packedData[sourceIndex];

            var matrix = data.m_unityToWorld;
            m_systemBuffer[index * 3 + 0] = new float4(matrix.c0.x, matrix.c0.y, matrix.c0.z, matrix.c1.x);
            m_systemBuffer[index * 3 + 1] = new float4(matrix.c1.y, matrix.c1.z, matrix.c2.x, matrix.c2.y);
            m_systemBuffer[index * 3 + 2] = new float4(matrix.c2.z, matrix.c3.x, matrix.c3.y, matrix.c3.z);
            m_systemBuffer[m_capacity * 3 + index] = data.m_color;
        }
    }
}