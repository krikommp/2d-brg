using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraSortAxisController : MonoBehaviour
{
    [Header("Camera Reference")]
    [Tooltip("要修改的相机，如果为空则使用当前物体上的相机")]
    public Camera targetCamera;

    [Header("Sort Axis Settings")]
    [Tooltip("自定义排序轴向量")]
    public Vector3 sortAxis = new Vector3(0f, 1f, 0f);
    
    [Tooltip("排序模式")]
    public TransparencySortMode sortMode = TransparencySortMode.CustomAxis;

    private void OnEnable()
    {
        // 自动获取相机组件
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();

        if (targetCamera == null)
        {
            Debug.LogError("No camera found! Please assign a target camera.");
            return;
        }

        ApplySortAxisSettings(targetCamera);

        foreach (var cam in UnityEditor.SceneView.GetAllSceneCameras())
        {
            ApplySortAxisSettings(cam);
        }
    }

    /// <summary>
    /// 应用自定义排序轴设置到目标相机
    /// </summary>
    public void ApplySortAxisSettings(Camera camera)
    {
        if (targetCamera == null)
        {
            Debug.LogError("Target camera is not assigned!");
            return;
        }

        // 设置排序模式和自定义轴
        camera.transparencySortMode = sortMode;
        
        if (sortMode == TransparencySortMode.CustomAxis)
        {
            camera.transparencySortAxis = sortAxis;
        }

        Debug.Log($"Camera sort axis applied: {sortAxis}, Mode: {sortMode}");
    }

    /// <summary>
    /// 动态修改排序轴并立即应用
    /// </summary>
    public void SetSortAxis(Vector3 newAxis)
    {
        sortAxis = newAxis;
        sortMode = TransparencySortMode.CustomAxis;
      
        ApplySortAxisSettings(targetCamera);

        foreach (var cam in UnityEditor.SceneView.GetAllSceneCameras())
        {
            ApplySortAxisSettings(cam);
        }
    }

    /// <summary>
    /// 设置为默认的Z轴排序
    /// </summary>
    public void SetDefaultSortAxis()
    {
        SetSortAxis(new Vector3(0f, 0f, 1f));
    }

    /// <summary>
    /// 设置为2D游戏中常用的Y轴排序
    /// </summary>
    public void Set2DYAxisSort()
    {
        SetSortAxis(new Vector3(0f, 1f, 0f));
    }

    /// <summary>
    /// 设置为等距游戏常用的排序轴
    /// </summary>
    public void SetIsometricSortAxis(float zValue = 0.5f)
    {
        SetSortAxis(new Vector3(0f, 1f, zValue));
    }

    /// <summary>
    /// 在编辑模式下实时预览的方法
    /// </summary>
    [ContextMenu("Apply Settings Now")]
    private void ApplySettingsEditor()
    {
        ApplySortAxisSettings(targetCamera);

        foreach (var cam in UnityEditor.SceneView.GetAllSceneCameras())
        {
            ApplySortAxisSettings(cam);
        }
    }

    /// <summary>
    /// 重置为默认设置
    /// </summary>
    [ContextMenu("Reset to Default")]
    private void ResetToDefault()
    {
        sortMode = TransparencySortMode.Default;
        sortAxis = new Vector3(0f, 0f, 1f);
        ApplySortAxisSettings(targetCamera);

        foreach (var cam in UnityEditor.SceneView.GetAllSceneCameras())
        {
            ApplySortAxisSettings(cam);
        }
    }

    // 在Inspector中绘制Gizmos以可视化排序轴
    private void OnDrawGizmosSelected()
    {
        if (targetCamera == null) return;

        Vector3 cameraPos = targetCamera.transform.position;
        float gizmoSize = 2f;

        // 绘制排序轴方向
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(cameraPos, cameraPos + sortAxis.normalized * gizmoSize);
        
        // 绘制排序轴终点球体
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(cameraPos + sortAxis.normalized * gizmoSize, 0.2f);
    }
}