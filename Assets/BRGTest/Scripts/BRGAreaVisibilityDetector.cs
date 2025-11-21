using System.Collections.Generic;
using UnityEngine;

namespace BRGTest
{
    public class BRGAreaVisibilityDetector
    { 
        private HashSet<Vector2Int> m_activeAreaCoords = new HashSet<Vector2Int>();
        private Vector2Int m_lastCameraGridCoord = new Vector2Int(int.MinValue, int.MinValue);
        private bool m_areasChanged = false;
      
        private const float kVisibleDistance = 100f;
        private const int kWorldAreaNeighborCount = 9;
        private const int kWorldAreaNeighborSize = 16;
        private static readonly Vector2Int[] kWorldAreaNeighborOffsets =
        {
            new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1),
            new Vector2Int(-1, 0), new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, -1)
        };
        
        public void NotifyAreasChanged()
        {
            m_areasChanged = true;
            Debug.Log("区域配置已变化，下次更新将强制刷新可见性");
        }
        
        public bool UpdateActiveAreas(Camera camera, List<BRGWorldArea> worldAreas)
        {
            if (camera == null || worldAreas == null || worldAreas.Count == 0)
            {
                return false;
            }
            
            if (!ShouldUpdateVisibility(camera, worldAreas))
            {
                return false;
            }
            
            PerformVisibilityUpdate(camera, worldAreas);

            return true;
        }
        
        public bool IsAreaActive(BRGWorldArea area)
        {
            return m_activeAreaCoords.Contains(area.gridCoordinate);
        }

        private Vector2Int GetCameraGridCoordinate(Vector3 worldPosition)
        {
            int gridX = Mathf.FloorToInt(worldPosition.x / kWorldAreaNeighborSize);
            int gridZ = Mathf.FloorToInt(worldPosition.z / kWorldAreaNeighborSize);
            return new Vector2Int(gridX, gridZ);
        }
        
        private bool ShouldUpdateVisibility(Camera camera, List<BRGWorldArea> worldAreas)
        {
            // 检测区域变化逻辑
            if (m_areasChanged)
            {
                m_areasChanged = false;
                return true;
            }
            
            Vector2Int currentCameraGrid = GetCameraGridCoordinate(camera.transform.position);
            if (currentCameraGrid != m_lastCameraGridCoord)
            {
                m_lastCameraGridCoord = currentCameraGrid;
                return true;
            }

            return false;
        }

        private void PerformVisibilityUpdate(Camera camera, List<BRGWorldArea> worldAreas)
        {
            var hitGridCoord = GetRaycastHitGridCoordinate(camera);
            if (hitGridCoord == null)
            {
                return;
            }
            
            CalculateVisibleAreaRange(hitGridCoord.Value);
            
            ApplyVisibilityToAreas(worldAreas);
        }
        
        private Vector2Int? GetRaycastHitGridCoordinate(Camera camera)
        {
            if (Physics.Raycast(camera.transform.position, camera.transform.forward, out RaycastHit hit,
                    kVisibleDistance, 1 << LayerMask.NameToLayer(nameof(BRGWorldArea))))
            {
                var hitArea = hit.collider.GetComponent<BRGWorldArea>();
                if (hitArea != null)
                {
                    return hitArea.gridCoordinate;
                }
            }
        
            return null;
        }
        
        private void CalculateVisibleAreaRange(Vector2Int centerGrid)
        {
            m_activeAreaCoords.Clear();
        
            for (int i = 0; i < kWorldAreaNeighborCount; i++)
            {
                var neighborCoord = centerGrid + kWorldAreaNeighborOffsets[i];
                m_activeAreaCoords.Add(neighborCoord);
            }
        }
        
        private void ApplyVisibilityToAreas(List<BRGWorldArea> worldAreas)
        {
            int visibleCount = 0;
        
            foreach (var area in worldAreas)
            {
                bool isActive = IsAreaActive(area);
                area.SetViewState(isActive);
            
                if (isActive) visibleCount++;
            }
        }
    }
}