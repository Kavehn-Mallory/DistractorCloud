using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

namespace DistractorClouds.DistractorTask
{
    public class SearchAreaConfigurator : MonoBehaviour
    {
        [SerializeField] private Canvas searchAreaCanvas;
        
        [SerializeField] private Image circularSearchArea;
        [SerializeField] private Image rectangularSearchArea;

        [SerializeField] private Color searchAreaColor = new(217f / 255f, 101 / 255f, 101 / 255f, 105 / 255f);

        private RectTransform _searchAreaCanvasRectTransform;

        
        private void Awake()
        {
            if (!searchAreaCanvas)
            {
                Debug.LogError("Missing a canvas for the search area", this);
            }

            if (!circularSearchArea)
            {
                Debug.LogError("Missing a circular search area", this);
            }
            
            if (!rectangularSearchArea)
            {
                Debug.LogError("Missing a rectangular search area", this);
            }

            _searchAreaCanvasRectTransform = searchAreaCanvas.GetComponent<RectTransform>();


        }

        
        
        public void ResizeSearchArea(SearchAreaShape searchAreaShape, Vector3 searchAreaPosition, Quaternion searchAreaRotation, float2 dimensions)
        {
            
            switch (searchAreaShape)
            {
                case SearchAreaShape.Circle:
                    SetupCircularSearchArea(dimensions.x);
                    break;
                case SearchAreaShape.Rectangle:
                    SetupRectangularSearchArea(dimensions);
                    break;
            }
            searchAreaCanvas.transform.SetPositionAndRotation(searchAreaPosition, searchAreaRotation);

        }

        private void MoveCanvasBasedOnControllerPosition(Vector3 canvasPosition)
        {
            //todo 
            searchAreaCanvas.transform.SetPositionAndRotation(canvasPosition, Quaternion.identity);
        }

        private void SetupRectangularSearchArea(float2 dimensions)
        {
            rectangularSearchArea.rectTransform.sizeDelta = dimensions;
            rectangularSearchArea.color = searchAreaColor;
            rectangularSearchArea.enabled = true;


            _searchAreaCanvasRectTransform.sizeDelta = dimensions;


        }

        private void SetupCircularSearchArea(float diameter)
        {
            circularSearchArea.rectTransform.sizeDelta = new Vector2(diameter, diameter);
            circularSearchArea.color = searchAreaColor;
            circularSearchArea.enabled = true;

            _searchAreaCanvasRectTransform.sizeDelta = new Vector2(diameter, diameter);
        }
    }
}