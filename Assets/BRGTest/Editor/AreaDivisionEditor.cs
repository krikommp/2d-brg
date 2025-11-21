using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.Mathematics;
using Random = UnityEngine.Random;

namespace BRGTest
{
    public class AreaDivisionEditor : EditorWindow
    {
        private string parentObjectName = "WorldAreas";
        private Vector2 areaSize = new Vector2(16, 16);
        private Bounds divisionBounds = new Bounds(Vector3.zero, new Vector3(100, 100, 100));
        private bool showDebugInfo = true;

        private bool generateQuads = true;
        private int minQuadsPerArea = 15;
        private int maxQuadsPerArea = 50;
        private Material quadMaterial;
        private Mesh quadMesh;
        private float quadSize = 1.0f;

        [MenuItem("Tools/场景工具/区域划分管理器")]
        public static void ShowWindow()
        {
            GetWindow<AreaDivisionEditor>("区域划分管理器");
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("场景区域划分工具", EditorStyles.boldLabel);

            GUILayout.Space(5);
            EditorGUILayout.HelpBox("此工具将场景划分为16x16的区域，并为每个区域创建带有BRGWorldArea脚本的GameObject", MessageType.Info);

            GUILayout.Space(10);
            parentObjectName = EditorGUILayout.TextField("父物体名称", parentObjectName);
            areaSize = EditorGUILayout.Vector2Field("区域尺寸", areaSize);

            GUILayout.Space(10);
            GUILayout.Label("划分边界设置", EditorStyles.boldLabel);
            divisionBounds.center = EditorGUILayout.Vector3Field("中心点", divisionBounds.center);
            divisionBounds.size = EditorGUILayout.Vector3Field("尺寸", divisionBounds.size);

            GUILayout.Space(10);
            GUILayout.Label("Quad生成设置", EditorStyles.boldLabel);
            generateQuads = EditorGUILayout.Toggle("生成Quads", generateQuads);

            if (generateQuads)
            {
                minQuadsPerArea = EditorGUILayout.IntSlider("最小Quad数量", minQuadsPerArea, 1, 20);
                maxQuadsPerArea = EditorGUILayout.IntSlider("最大Quad数量", maxQuadsPerArea, minQuadsPerArea, 50);
                quadSize = EditorGUILayout.Slider("Quad大小", quadSize, 0.5f, 5.0f);
                quadMaterial = (Material)EditorGUILayout.ObjectField("Quad材质", quadMaterial, typeof(Material), false);
                quadMesh = (Mesh)EditorGUILayout.ObjectField("Quad Mesh", quadMesh, typeof(Mesh), false);
            }

            GUILayout.Space(15);
            if (GUILayout.Button("生成区域划分", GUILayout.Height(30)))
            {
                GenerateAreaDivision();
            }

            if (GUILayout.Button("清除所有区域", GUILayout.Height(25)))
            {
                ClearAllAreas();
            }

            if (GUILayout.Button("统计区域信息", GUILayout.Height(25)))
            {
                CountAreas();
            }

            showDebugInfo = EditorGUILayout.Toggle("显示调试信息", showDebugInfo);

            GUILayout.Space(10);
            if (showDebugInfo)
            {
                DisplayAreaInfo();
            }
        }

        private void GenerateAreaDivision()
        {
            // 查找或创建父物体
            GameObject parentObject = GameObject.Find(parentObjectName);
            if (parentObject == null)
            {
                parentObject = new GameObject(parentObjectName);
                parentObject.transform.position = Vector3.zero;
            }

            // 清除现有的区域子物体
            ClearChildAreas(parentObject);

            // 计算划分数量
            int xCount = Mathf.CeilToInt(divisionBounds.size.x / areaSize.x);
            int zCount = Mathf.CeilToInt(divisionBounds.size.z / areaSize.y);
            int totalAreas = xCount * zCount;

            if (showDebugInfo)
            {
                Debug.Log($"开始划分区域: X方向{xCount}个, Z方向{zCount}个, 总共{totalAreas}个区域");
            }

            // 生成区域
            int areasCreated = 0;
            for (int x = 0; x < xCount; x++)
            {
                for (int z = 0; z < zCount; z++)
                {
                    var areaObject = CreateAreaGameObject(x, z, parentObject.transform);
                    areasCreated++;
                }
            }

            Debug.Log($"区域划分完成！共创建 {areasCreated} 个区域物体");

            // 创建测试脚本并附加统计信息
            CreateAreaTesterScript(areasCreated);
        }

        private GameObject CreateAreaGameObject(int xIndex, int zIndex, Transform parent)
        {
            // 计算区域位置和边界
            Vector3 areaPosition = new Vector3(
                divisionBounds.min.x + xIndex * areaSize.x + areaSize.x / 2,
                divisionBounds.center.y,
                divisionBounds.min.z + zIndex * areaSize.y + areaSize.y / 2
            );

            Bounds areaBounds = new Bounds(areaPosition, new Vector3(areaSize.x, divisionBounds.size.y, areaSize.y));

            // 创建区域GameObject
            GameObject areaObject = new GameObject($"Area_{xIndex}_{zIndex}");
            areaObject.transform.position = areaPosition;
            areaObject.transform.parent = parent;
            areaObject.layer = LayerMask.NameToLayer(nameof(BRGWorldArea));

            // 添加BRGWorldArea组件并配置
            BRGWorldArea areaComponent = areaObject.AddComponent<BRGWorldArea>();
            areaComponent.gridCoordinate = new Vector2Int(xIndex, zIndex);
            areaComponent.areaBounds = areaBounds;

            // 添加BoxCollider用于可视化（可选）
            BoxCollider collider = areaObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(areaSize.x, 0.1f, areaSize.y);
            collider.isTrigger = true;

            // 生成Quads
            var quadRenderers = CreateMeshInArea(areaComponent, areaPosition, quadMesh);
            
            // 排序区域内的对象（可选）
            CreateSortedRenderersList(areaComponent, quadRenderers);
            
            return areaObject;
        }
        
        private List<Renderer> CreateMeshInArea(BRGWorldArea area, Vector3 areaPosition, Mesh sourceMesh)
        {
            var createdRenderers = new List<Renderer>();
    
            // 安全检查
            if (sourceMesh == null)
            {
                Debug.LogError("无法创建Mesh物体：传入的Mesh资源为null");
                return createdRenderers;
            }
    
            if (!generateQuads || quadMaterial == null)
            {
                return createdRenderers;
            }
    
            // 生成随机数量的Mesh物体
            int meshCount = Random.Range(minQuadsPerArea, maxQuadsPerArea + 1);
    
            for (int i = 0; i < meshCount; i++)
            {
                // 计算随机位置（与之前相同）
                Vector3 randomPosition = new Vector3(
                    Random.Range(area.areaBounds.min.x, area.areaBounds.max.x),
                    areaPosition.y + 0.1f, // 稍微抬高避免z-fighting
                    Random.Range(area.areaBounds.min.z, area.areaBounds.max.z)
                );

                // 创建GameObject并添加Mesh组件
                GameObject meshObject = new GameObject($"MeshObject_{area.name}_{i}");
                MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
                MeshCollider meshCollider = meshObject.AddComponent<MeshCollider>();
        
                // 分配Mesh资源
                meshFilter.mesh = sourceMesh;
                meshRenderer.material = quadMaterial;
        
                // 设置变换属性
                meshObject.transform.position = randomPosition;
                meshObject.transform.localScale = new Vector3(quadSize, quadSize, quadSize);
                meshObject.transform.rotation = Quaternion.Euler(45, 0, 0);
                meshObject.transform.parent = area.transform;
                
                meshCollider.sharedMesh = sourceMesh;
        
                createdRenderers.Add(meshRenderer);
            }
    
            Debug.Log($"在区域 {area.name} 中创建了 {meshCount} 个Mesh物体");
            return createdRenderers;
        }

        private void CreateSortedRenderersList(BRGWorldArea area, List<Renderer> renderers)
        {
            var camera = Camera.main;
            
            var cmparer = new BRGRendererSortingComparer(camera);
            renderers.Sort(cmparer);
            area.sortedPropDatas.Clear();

            foreach (var renderer in renderers)
            {
                BRGRendererSortingComparer.EvaluateObjectDepth(renderer, camera, out float distanceAlongView, out float distanceForSort);
                var layerAndOrder = BRGRendererSortingComparer.CalculateLayerAndOrder(renderer);
                
                var randomColor = Random.ColorHSV();
                BRGPackedData data = new BRGPackedData
                {
                    m_unityToWorld = renderer.transform.localToWorldMatrix,
                    m_color = new float4(randomColor.r, randomColor.g, randomColor.b, randomColor.a),
                    m_worldPos = renderer.transform.position,
                    m_distance = distanceForSort,
                    m_layerAndOrder = layerAndOrder
                };
                area.sortedPropDatas.Add(data);
                area.sortedPropObjects.Add(renderer.gameObject);
            }
        }
        
        private void ClearChildAreas(GameObject parent)
        {
            int childCount = parent.transform.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(parent.transform.GetChild(i).gameObject);
            }

            if (showDebugInfo && childCount > 0)
            {
                Debug.Log($"已清除 {childCount} 个现有区域物体");
            }
        }

        private void ClearAllAreas()
        {
            GameObject parentObject = GameObject.Find(parentObjectName);
            if (parentObject != null)
            {
                DestroyImmediate(parentObject);
                Debug.Log("已清除所有区域物体");
            }
            else
            {
                Debug.Log("未找到区域父物体，无需清除");
            }
        }

        private void CountAreas()
        {
            BRGWorldArea[] areas = FindObjectsOfType<BRGWorldArea>();
            Debug.Log($"场景中共有 {areas.Length} 个BRGWorldArea区域");

            // 更新每个区域的对象计数
            foreach (var area in areas)
            {
                area.UpdateObjectCount();
            }
        }

        private void CreateAreaTesterScript(int totalAreas)
        {
            // 查找或创建测试脚本挂载对象
            GameObject testerObject = GameObject.Find("AreaDivisionTester");
            if (testerObject == null)
            {
                testerObject = new GameObject("AreaDivisionTester");
            }

            // 添加或获取测试组件
            AreaDivisionTester tester = testerObject.GetComponent<AreaDivisionTester>();
            if (tester == null)
            {
                tester = testerObject.AddComponent<AreaDivisionTester>();
            }

            tester.UpdateAreaCount(totalAreas);
        }

        private void DisplayAreaInfo()
        {
            BRGWorldArea[] areas = FindObjectsOfType<BRGWorldArea>();
            if (areas.Length > 0)
            {
                GUILayout.Space(10);
                GUILayout.Label($"当前场景区域统计: {areas.Length} 个区域", EditorStyles.boldLabel);

                foreach (var area in areas)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel($"区域 {area.gridCoordinate.x},{area.gridCoordinate.y}");
                    EditorGUILayout.LabelField($"对象: {area.objectCount} 位置: {area.transform.position}");
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}