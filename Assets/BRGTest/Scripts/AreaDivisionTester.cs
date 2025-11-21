using UnityEngine;

namespace BRGTest
{
    public class AreaDivisionTester : MonoBehaviour
    {
        [Header("区域统计信息")] public int totalAreaCount = 0;
        public int areasWithObjects = 0;
        public int totalObjectsInAreas = 0;

        [Header("测试功能")] public bool autoRefreshOnPlay = true;
        public float testSphereRadius = 5f;

        private void Start()
        {
            if (autoRefreshOnPlay)
            {
                RefreshAreaStatistics();
            }
        }

        [ContextMenu("刷新区域统计")]
        public void RefreshAreaStatistics()
        {
            BRGWorldArea[] allAreas = FindObjectsOfType<BRGWorldArea>();
            totalAreaCount = allAreas.Length;
            areasWithObjects = 0;
            totalObjectsInAreas = 0;

            foreach (var area in allAreas)
            {
                area.UpdateObjectCount();
                totalObjectsInAreas += area.objectCount;

                if (area.objectCount > 0)
                {
                    areasWithObjects++;
                }
            }

            Debug.Log($"区域统计完成: 总共{totalAreaCount}个区域, {areasWithObjects}个区域包含对象, 总对象数{totalObjectsInAreas}");
        }

        public void UpdateAreaCount(int count)
        {
            totalAreaCount = count;
            RefreshAreaStatistics();
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            // 在场景中绘制测试球体
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, testSphereRadius);

            // 显示统计信息
#if UNITY_EDITOR
            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.yellow;
            style.fontSize = 12;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2,
                $"区域: {totalAreaCount}\n有对象区域: {areasWithObjects}\n总对象: {totalObjectsInAreas}", style);
#endif
        }
    }
}