using System;
using System.Collections.Generic;
using BRGTest.DynamicObject;
using BRGTest.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace BRGTest
{
    public unsafe class BRGPropRenderer : IBRGRenderer
    {
        private BatchRendererGroup m_brg;
        private GraphicsBuffer m_gpuPersistentBuffer;
        private BRGRenderData m_renderData;
    
        private int m_maxInstances;
        private int m_visibleCount;
    
        private BatchID m_batchID;
        private BatchMeshID m_meshID;
        private BatchMaterialID m_materialID;
    
        private Mesh m_mesh;
        private Material m_material;
        
        private int m_totalGPUBufferSize;
        private int m_alignedGPUWindowSize;
        private int m_maxInstancePerWindow;
        private int m_windowsCount;
        
        private const int kSizeOfFloat4 = sizeof(float) * 4;
        private const int kSizeOfMatrix = kSizeOfFloat4 * 4;
        private const int kSizeOfPackedMatrix = kSizeOfFloat4 * 3;
        private const int kBytesPerInstance = kSizeOfPackedMatrix + kSizeOfFloat4;

        public BRGPropRenderer(int maxInstances, Mesh mesh, Material material)
        {
            m_maxInstances = maxInstances;
            m_mesh = mesh;
            m_material = material;
            
            m_renderData = new BRGRenderData(maxInstances);
        }

        public void Dispose()
        {
            m_renderData?.Dispose();
            m_gpuPersistentBuffer?.Dispose();
            m_brg?.Dispose();
        
            m_renderData = null;
            m_gpuPersistentBuffer = null;
            m_brg = null;
        }
        
        public void Initialize()
        {
            if (m_brg != null)
            {
                return;
            }
            
            m_brg = new BatchRendererGroup(OnPerformCulling, IntPtr.Zero);
            
            m_meshID = m_brg.RegisterMesh(m_mesh);
            m_materialID = m_brg.RegisterMaterial(m_material);

            m_alignedGPUWindowSize = (m_maxInstances * kBytesPerInstance + 15) & (-16);
            m_maxInstancePerWindow = m_maxInstances;
            m_windowsCount = 1;
            m_totalGPUBufferSize = m_windowsCount * m_alignedGPUWindowSize;
            
            m_gpuPersistentBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, m_totalGPUBufferSize / sizeof(float), sizeof(float));

            SetupBatchMetaData();
            
            Debug.Log($"BRG道具渲染器初始化完成，最大实例数: {m_maxInstances}");
        }

        public void UpdateRenderData(NativeArray<BRGPackedData> renderData, BRGDynamicObjectInjector dynamicObjectInjector)
        {
            m_visibleCount = Mathf.Min(renderData.Length, m_maxInstances);

            var updateJob = new BRGUpdateRenderDataJob()
            {
                m_packedData = renderData,
                m_objectToWorldMatrices = m_renderData.m_objectToWorldMatrices,
                m_colors = m_renderData.m_colors,
                m_visibleCount = m_visibleCount
            };
            
            var jobHandle = updateJob.Schedule(m_visibleCount, 64);
            jobHandle.Complete();

            UploadToGPU();
        }

        private void UploadToGPU()
        {
            if (m_gpuPersistentBuffer == null || m_renderData == null || !m_renderData.IsValid)
            {
                return;
            }
            
            int matrixDataSize = m_visibleCount;
            m_gpuPersistentBuffer.SetData(m_renderData.m_objectToWorldMatrices, 0, 0, matrixDataSize);
            
            int colorOffset = m_maxInstances * 3;
            int colorDataSize = m_visibleCount;
            m_gpuPersistentBuffer.SetData(m_renderData.m_colors, 0, colorOffset, colorDataSize);
        }

        public string RendererType => "BRGPropRenderer";

        private void SetupBatchMetaData()
        {
            int objectToWorldID = Shader.PropertyToID("unity_ObjectToWorld");
            int baseColorID = Shader.PropertyToID("_BaseColor");

            var batchMetadata = new NativeArray<MetadataValue>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            batchMetadata[0] = CreateMetadataValue(objectToWorldID, 0, true);
            batchMetadata[1] = CreateMetadataValue(baseColorID, kSizeOfPackedMatrix * m_maxInstancePerWindow, true);
            
            m_batchID = m_brg.AddBatch(batchMetadata, m_gpuPersistentBuffer.bufferHandle, 0, 0);

            batchMetadata.Dispose();
        }
        
        private JobHandle OnPerformCulling(BatchRendererGroup rendererGroup, BatchCullingContext cullingContext, BatchCullingOutput cullingOutput, IntPtr userContext)
        {
            var jobHandle = new JobHandle();
            if (m_visibleCount == 0)
            {
                return jobHandle;
            }

            BatchCullingOutputDrawCommands drawCommands = new BatchCullingOutputDrawCommands();

            int drawCommandCount = 1;
            int maxInstancePerDrawCommand = m_maxInstancePerWindow;
            drawCommands.drawCommandCount = drawCommandCount;

            drawCommands.drawRangeCount = 1;
            drawCommands.drawRanges = Malloc<BatchDrawRange>(1);
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

            drawCommands.visibleInstances = Malloc<int>((uint)m_visibleCount);
            for (int i = 0; i < m_visibleCount; i++)
            {
                drawCommands.visibleInstances[i] = i;
            }

            drawCommands.drawCommands = Malloc<BatchDrawCommand>((uint)drawCommandCount);
            drawCommands.drawCommands[0] = new BatchDrawCommand
            {
                visibleOffset = 0,
                visibleCount = (uint)m_visibleCount,
                batchID = m_batchID,
                materialID = m_materialID,
                meshID = m_meshID,
                submeshIndex = 0,
                splitVisibilityMask = 0xff,
                flags = BatchDrawCommandFlags.None,
                sortingPosition = 0
            };

            cullingOutput.drawCommands[0] = drawCommands;
            drawCommands.instanceSortingPositions = null;
            drawCommands.instanceSortingPositionFloatCount = 0;
        
            return jobHandle;
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
        
        private static T* Malloc<T>(uint count) where T : unmanaged
        {
            return (T*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<T>() * count,
                UnsafeUtility.AlignOf<T>(),
                Allocator.TempJob);
        }
    }
}