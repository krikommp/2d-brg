using System.Collections.Generic;
using UnityEngine;

namespace BRGTest
{
    public class BRGRendererSortingComparer : IComparer<Renderer>
    {
        private Camera targetCamera;

        private const int LayerBase = 100000;

        public BRGRendererSortingComparer(Camera camera)
        {
            targetCamera = camera;
        }

        public static int CalculateLayerAndOrder(Renderer renderer)
        {
            return CalculateLayerAndOrder(renderer.sortingLayerName, renderer.sortingOrder);
        }

        private static int CalculateLayerAndOrder(string sortingLayerName, int orderInLayer)
        {
            SortingLayer[] sortingLayers = SortingLayer.layers;
            int sortingLayerIndex = -1;
            for (int i = 0; i < sortingLayers.Length; i++)
            {
                if (sortingLayers[i].name == sortingLayerName)
                {
                    sortingLayerIndex = i;
                    break;
                }
            }
            
            if (sortingLayerIndex == -1)
            {
                sortingLayerIndex = 0;
            }
            
            int sortingPosition = sortingLayerIndex * LayerBase + orderInLayer;
            return sortingPosition;
        }
        
        public static void EvaluateObjectDepth(Renderer renderer, Camera camera, out float distanceAlongView, out float distanceForSort)
        {
            var center = renderer.transform.position;
            var sortingFudge = 0.0f;
            
            var worldToCameraMatrix = camera.worldToCameraMatrix;
            distanceForSort = worldToCameraMatrix.MultiplyPoint(center).z;
            distanceAlongView = -distanceForSort;

            switch (camera.transparencySortMode)
            {
                case TransparencySortMode.CustomAxis:
                    distanceForSort = -Vector3.Dot(camera.transparencySortAxis, center) - sortingFudge;
                    break;
                case TransparencySortMode.Orthographic:
                    distanceForSort = distanceForSort - sortingFudge;
                    break;
                case TransparencySortMode.Perspective:
                default:
                    distanceForSort = Vector3.Dot(center - camera.transform.position, center - camera.transform.position);
                    if (sortingFudge != 0.0f)
                    {
                        distanceForSort = Mathf.Sqrt(distanceForSort) + sortingFudge;
                        distanceForSort = Mathf.Sqrt(distanceForSort) * Mathf.Sign(distanceForSort);
                    }

                    distanceForSort = -distanceForSort;
                    break;
            }
        }

        public int Compare(Renderer x, Renderer y)
        {
            int layerOrderX = CalculateLayerAndOrder(x);
            int layerOrderY = CalculateLayerAndOrder(y);
            
            if (layerOrderX != layerOrderY)
            {
                return layerOrderX.CompareTo(layerOrderY);
            }
            
            EvaluateObjectDepth(x, targetCamera, out float distanceAlongViewX, out float distanceForSortX);
            EvaluateObjectDepth(y, targetCamera, out float distanceAlongViewY, out float distanceForSortY);
            
            return distanceForSortX.CompareTo(distanceForSortY);
        }
    }
}