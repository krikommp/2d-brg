using System;
using System.Collections.Generic;
using BRGTest.Batch;
using BRGTest.DynamicObject;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace BRGTest
{
    public class BRGRenderQueue
    {
        private List<BRGBatchData> m_batches = new List<BRGBatchData>();
        private BatchMaterialID m_materialID;
        private BatchMeshID m_meshID;
        
        public void BuildRenderQueue(List<BRGBatchData> batches, BatchMaterialID materialID, BatchMeshID meshID)
        {
            m_batches.Clear();
            m_batches.AddRange(batches);
            
            m_materialID = materialID;
            m_meshID = meshID;
        }
        
        public unsafe JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
        {
            var jobHandle = new JobHandle();

            BatchCullingOutputDrawCommands drawCommands = new BatchCullingOutputDrawCommands();

            int drawCommandCount = m_batches.Count;
            drawCommands.drawCommandCount = drawCommandCount;

            int drawRangeCount = 1;
            drawCommands.drawRangeCount = drawRangeCount;
            drawCommands.drawRanges = Malloc<BatchDrawRange>((uint)drawRangeCount);
            drawCommands.drawRanges[0] = new BatchDrawRange
            {
                drawCommandsBegin = 0,
                drawCommandsCount = (uint)drawCommandCount,
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

            int visibleCount = 0;
            foreach (var batch in m_batches)
            {
                visibleCount += batch.m_count;
            }
            drawCommands.visibleInstances = Malloc<int>((uint)visibleCount);
            for (int i = 0; i < visibleCount; i++)
            {
                drawCommands.visibleInstances[i] = i;
            }
            
            drawCommands.drawCommands = Malloc<BatchDrawCommand>((uint)drawCommandCount);
            for (int i = 0; i < m_batches.Count; ++i)
            {
                var batch = m_batches[i];
                drawCommands.drawCommands[i] = new BatchDrawCommand
                {
                    visibleOffset = (uint)batch.m_startIndex,
                    visibleCount = (uint)batch.m_count,
                    batchID = batch.m_batchId,
                    materialID = m_materialID,
                    meshID = m_meshID,
                    submeshIndex = 0,
                    splitVisibilityMask = 0xff,
                    flags = BatchDrawCommandFlags.None,
                    sortingPosition = 0,
                };
            }

            cullingOutput.drawCommands[0] = drawCommands;
            drawCommands.instanceSortingPositions = null;
            drawCommands.instanceSortingPositionFloatCount = 0;

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