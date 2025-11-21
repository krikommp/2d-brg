using System;
using System.Collections.Generic;
using BRGTest.Batch;
using BRGTest.DynamicObject;
using BRGTest.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BRGTest
{
    public struct BRGRenderRange
    {
        public int m_startIndex;
        public int m_count;
        public EBRGRangeType m_rangeType;
        public float3 m_insertionPosition;
    }

    public enum EBRGRangeType
    {
        BRGInstances,
        DynamicObject
    }
    
    public class BRGPropRenderer_Dynamic : IBRGRenderer
    {
        private BatchRendererGroup m_brg;
        private BRGBatchPool m_batchPool;
        private BRGRenderQueue m_renderQueue;
        private Mesh m_mesh;
        private Material m_material;
        
        private BatchMeshID m_meshID;
        private BatchMaterialID m_materialID;
        
        private Dictionary<int, BRGBatchData> m_activeBatches = new Dictionary<int, BRGBatchData>(); // 当前活跃的批次
        private List<BRGBatchData> m_usedBatches = new List<BRGBatchData>(); // 当前使用的批次
        private List<BRGBatchData> m_batches = new();

        private const int kBatchCapacity = 1024; // 每个 batch 支持的实例数量
        private const int kMaxPoolSize = 32;    // 最大 batch 池大小
        private static readonly Vector3 kBackOffset = new Vector3(0, 0, 1f);

        public BRGPropRenderer_Dynamic(Mesh mesh, Material material)
        {
            m_mesh = mesh;
            m_material = material;
        }

        public void Initialize()
        {
            if (m_brg != null)
            {
                return;
            }
            
            m_renderQueue = new BRGRenderQueue();
            m_brg = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
            
            m_meshID = m_brg.RegisterMesh(m_mesh);
            m_materialID = m_brg.RegisterMaterial(m_material);

            m_batchPool = new BRGBatchPool(
                m_brg,
                initialSize: 8,
                batchCapacity: kBatchCapacity,
                maxPoolSize: kMaxPoolSize);
        }

        public void UpdateRenderData(NativeArray<BRGPackedData> renderData, BRGDynamicObjectInjector dynamicObjectInjector)
        {
            var insertionPoints = dynamicObjectInjector.CalculateInsertionPoints(renderData);
            
            var renderRanges = SplitRenderRanges(renderData, insertionPoints);

            var batches = AssignBatchesToRanges(renderRanges, renderData);
            
            m_renderQueue.BuildRenderQueue(batches, m_materialID, m_meshID);

            UpdateBatchGPUData(batches);
        }
        
        public void Dispose()
        {
            m_batchPool?.Dispose();
        }

        private List<BRGRenderRange> SplitRenderRanges(NativeArray<BRGPackedData> renderData, List<BRGInsertionPoint> insertionPoints)
        {
            var totalInstances = renderData.Length;
            var ranges = new List<BRGRenderRange>();
            int currentStart = 0;
        
            // 对插入点排序并去重
            var sortedInsertions = new SortedSet<BRGInsertionPoint>(insertionPoints);
        
            foreach (var insertion in sortedInsertions)
            {
                if (insertion.m_insertionPoint > currentStart && insertion.m_insertionPoint <= totalInstances)
                {
                    // 添加从currentStart到insertion-1的BRG实例范围
                    ranges.Add(new BRGRenderRange 
                    { 
                        m_startIndex = currentStart, 
                        m_count = insertion.m_insertionPoint - currentStart,
                        m_rangeType = EBRGRangeType.BRGInstances,
                        m_insertionPosition = renderData[insertion.m_insertionPoint - 1].m_worldPos,
                    });
                
                    // 添加插入点（动态物体位置）
                    ranges.Add(new BRGRenderRange 
                    { 
                        m_startIndex = insertion.m_insertionPoint, 
                        m_count = 0, // 动态物体不计入BRG实例
                        m_rangeType = EBRGRangeType.DynamicObject,
                    });
                
                    currentStart = insertion.m_insertionPoint;
                }
            }
        
            // 添加最后一段BRG实例
            if (currentStart < totalInstances)
            {
                ranges.Add(new BRGRenderRange 
                { 
                    m_startIndex = currentStart, 
                    m_count = totalInstances - currentStart,
                    m_rangeType = EBRGRangeType.BRGInstances,
                    m_insertionPosition = renderData[currentStart].m_worldPos,
                });
            }
        
            return ranges;
        }

        private List<BRGBatchData> AssignBatchesToRanges(List<BRGRenderRange> ranges, NativeArray<BRGPackedData> sortedData)
        {
            m_batches = new List<BRGBatchData>();
            var batchesToReturn = new List<BRGBatchData>();
            
            // 收集当前未使用的Batch，准备重用
            foreach (var batch in m_usedBatches)
            {
                // if (!IsBatchStillNeeded(batch, ranges))
                {
                    batchesToReturn.Add(batch);
                }
            }
            
            // 归还不再需要的Batch
            foreach (var batch in batchesToReturn)
            {
                m_usedBatches.Remove(batch);
                m_batchPool.ReturnBatch(batch);
            }
        
            // 为每个范围分配Batch
            foreach (var range in ranges)
            {
                if (range.m_rangeType == EBRGRangeType.BRGInstances && range.m_count > 0)
                {
                    var batch = GetOrCreateBatchForRange(range);
                    if (batch != null)
                    {
                        UpdateBatchData(batch, range, sortedData);
                        m_batches.Add(batch);
                    }
                }
            }
        
            return m_batches;
        }
        
        private BRGBatchData GetOrCreateBatchForRange(BRGRenderRange range)
        {
            BRGBatchData batch = null;
        
            // 尝试查找现有的可用Batch
            foreach (var existingBatch in m_usedBatches)
            {
                if (existingBatch.IsAvailable && existingBatch.m_capacity >= range.m_count)
                {
                    batch = existingBatch;
                    break;
                }
            }
        
            // 如果没有找到合适的现有Batch，从池中获取
            if (batch == null)
            {
                batch = m_batchPool.GetBatch();
                if (batch != null)
                {
                    m_usedBatches.Add(batch);
                }
            }
        
            if (batch != null)
            {
                batch.m_startIndex = range.m_startIndex;
                batch.m_count = range.m_count;
                batch.m_sortingPosition = range.m_insertionPosition;
                batch.lastUsedFrame = DateTime.Now.Ticks;
            }
        
            return batch;
        }
        
        private bool IsBatchStillNeeded(BRGBatchData batch, List<BRGRenderRange> currentRanges)
        {
            // 检查Batch是否仍在当前渲染范围内使用
            foreach (var range in currentRanges)
            {
                if (range.m_rangeType == EBRGRangeType.BRGInstances && 
                    range.m_startIndex == batch.m_startIndex && 
                    range.m_count == batch.m_count)
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateBatchData(BRGBatchData batch, BRGRenderRange range, NativeArray<BRGPackedData> sortedData)
        {
            var updateJob = new BRGUpdateBatchDataJob
            {
                m_packedData = sortedData,
                m_sourceStartIndex = range.m_startIndex,
                m_count = range.m_count,
                m_capacity = batch.m_capacity,
                m_systemBuffer = batch.m_systemBuffer,
            };
            
            var jobHandle = updateJob.Schedule(range.m_count, 64);
            jobHandle.Complete();
        }

        private void UpdateBatchGPUData(List<BRGBatchData> batches)
        {
            foreach (var batch in batches)
            {
                if (batch.m_count == 0)
                {
                    continue;
                }

                int dataSize = batch.m_capacity * ((BRGBatchPool.kSizeOfPackedMatrix + BRGBatchPool.kSizeOfFloat4) / BRGBatchPool.kSizeOfFloat4);
                batch.m_instanceData.SetData(batch.m_systemBuffer, 0, batch.m_systemBufferOffset, dataSize);
            }
        }

        public string RendererType => "BRGPropRenderer";
        
        public unsafe JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
        {
            var jobHandle = new JobHandle();
            
            if (m_batches.Count <= 0)
            {
                return jobHandle;
            }

            BatchCullingOutputDrawCommands drawCommands = new BatchCullingOutputDrawCommands();

            int drawCommandCount = m_batches.Count;
            drawCommands.drawCommandCount = drawCommandCount;

            int drawRangeCount = 1;
            drawCommands.drawRangeCount = drawRangeCount;
            drawCommands.drawRanges = Malloc<BatchDrawRange>((uint)drawRangeCount);
            drawCommands.drawRanges[0] = new BatchDrawRange
            {
                drawCommandsBegin = 0,
                // drawCommandsCount = (uint)drawCommandCount,
                drawCommandsCount = 1,
                filterSettings = new BatchFilterSettings
                {
                    renderingLayerMask = 1,
                    layer = 0,
                    motionMode = MotionVectorGenerationMode.Camera,
                    shadowCastingMode = ShadowCastingMode.Off,
                    receiveShadows = true,
                    staticShadowCaster = false,
                    allDepthSorted = false
                }
            };

            int visibleCount = kBatchCapacity;
            drawCommands.visibleInstances = Malloc<int>((uint)visibleCount);
            for (int i = 0; i < visibleCount; i++)
            {
                drawCommands.visibleInstances[i] = i;
            }

            int totalInstanceSortingPositionCount = 3 * drawCommandCount;
            drawCommands.instanceSortingPositions = Malloc<float>((uint)totalInstanceSortingPositionCount);
            drawCommands.instanceSortingPositionFloatCount = totalInstanceSortingPositionCount;
            var test = BRGController.Instance.TestGetDynamicPos();
            for (int i = 0; i < drawCommandCount; ++i)
            {
                drawCommands.instanceSortingPositions[i * 3 + 0] = test.x;
                drawCommands.instanceSortingPositions[i * 3 + 1] = test.y;
                drawCommands.instanceSortingPositions[i * 3 + 2] = test.z + BRGController.Instance.m_testOffset;
            }
            drawCommands.drawCommands = Malloc<BatchDrawCommand>((uint)drawCommandCount);
            for (int i = 0; i < m_batches.Count; ++i)
            {
                int batchIndex = i;
                var batch = m_batches[batchIndex];
                drawCommands.drawCommands[i] = new BatchDrawCommand
                {
                    visibleOffset = (uint)0,
                    visibleCount = (uint)batch.m_count,
                    batchID = batch.m_batchId,
                    materialID = m_materialID,
                    meshID = m_meshID,
                    submeshIndex = 0,
                    splitVisibilityMask = 0xff,
                    flags = BatchDrawCommandFlags.HasSortingPosition,
                    sortingPosition = i * 3,
                };
            }

            cullingOutput.drawCommands[0] = drawCommands;
            return jobHandle;
        }
        
        private static unsafe T* Malloc<T>(uint count) where T : unmanaged
        {
            return (T*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<T>() * count,
                UnsafeUtility.AlignOf<T>(),
                Allocator.TempJob);
        }
    }
}