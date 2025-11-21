using UnityEngine;
using UnityEditor;
using System.IO;

public class BottomPivotQuadCreatorWindow : EditorWindow
{
    private float width = 1f;
    private float height = 1f;
    private string assetName = "BottomPivotQuad";
    private DefaultAsset saveFolder = null;
    private Mesh previewMesh;
    
    [MenuItem("Tools/Mesh Tools/Bottom Pivot Quad Creator")]
    public static void ShowWindow()
    {
        GetWindow<BottomPivotQuadCreatorWindow>("Quad Creator");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Quad Mesh Settings", EditorStyles.boldLabel);
        
        // 参数设置
        width = EditorGUILayout.FloatField("Width", width);
        height = EditorGUILayout.FloatField("Height", height);
        assetName = EditorGUILayout.TextField("Asset Name", assetName);
        
        // 保存文件夹选择
        saveFolder = (DefaultAsset)EditorGUILayout.ObjectField("Save Folder", saveFolder, typeof(DefaultAsset), false);
        
        GUILayout.Space(10);
        
        // 按钮区域
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate Preview"))
        {
            GeneratePreviewMesh();
        }
        if (GUILayout.Button("Save as Asset"))
        {
            SaveMeshAsset();
        }
        GUILayout.EndHorizontal();
        
        // 预览信息
        if (previewMesh != null)
        {
            EditorGUILayout.HelpBox($"Preview Mesh: {previewMesh.vertexCount} vertices, {previewMesh.triangles.Length / 3} triangles", MessageType.Info);
        }
        
        GUILayout.Space(5);
        
        // 快速保存到默认路径
        if (GUILayout.Button("Quick Save to Assets/Meshes/"))
        {
            QuickSaveToDefaultPath();
        }
    }
    
    private void GeneratePreviewMesh()
    {
        previewMesh = CreateBottomPivotQuadMesh(width, height);
        SceneView.RepaintAll();
    }
    
    public static Mesh CreateBottomPivotQuadMesh(float width, float height)
    {
        Mesh mesh = new Mesh();
        mesh.name = "BottomPivotQuad";
        
        // 设置顶点坐标（轴心点在底部中点）
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-width / 2, 0, 0),        // 左下角
            new Vector3(width / 2, 0, 0),         // 右下角
            new Vector3(-width / 2, height, 0),   // 左上角
            new Vector3(width / 2, height, 0)     // 右上角
        };
        
        // 设置三角形（两个三角形组成一个四边形）
        int[] triangles = new int[6]
        {
            0, 2, 1,  // 第一个三角形
            2, 3, 1   // 第二个三角形
        };
        
        // 设置UV坐标
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        // 设置法线
        Vector3[] normals = new Vector3[4]
        {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.normals = normals;
        
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    private void SaveMeshAsset()
    {
        if (string.IsNullOrEmpty(assetName))
        {
            Debug.LogError("请指定资源名称");
            return;
        }
        
        // 获取保存路径
        string folderPath = "Assets/";
        if (saveFolder != null)
        {
            folderPath = AssetDatabase.GetAssetPath(saveFolder);
        }
        
        // 确保路径以/结尾
        if (!folderPath.EndsWith("/"))
            folderPath += "/";
        
        string fullPath = folderPath + assetName + ".asset";
        
        // 创建Mesh
        Mesh meshToSave = CreateBottomPivotQuadMesh(width, height);
        
        // 保存资源[6,7](@ref)
        AssetDatabase.CreateAsset(meshToSave, fullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Mesh资源已保存: {fullPath}");
        
        // 高亮显示新创建的资源
        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(fullPath);
        EditorUtility.FocusProjectWindow();
        
        previewMesh = meshToSave;
    }
    
    private void QuickSaveToDefaultPath()
    {
        // 确保默认目录存在
        string defaultFolder = "Assets/Meshes/";
        if (!Directory.Exists(defaultFolder))
        {
            Directory.CreateDirectory(defaultFolder);
            AssetDatabase.Refresh();
        }
        
        string fullPath = defaultFolder + assetName + ".asset";
        
        // 检查是否已存在同名资源
        if (File.Exists(fullPath))
        {
            if (!EditorUtility.DisplayDialog("资源已存在", 
                $"资源 {fullPath} 已存在，是否覆盖？", "覆盖", "取消"))
            {
                return;
            }
        }
        
        SaveMeshWithPath(fullPath);
    }
    
    private void SaveMeshWithPath(string fullPath)
    {
        Mesh meshToSave = CreateBottomPivotQuadMesh(width, height);
        
        // 处理已存在资源的情况[6](@ref)
        Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(fullPath);
        if (existingMesh != null)
        {
            // 复制Mesh数据到现有资源
            existingMesh.Clear();
            existingMesh.vertices = meshToSave.vertices;
            existingMesh.triangles = meshToSave.triangles;
            existingMesh.uv = meshToSave.uv;
            existingMesh.normals = meshToSave.normals;
            existingMesh.name = meshToSave.name;
            existingMesh.RecalculateBounds();
            
            EditorUtility.SetDirty(existingMesh);
            DestroyImmediate(meshToSave);
        }
        else
        {
            // 创建新资源
            AssetDatabase.CreateAsset(meshToSave, fullPath);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"Mesh资源已保存: {fullPath}");
        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(fullPath);
    }
}