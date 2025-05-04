using System;
using Unity.Mathematics;
using UnityEngine;

namespace DistractorClouds.DistractorTask
{
    public class SearchAreaHandler : MonoBehaviour
    {
        [SerializeField] private SearchAreaShape searchAreaShape;

        [SerializeField] private SearchAreaValidation searchAreaValidation;

        [SerializeField] private float2 searchAreaAspectRatio = new float2(300, 200);
        [SerializeField] private float diagonalViewingAngle = 10f;

        [SerializeField] private float canvasDistanceFromCamera = 1f;

        [SerializeField, Range(0, 1)]
        private float overlapPercent = 0.5f;

        [Tooltip("Used for overlap comparison. How much overlap is necessary for the selection to be valid")]
        [SerializeField] private SearchAreaConfigurator searchAreaConfigurator;

        private float2 _dimensions;

        private void Start()
        {

            if (!Camera.main)
            {
                Debug.LogError("Missing a main camera in the scene.", this);
                return;
            }

            if (!searchAreaConfigurator)
            {
                Debug.LogWarning("Missing a SearchAreaConfigurator. Search area won't be displayed accurately", this);
            }
            var diagonalSize = CalculateSizeFromViewingAngle(canvasDistanceFromCamera, diagonalViewingAngle);

            switch (searchAreaShape)
            {
                case SearchAreaShape.Circle:
                    _dimensions = new float2(diagonalSize, diagonalSize);
                    searchAreaConfigurator?.ConfigureSearchAreaCanvas(SearchAreaShape.Circle, _dimensions, canvasDistanceFromCamera);
                    break;
                case SearchAreaShape.Rectangle:
                    _dimensions = CalculateRectangularSearchAreaSize(diagonalSize, searchAreaAspectRatio);
                    searchAreaConfigurator?.ConfigureSearchAreaCanvas(SearchAreaShape.Rectangle, _dimensions, canvasDistanceFromCamera);
                    break;
            }

            _dimensions = CalculateScreenSpaceSize(Camera.main, _dimensions, canvasDistanceFromCamera);

        }

        private static float2 CalculateScreenSpaceSize(Camera targetCamera, float2 dimensions, float distanceFromCamera)
        {
            var center = (float2)targetCamera.pixelRect.center;
            var worldPos = targetCamera.ScreenToWorldPoint(new Vector3(center.x, center.y, distanceFromCamera));
            var offsetToScreen = ((float3)targetCamera.WorldToScreenPoint(worldPos + new Vector3(dimensions.x, dimensions.y, 0))).xy;

            var dif = math.abs(offsetToScreen - center);

            return new float2(dif.x, dif.y);
        }


        public bool IsTargetInSearchArea(Camera targetCamera, Collider target)
        {
            var searchAreaPosition = CalculateSearchAreaPosition(targetCamera);
            switch (searchAreaShape)
            {
                case SearchAreaShape.Circle:
                    
                    return IsTargetInCircularSearchArea(targetCamera, target, searchAreaPosition, _dimensions, searchAreaValidation, overlapPercent);
                case SearchAreaShape.Rectangle:
                    return IsTargetInRectangularSearchArea(targetCamera, target, searchAreaPosition, _dimensions, searchAreaValidation, overlapPercent);
            }

            return false;
        }

        private static bool IsTargetInRectangularSearchArea(Camera targetCamera, Collider target, float2 searchAreaPosition, float2 dimensions, SearchAreaValidation searchAreaValidation, float overlapPercent)
        {
            float2 halfDimensions = dimensions / 2f;
            float2 searchAreaMinOnScreen = searchAreaPosition - halfDimensions;
            float2 searchAreaMaxOnScreen = searchAreaPosition + halfDimensions;

            float2 screenSpaceMin = new float2(math.min(searchAreaMinOnScreen.x, searchAreaMaxOnScreen.x),
                math.min(searchAreaMinOnScreen.y, searchAreaMaxOnScreen.y));
            
            float2 screenSpaceMax = new float2(math.max(searchAreaMinOnScreen.x, searchAreaMaxOnScreen.x),
                math.max(searchAreaMinOnScreen.y, searchAreaMaxOnScreen.y));


            return IsTargetInScreenSpaceSearchArea(targetCamera, screenSpaceMin, screenSpaceMax, target, searchAreaValidation,
                overlapPercent);
        }
        
        //todo https://petrelharp.github.io/circle_rectangle_intersection/circle_rectangle_intersection.html
        //https://www.reddit.com/r/Unity3D/comments/eye8yk/performance_results_collider_vs_overlap_test/

        private float2 CalculateSearchAreaPosition(Camera targetCamera)
        {
            if (!targetCamera)
            {
                return new float2();
            }
            return targetCamera.pixelRect.center;
        }

        private static bool IsTargetInCircularSearchArea(Camera targetCamera, Collider target, float2 searchAreaPosition, float2 dimensions, SearchAreaValidation searchAreaValidation, float overlapPercent) =>
            searchAreaValidation switch
            {
                SearchAreaValidation.CenterPointInside => CenterInsideCircle(targetCamera, target,
                    searchAreaPosition, dimensions.x / 2f),
                SearchAreaValidation.OverlapPercent => CircleOverlap(targetCamera, target, searchAreaPosition, dimensions.x / 2f, overlapPercent),
                SearchAreaValidation.FullBoundingBoxInside => CircleOverlap(targetCamera, target, searchAreaPosition, dimensions.x / 2f, 1.0f),
                _ => throw new ArgumentOutOfRangeException(nameof(searchAreaValidation), searchAreaValidation, null)
            };

        private static bool CircleOverlap(Camera targetCamera, Collider target, float2 searchAreaPosition, float radius, float overlapPercent)
        {
            var minPos = ((float3)targetCamera.WorldToScreenPoint(target.bounds.min, targetCamera.stereoActiveEye)).xy;
            var maxPos = ((float3)targetCamera.WorldToScreenPoint(target.bounds.max, targetCamera.stereoActiveEye)).xy;
            var minScreenPos = new float2(math.min(minPos.x, maxPos.x), math.min(minPos.y, maxPos.y));
            var maxScreenPos = new float2(math.max(minPos.x, maxPos.x), math.max(minPos.y, maxPos.y));

            var targetDif = maxScreenPos - minScreenPos;
            var targetSize = targetDif.x * targetDif.y;

            var circleArea = math.PI2 * radius * radius;

            if (math.distance(minScreenPos, searchAreaPosition) > radius &&
                math.distance(maxScreenPos, searchAreaPosition) > radius)
            {
                //min and max are further away from the search area than the radius
                return false;
            }

            if (targetSize * overlapPercent > circleArea)
            {
                //the percentage of the target area that would have to be overlapped by the target area is already too big to be covered by the circle 
                return false;
            }
            
            //we have overlap, we just need to calculate the overlapping part 

            //Todo this does not work right now but its fine
            return true;

        }

        private static bool CenterInsideCircle(Camera targetCamera, Collider target, float2 searchAreaPosition, float radius)
        {
            var position = ((float3)targetCamera.WorldToScreenPoint(target.bounds.center, targetCamera.stereoActiveEye)).xy;
            
            return math.distance(searchAreaPosition, position.xy) <= radius;
        }


        private static bool IsTargetInScreenSpaceSearchArea(Camera targetCamera, float2 screenSpaceMin,
            float2 screenSpaceMax, Collider target, SearchAreaValidation comparison, float overlapPercent) =>
            comparison switch
            {
                SearchAreaValidation.CenterPointInside => CenterInsideBoundingBox(targetCamera, target,
                    screenSpaceMin, screenSpaceMax),
                SearchAreaValidation.OverlapPercent => TargetBoundingBoxOverlap(targetCamera, target, screenSpaceMin, screenSpaceMax, overlapPercent),
                SearchAreaValidation.FullBoundingBoxInside => TargetBoundingBoxInsideBoundingBox(targetCamera, target, screenSpaceMin, screenSpaceMax),
                _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null)
            };
        
        
        private static bool CenterInsideBoundingBox(Camera targetCamera, Collider target, float2 searchAreaMin, float2 searchAreaMax)
        {
            var position = ((float3)targetCamera.WorldToScreenPoint(target.bounds.center, targetCamera.stereoActiveEye)).xy;
            return math.all(searchAreaMin <= position) && math.all(searchAreaMax >= position);
        }
        
        private static bool TargetBoundingBoxInsideBoundingBox(Camera targetCamera, Collider target, float2 searchAreaMin, float2 searchAreaMax)
        {
            var minPos = ((float3)targetCamera.WorldToScreenPoint(target.bounds.min, targetCamera.stereoActiveEye)).xy;
            var maxPos = ((float3)targetCamera.WorldToScreenPoint(target.bounds.max, targetCamera.stereoActiveEye)).xy;
            var minScreenPos = new float2(math.min(minPos.x, maxPos.x), math.min(minPos.y, maxPos.y));
            var maxScreenPos = new float2(math.max(minPos.x, maxPos.x), math.max(minPos.y, maxPos.y));
            return math.all(searchAreaMin <= minScreenPos) && math.all(searchAreaMax >= maxScreenPos);
        }
        
        private static bool TargetBoundingBoxOverlap(Camera targetCamera, Collider target, float2 searchAreaMin, float2 searchAreaMax, float overlap)
        {
            var minPos = ((float3)targetCamera.WorldToScreenPoint(target.bounds.min, targetCamera.stereoActiveEye)).xy;
            var maxPos = ((float3)targetCamera.WorldToScreenPoint(target.bounds.max, targetCamera.stereoActiveEye)).xy;
            var minScreenPos = new float2(math.min(minPos.x, maxPos.x), math.min(minPos.y, maxPos.y));
            var maxScreenPos = new float2(math.max(minPos.x, maxPos.x), math.max(minPos.y, maxPos.y));

            var diff = maxScreenPos - minScreenPos;
            var totalArea = diff.x * diff.y;
            

            if (math.any(minScreenPos > searchAreaMax) || math.any(maxScreenPos < searchAreaMin))
            {
                return false;
            }
            
            var bottomLeft = math.max(minScreenPos, searchAreaMin);
            var topRight = math.min(maxScreenPos, searchAreaMax);
            diff = topRight - bottomLeft;
            var area = diff.x * diff.y;
            return area / totalArea >= overlap;
        }


        
        /// <summary>
        /// Calculates the size of an object at distance r from the camera covering a viewing angle of alpha degrees 
        /// </summary>
        /// <param name="r">Distance from camera</param>
        /// <param name="alpha">Viewing angle in degrees</param>
        /// <returns></returns>
        private static float CalculateSizeFromViewingAngle(float r, float alpha)
        {
            var radians = math.radians(alpha);
            return math.abs(2f * r * math.tan((radians / 2f)));
        }

        private static float2 CalculateRectangularSearchAreaSize(float diagonalSize, float2 searchAreaAspectRatio)
        {
            //because I'm too dumb to math: https://www.omnicalculator.com/other/screen-size
            var aspectRatio = searchAreaAspectRatio.x / searchAreaAspectRatio.y;
            var height = diagonalSize / math.sqrt((aspectRatio * aspectRatio) + 1);
            var width = aspectRatio * height;
            return new float2(width, height);
        }
        
        
    }
    
    
    [Serializable]
    public enum SearchAreaShape
    {
        Rectangle,
        Circle
    }

    [Serializable]
    public enum SearchAreaValidation
    {
        CenterPointInside,
        OverlapPercent,
        FullBoundingBoxInside
    }
}