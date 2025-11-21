using Unity.Collections;
using Unity.Jobs;

namespace BRGTest.Jobs
{
    public struct BRGThreeWayMergeJob : IJob
    {
        [ReadOnly] public NativeArray<BRGPackedData> m_firstRow;
        [ReadOnly] public NativeArray<BRGPackedData> m_secondRow;
        [ReadOnly] public NativeArray<BRGPackedData> m_thirdRow;
        [WriteOnly] public NativeArray<BRGPackedData> m_result;
        
        public void Execute()
        {
            int i = 0, j = 0, k = 0, index = 0;
            int firstLen = m_firstRow.Length;
            int secondLen = m_secondRow.Length;
            int thirdLen = m_thirdRow.Length;
        
            // 三路归并
            while (i < firstLen && j < secondLen && k < thirdLen)
            {
                var first = m_firstRow[i];
                var second = m_secondRow[j];
                var third = m_thirdRow[k];
            
                if (first.CompareTo(second) <= 0 && first.CompareTo(third) <= 0)
                {
                    m_result[index++] = first;
                    i++;
                }
                else if (second.CompareTo(first) <= 0 && second.CompareTo(third) <= 0)
                {
                    m_result[index++] = second;
                    j++;
                }
                else
                {
                    m_result[index++] = third;
                    k++;
                }
            }
        
            // 处理两路归并
            while (i < firstLen && j < secondLen)
            {
                if (m_firstRow[i].CompareTo(m_secondRow[j]) <= 0)
                {
                    m_result[index++] = m_firstRow[i++];
                }
                else
                {
                    m_result[index++] = m_secondRow[j++];
                }
            }
        
            while (i < firstLen && k < thirdLen)
            {
                if (m_firstRow[i].CompareTo(m_thirdRow[k]) <= 0)
                {
                    m_result[index++] = m_firstRow[i++];
                }
                else
                {
                    m_result[index++] = m_thirdRow[k++];
                }
            }
        
            while (j < secondLen && k < thirdLen)
            {
                if (m_secondRow[j].CompareTo(m_thirdRow[k]) <= 0)
                {
                    m_result[index++] = m_secondRow[j++];
                }
                else
                {
                    m_result[index++] = m_thirdRow[k++];
                }
            }
        
            // 处理单路剩余
            while (i < firstLen) m_result[index++] = m_firstRow[i++];
            while (j < secondLen) m_result[index++] = m_secondRow[j++];
            while (k < thirdLen) m_result[index++] = m_thirdRow[k++];
        }
    }
}