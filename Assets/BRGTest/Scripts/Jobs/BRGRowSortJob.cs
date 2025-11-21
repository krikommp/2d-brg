using Unity.Collections;
using Unity.Jobs;

namespace BRGTest.Jobs
{
    public struct BRGRowSortJob : IJob
    {
        public NativeArray<BRGPackedData> m_data;

        public void Execute()
        {
            MergeSortByLayerAndDistance(m_data, 0, m_data.Length - 1);
        }

        private void MergeSortByLayerAndDistance(NativeArray<BRGPackedData> array, int left, int right)
        {
            if (left < right)
            {
                int mid = (left + right) / 2;
                MergeSortByLayerAndDistance(array, left, mid);
                MergeSortByLayerAndDistance(array, mid + 1, right);
                MergeByLayerAndDistance(array, left, mid, right);
            }
        }

        private void MergeByLayerAndDistance(NativeArray<BRGPackedData> array, int left, int mid, int right)
        {
            int n1 = mid - left + 1;
            int n2 = right - mid;

            // 使用临时数组（避免多次分配）
            var leftArray = new NativeArray<BRGPackedData>(n1, Allocator.Temp);
            var rightArray = new NativeArray<BRGPackedData>(n2, Allocator.Temp);

            // 复制数据
            for (int i = 0; i < n1; i++)
                leftArray[i] = array[left + i];
            for (int j = 0; j < n2; j++)
                rightArray[j] = array[mid + 1 + j];

            // 归并
            int x = 0, y = 0, k = left;
            while (x < n1 && y < n2)
            {
                if (leftArray[x].CompareTo(rightArray[y]) <= 0)
                {
                    array[k] = leftArray[x];
                    x++;
                }
                else
                {
                    array[k] = rightArray[y];
                    y++;
                }

                k++;
            }

            // 复制剩余元素
            while (x < n1)
            {
                array[k] = leftArray[x];
                x++;
                k++;
            }

            while (y < n2)
            {
                array[k] = rightArray[y];
                y++;
                k++;
            }

            leftArray.Dispose();
            rightArray.Dispose();
        }
    }
}