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
                    searchAreaConfigurator?.ConfigureSearchAreaCanvas(SearchAreaShape.Circle, new float2(diagonalSize, diagonalSize), canvasDistanceFromCamera);
                    break;
                case SearchAreaShape.Rectangle:
                    searchAreaConfigurator?.ConfigureSearchAreaCanvas(SearchAreaShape.Rectangle, CalculateRectangularSearchAreaSize(diagonalSize, searchAreaAspectRatio), canvasDistanceFromCamera);
                    break;
            }
            
            
            
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