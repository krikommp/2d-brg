using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BRGTest.Batch
{
    public class BRGBatchData : IDisposable
    {
        public int m_startIndex;
        public int m_count;
        public int m_capacity;
        public EBatchState m_state;
        public Vector3 m_sortingPosition;
        public float m_sortingDistance;
        public BatchID m_batchId;
        
        public GraphicsBuffer m_instanceData;
        public int m_instanceDataOffset;
        public NativeArray<float4> m_systemBuffer;
        public int m_systemBufferOffset;
        
        public long lastUsedFrame;
    
        public bool IsAvailable => m_state == EBatchState.Available;


        public void Dispose()
        {
            m_systemBuffer.Dispose();
        }
    }
}