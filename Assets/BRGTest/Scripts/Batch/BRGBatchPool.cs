using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BRGTest.Batch
{
    [Flags]
    public enum EBatchState : byte
    {
        Available = 0,
        InUse = 1 << 0,
        Dirty = 1 << 1,
        Reserved = 1 << 2
    }
    
    public class BRGBatchPool : IDisposable
    {
        private BatchRendererGroup m_brg;
        private Stack<BRGBatchData> m_availableBatches;
        private List<BRGBatchData> m_allBatches;
        private int m_maxPoolSize;
        private int m_batchCapacity;
    
        private GraphicsBuffer m_persistentBuffer;
        private int m_nextBufferOffset;
        
        public const int kSizeOfFloat4 = sizeof(float) * 4;
        public const int kSizeOfMatrix = kSizeOfFloat4 * 4;
        public const int kSizeOfPackedMatrix = kSizeOfFloat4 * 3;
        public const int kBytesPerInstance = kSizeOfPackedMatrix + kSizeOfFloat4;
        
        public BRGBatchPool(BatchRendererGroup brg, int initialSize, int batchCapacity, int maxPoolSize)
        {
            m_brg = brg;
            m_maxPoolSize = maxPoolSize;
            m_batchCapacity = batchCapacity;
            m_availableBatches = new Stack<BRGBatchData>(initialSize);
            m_allBatches = new List<BRGBatchData>(maxPoolSize);
        
            InitializePersistentBuffer();
            PreallocateBatches(initialSize);
        }
        
        public BRGBatchData GetBatch()
        {
            BRGBatchData batch = null;
        
            // 首先尝试从池中获取可用Batch
            if (m_availableBatches.Count > 0)
            {
                batch = m_availableBatches.Pop();
            }
            // 如果池中无可用且未达上限，创建新Batch
            else if (m_allBatches.Count < m_maxPoolSize)
            {
                batch = CreateBatch();
                m_allBatches.Add(batch);
            }
        
            if (batch != null)
            {
                batch.m_state = EBatchState.InUse;
            }
        
            return batch;
        }
        
        public void ReturnBatch(BRGBatchData batch)
        {
            if (batch == null) return;
        
            // 重置Batch状态
            batch.m_state = EBatchState.Available;
            
            // 回收到池中
            m_availableBatches.Push(batch);
        }
        
        private void InitializePersistentBuffer()
        {
            // 计算单个Batch需要的内存
            int perBatchSize = CalculatePerBatchSize();
            int totalSize = perBatchSize * m_maxPoolSize;
        
            m_persistentBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, totalSize / sizeof(float), sizeof(float));
        }
        
        private int CalculatePerBatchSize()
        {
            int matrixSize = m_batchCapacity * kSizeOfPackedMatrix;
            int colorSize = m_batchCapacity * kSizeOfFloat4;
            return matrixSize + colorSize;
        }
        
        private void PreallocateBatches(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var batch = CreateBatch();
                SetupBatchMetaData(batch);
                
                m_availableBatches.Push(batch);
                m_allBatches.Add(batch);
            }
        }
        
        private BRGBatchData CreateBatch()
        {
            int batchSize = CalculatePerBatchSize();
            int bufferOffset = m_nextBufferOffset;
            m_nextBufferOffset += batchSize;
        
            return new BRGBatchData
            {
                m_startIndex = 0,
                m_count = 0,
                m_capacity = m_batchCapacity,
                m_state = EBatchState.Available,
                m_instanceDataOffset = bufferOffset,
                m_instanceData = m_persistentBuffer,
                m_systemBuffer = new NativeArray<float4>(batchSize / kSizeOfFloat4, Allocator.Persistent, NativeArrayOptions.UninitializedMemory),
                m_systemBufferOffset = bufferOffset / kSizeOfFloat4,
                m_batchId = default,
                m_sortingPosition = Vector3.zero,
                m_sortingDistance = 0,
            };
        }
        
        private void SetupBatchMetaData(BRGBatchData batchData)
        {
            int objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
            int baseColorID = Shader.PropertyToID("_BaseColor");

            var batchMetadata = new NativeArray<MetadataValue>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            batchMetadata[0] = CreateMetadataValue(objectToWorldID, batchData.m_instanceDataOffset, true);
            batchMetadata[1] = CreateMetadataValue(baseColorID, batchData.m_instanceDataOffset + kSizeOfPackedMatrix * batchData.m_capacity, true);
            
            batchData.m_batchId = m_brg.AddBatch(batchMetadata, m_persistentBuffer.bufferHandle, 0, 0);

            batchMetadata.Dispose();
        }
        
        private MetadataValue CreateMetadataValue(int nameID, int gpuOffset, bool isPerInstance)
        {
            const uint kIsPerInstanceBit = 0x80000000;
            return new MetadataValue
            {
                NameID = nameID,
                Value = (uint)gpuOffset | (isPerInstance ? (kIsPerInstanceBit) : 0),
            };
        }
        
        public void Dispose()
        {
            foreach (var batch in m_allBatches)
            {
                batch.Dispose();
            }
            
            m_availableBatches?.Clear();
            m_allBatches?.Clear();
            m_persistentBuffer?.Dispose();
        }
    }
}