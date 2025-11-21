using System.Collections.Generic;
using BRGTest.Jobs;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace BRGTest
{
    public struct GridRowIndices
    {
        public int m_startIndex;
        public int m_length;
        public NativeArray<BRGPackedData> m_rowData;
    }
    
    public class BRGAreaSorter
    {
        private NativeArray<BRGPackedData> m_finalResult;
        private bool m_isInitialized = false;

        // 横排数据缓存
        private NativeArray<BRGPackedData>[] m_rowArrays = new NativeArray<BRGPackedData>[3];
        private int[] m_rowLengths = new int[3];

        public void Initialize()
        {
            m_isInitialized = true;
        }

        public NativeArray<BRGPackedData> SortVisibleAreas(BRGAreaVisibilityDetector detector,
            List<BRGWorldArea> worldAreas)
        {
            if (!m_isInitialized) Initialize();

            // 1. 获取九宫格内的活动区域
            var activeAreas = GetActiveAreasInGrid(detector, worldAreas);
            if (activeAreas.Count == 0)
                return new NativeArray<BRGPackedData>(0, Allocator.Temp);

            // 2. 按横排分组
            var rows = GroupAreasByRows(activeAreas);
            if (rows.Length < 3)
            {
                Debug.LogWarning("活动区域不足三行，跳过排序，直接合并");
                return MergeRemainingRows(rows);
            }

            // 3. 并行排序三个横排
            var sortHandles = SortRowsInParallel(rows);

            // 4. 三路归并
            var finalResult = MergeSortedRows(rows, sortHandles);

            // 清理临时数据
            CleanupTempData(rows);

            return finalResult;
        }

        private List<BRGWorldArea> GetActiveAreasInGrid(BRGAreaVisibilityDetector detector,
            List<BRGWorldArea> worldAreas)
        {
            var activeAreas = new List<BRGWorldArea>();
            foreach (var area in worldAreas)
            {
                if (detector.IsAreaActive(area))
                {
                    activeAreas.Add(area);
                }
            }

            return activeAreas;
        }

        private GridRowIndices[] GroupAreasByRows(List<BRGWorldArea> activeAreas)
        {
            // 按Y坐标分组（三个横排）
            var rows = new Dictionary<int, List<BRGWorldArea>>();

            foreach (var area in activeAreas)
            {
                int rowY = area.gridCoordinate.y;
                if (!rows.ContainsKey(rowY))
                    rows[rowY] = new List<BRGWorldArea>();
                rows[rowY].Add(area);
            }

            // 转换为排序后的横排数组
            var rowList = new List<GridRowIndices>();
            foreach (var kvp in rows)
            {
                int totalSize = 0;
                foreach (var area in kvp.Value)
                    totalSize += area.nativePropDatas.Length;

                var rowData = new NativeArray<BRGPackedData>(totalSize, Allocator.TempJob);
                int currentIndex = 0;

                foreach (var area in kvp.Value)
                {
                    NativeArray<BRGPackedData>.Copy(area.nativePropDatas, 0,
                        rowData, currentIndex, area.nativePropDatas.Length);
                    currentIndex += area.nativePropDatas.Length;
                }

                rowList.Add(new GridRowIndices
                {
                    m_startIndex = 0,
                    m_length = totalSize,
                    m_rowData = rowData
                });
            }

            return rowList.ToArray();
        }

        private NativeArray<JobHandle> SortRowsInParallel(GridRowIndices[] rows)
        {
            var handles = new NativeArray<JobHandle>(3, Allocator.Temp);

            // 并行调度三个横排的排序任务
            for (int i = 0; i < 3; i++)
            {
                var sortJob = new BRGRowSortJob
                { 
                    m_data = rows[i].m_rowData,
                };
                handles[i] = sortJob.Schedule();
            }

            return handles;
        }

        private NativeArray<BRGPackedData> MergeSortedRows(GridRowIndices[] rows, NativeArray<JobHandle> sortHandles)
        {
            // 等待所有排序完成
            JobHandle.CombineDependencies(sortHandles).Complete();

            int totalSize = rows[0].m_length + rows[1].m_length + rows[2].m_length;
            var finalResult = new NativeArray<BRGPackedData>(totalSize, Allocator.Persistent);

            // 执行三路归并
            var mergeJob = new BRGThreeWayMergeJob
            {
                m_firstRow = rows[0].m_rowData,
                m_secondRow = rows[1].m_rowData,
                m_thirdRow = rows[2].m_rowData,
                m_result = finalResult
            };

            mergeJob.Schedule().Complete();

            return finalResult;
        }

        private NativeArray<BRGPackedData> MergeRemainingRows(GridRowIndices[] rows)
        {
            // 处理行数不足3个的情况
            int totalSize = 0;
            foreach (var row in rows)
                totalSize += row.m_length;

            var result = new NativeArray<BRGPackedData>(totalSize, Allocator.Persistent);
            int currentIndex = 0;

            foreach (var row in rows)
            {
                NativeArray<BRGPackedData>.Copy(row.m_rowData, 0, result, currentIndex, row.m_length);
                currentIndex += row.m_length;
            }

            return result;
        }

        private void CleanupTempData(GridRowIndices[] rows)
        {
            foreach (var row in rows)
            {
                if (row.m_rowData.IsCreated)
                    row.m_rowData.Dispose();
            }
        }

        public void Dispose()
        {
            if (m_finalResult.IsCreated)
                m_finalResult.Dispose();

            for (int i = 0; i < 3; i++)
            {
                if (m_rowArrays[i].IsCreated)
                    m_rowArrays[i].Dispose();
            }
        }
    }
}