using System;
using System.Collections.Generic;
using DistractorClouds.DistractorTask.StudyEventData;
using DistractorClouds.PanelGeneration;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace DistractorClouds.DistractorTask
{
    public class DistractorTaskManager : MonoBehaviour
    {
        [Header("Distractor Task Settings")]
        public Camera targetCamera;

        public ClosestSplinePointGeneration pointGenerator;
        
        public Material defaultMaterial;
        public Material targetMaterial;

        [Header("Search Area Settings")]
        public float2 searchAreaInPixel;
        public Image searchArea;
        public SearchAreaComparison searchAreaComparison;
        [Range(0, 1)]
        [Tooltip("Used for overlap comparison. How much overlap is necessary for the selection to be valid")]
        public float overlapPercent = 0.5f;
        
        #region EventHooks

        [Header("Event Hooks")]
        public Action<StudyStartData> OnStudyStartEvent = delegate {};
        [SerializeField]
        private UnityEvent<StudyStartData> onStudyStartEvent = new();
        
        public Action<SelectionData> OnSelectionPressedEvent = delegate {};
        [SerializeField]
        private UnityEvent<SelectionData> onSelectionPressedEvent = new();
        
        public Action<StudyEndData> OnStudyEndEvent = delegate {};
        [SerializeField]
        private UnityEvent<StudyEndData> onStudyEndEvent = new();
        
        public Action<StudyPathEndPointReachedData> OnStudyPathEndReachedEvent = delegate {};
        [SerializeField]
        private UnityEvent<StudyPathEndPointReachedData> onStudyPathEndReachedEvent = new();
        
        public Action<SplineRepositioningData> OnSplineRepositioningEvent = delegate {};
        [SerializeField]
        private UnityEvent<SplineRepositioningData> onSplineRepositioningEvent = new();

        #endregion

        #region Private Fields
        private const string Visible = "visible";
        private const string NotVisible = "not visible";

        private MeshRenderer _targetRenderer;
        
        private int _itemCount = -1;

        private float4 _searchAreaInViewSpace;

        private int _currentGroup;

        private int _groupCount;

        private List<ClosestSplinePointGeneration.InstantiatedPointCloudObject> _instantiatedPointCloudObjects = new();
        private float _maxLength;

        #endregion



        private void OnStudyStart()
        {
            var studyStartData = new StudyStartData
            {
                TimeStamp = Time.time
            };
            OnStudyStartEvent.Invoke(studyStartData);
            onStudyStartEvent.Invoke(studyStartData);
        }

        private void OnSelectionPressed(bool validSelection)
        {
            var selectionPressedData = new SelectionData
            {
                TimeStamp = Time.time,
                IsValidSelection = validSelection
            };
            
            OnSelectionPressedEvent.Invoke(selectionPressedData);
            onSelectionPressedEvent.Invoke(selectionPressedData);
        }
        
        private void OnStudyEnd()
        {
            var studyEndData = new StudyEndData()
            {
                TimeStamp = Time.time
            };
            
            OnStudyEndEvent.Invoke(studyEndData);
            onStudyEndEvent.Invoke(studyEndData);
        }
        
        private void OnSplineEndReached(float splinePosition)
        {
            var studyPathEndPointReachedData = new StudyPathEndPointReachedData()
            {
                TimeStamp = Time.time,
                SplinePosition = splinePosition
            };
            
            OnStudyPathEndReachedEvent.Invoke(studyPathEndPointReachedData);
            onStudyPathEndReachedEvent.Invoke(studyPathEndPointReachedData);
        }

        private void OnSplineRepositioning(Vector3 oldPosition, Vector3 newPosition, Quaternion oldOrientation,
            Quaternion newOrientation)
        {
            var repositioningData = new SplineRepositioningData
            {
                TimeStamp = Time.time,
                OldOrientation = oldOrientation,
                NewOrientation = newOrientation,
                NewPosition = newPosition,
                OldPosition = oldPosition
            };
            OnSplineRepositioningEvent.Invoke(repositioningData);
            onSplineRepositioningEvent.Invoke(repositioningData);
        }
        
        private void Start()
        {
            FindFirstObjectByType<InputHandler>().OnBumperDown += TrySelectDistractor;
            _searchAreaInViewSpace = CalculateSearchArea(targetCamera, searchAreaInPixel);
            UpdateSearchAreaSize();
        }

        private void GeneratePointCloudData()
        {
            _instantiatedPointCloudObjects.AddRange(pointGenerator.InstantiatedGameObjects);
            _instantiatedPointCloudObjects.Sort(ComparePointCloudObjectsByPosition);
            _groupCount = pointGenerator.GroupCount;
            _maxLength = pointGenerator.MaxLength;
            _currentGroup = 0;
        }

        private static int ComparePointCloudObjectsByPosition(ClosestSplinePointGeneration.InstantiatedPointCloudObject x,
            ClosestSplinePointGeneration.InstantiatedPointCloudObject y)
        {
            return x.splinePosition.CompareTo(y.splinePosition);
        }

        private void OnValidate()
        {
            if (searchArea)
            {
                UpdateSearchAreaSize();
            }
        }


        private void UpdateSearchAreaSize()
        {
            if (!searchArea)
            {
                return;
            }
            var imageTransform = searchArea.GetComponent<RectTransform>();

            imageTransform.sizeDelta = searchAreaInPixel;
        }

        private static float4 CalculateSearchArea(Camera targetCamera, float2 searchAreaInPixel)
        {
            if (!targetCamera)
            {
                return new float4();
            }
            searchAreaInPixel /= 2f;
            var center = targetCamera.pixelRect.center;
            var bottomLeft = new Vector3(center.x - searchAreaInPixel.x, center.y - searchAreaInPixel.y, 0);
            var topRight = new Vector3(center.x + searchAreaInPixel.x, center.y + searchAreaInPixel.y, 0);
            var bottomLeftViewSpace = ((float3)targetCamera.ScreenToViewportPoint(bottomLeft)).xy;
            var topRightViewSpace = ((float3)targetCamera.ScreenToViewportPoint(topRight)).xy;

            
            return new float4(bottomLeftViewSpace, topRightViewSpace);
        }

        public static bool IsTargetInSearchArea(Camera targetCamera, Vector3 searchAreaMin, Vector3 searchAreaMax, Collider target, SearchAreaComparison comparison = SearchAreaComparison.CenterInsideBoundingBox, float overlapPercent = 0.5f)
        {

            Vector3 searchAreaMinToScreen = targetCamera.WorldToScreenPoint(searchAreaMin, targetCamera.stereoActiveEye);
            Vector3 searchAreaMaxToScreen = targetCamera.WorldToScreenPoint(searchAreaMax, targetCamera.stereoActiveEye);

            float2 screenSpaceMin = new float2(math.min(searchAreaMinToScreen.x, searchAreaMaxToScreen.x),
                math.min(searchAreaMinToScreen.y, searchAreaMaxToScreen.y));
            
            float2 screenSpaceMax = new float2(math.max(searchAreaMinToScreen.x, searchAreaMaxToScreen.x),
                math.max(searchAreaMinToScreen.y, searchAreaMaxToScreen.y));


            return IsTargetInScreenSpaceSearchArea(targetCamera, screenSpaceMin, screenSpaceMax, target, comparison,
                overlapPercent);

        }

        private static bool IsTargetInScreenSpaceSearchArea(Camera targetCamera, float2 screenSpaceMin,
            float2 screenSpaceMax, Collider target, SearchAreaComparison comparison, float overlapPercent) =>
            comparison switch
            {
                SearchAreaComparison.CenterInsideBoundingBox => CenterInsideBoundingBox(targetCamera, target,
                    screenSpaceMin, screenSpaceMax),
                SearchAreaComparison.BoundingBoxOverlap => TargetBoundingBoxOverlap(targetCamera, target, screenSpaceMin, screenSpaceMax, overlapPercent),
                SearchAreaComparison.TargetBoundingBoxInsideBoundingBox => TargetBoundingBoxInsideBoundingBox(targetCamera, target, screenSpaceMin, screenSpaceMax),
                _ => throw new ArgumentOutOfRangeException(nameof(comparison), comparison, null)
            };


        private void TrySelectDistractor()
        {
            if (!_targetRenderer)
            {
                GeneratePointCloudData();
                ChooseTargetDistractorInGroup(0);
                OnStudyStart();
                Debug.Log($"First object was selected");
                return;
            }
            if (IsTargetInView(_targetRenderer?.GetComponent<Collider>(), targetCamera, _searchAreaInViewSpace))
            {
                OnSelectionPressed(true);
                //ChooseTargetDistractor();
                ChooseTargetDistractorInGroup(0);
                Debug.Log($"Object is {Visible}");
                return;
            }
            OnSelectionPressed(false);
            Debug.Log($"Object is {NotVisible}");
            
        }

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

        private static bool IsTargetInView(Collider target, Camera camera, float4 searchArea)
        {
            if (!target)
            {
                return false;
            }
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            if (GeometryUtility.TestPlanesAABB(planes, target.bounds))
            {
                //object is on screen, check if in center 

                //return IsTargetInSearchArea(camera, searchArea.xy, searchArea.zw)
                var position = ((float3)camera.WorldToViewportPoint(target.bounds.center, camera.stereoActiveEye)).xy;
                return math.all(searchArea.xy <= position) && math.all(searchArea.zw >= position);

            }

            return false;
        }
        
        
        
        private void ChooseTargetDistractor()
        {
            _itemCount = _instantiatedPointCloudObjects.Count;
           
            SwapMaterial(_targetRenderer, defaultMaterial);
            var index = Random.Range(0, _itemCount);
            _targetRenderer = GetRenderer(index);
            SwapMaterial(_targetRenderer, targetMaterial);
            
        }

        private void ChooseTargetDistractorInGroup(int currentIndex)
        {
            if (_currentGroup == _groupCount)
            {
                OnStudyEnd();
                return;
            }
            var groupStartPosition = (_currentGroup / (float)_groupCount) * _maxLength;
            var groupEndPosition = (_currentGroup + 1) /  (float)_groupCount * _maxLength;

            var groupStartIndex = 0;
            var groupEndIndex = 0;

            for (int i = currentIndex; i < _instantiatedPointCloudObjects.Count; i++)
            {
                if (_instantiatedPointCloudObjects[i].splinePosition >= groupStartPosition)
                {
                    groupStartIndex = i;
                    break;
                }
            }

            for (int i = groupStartIndex; i < _instantiatedPointCloudObjects.Count; i++)
            {
                if (_instantiatedPointCloudObjects[i].splinePosition > groupEndPosition)
                {
                    groupEndIndex = i;
                    break;
                }
            }

            _currentGroup += 1;
            SwapMaterial(_targetRenderer, defaultMaterial);
            var index = Random.Range(groupStartIndex, groupEndIndex);
            _targetRenderer = GetRenderer(index);
            SwapMaterial(_targetRenderer, targetMaterial);
        }

        private MeshRenderer GetRenderer(int index)
        {
            if (index < 0 || index >= _instantiatedPointCloudObjects.Count)
            {
                throw new IndexOutOfRangeException(
                    $"Index is out of range. List has {_instantiatedPointCloudObjects.Count} objects but index is {index}");
            }
            return _instantiatedPointCloudObjects[index].instantiatedGameObject.GetComponent<MeshRenderer>();
        }

        private static void SwapMaterial(MeshRenderer renderer, Material material)
        {
            if (!renderer)
            {
                return;
            }
            renderer.material = material;
        }
        
        
        
    }

    [Serializable]
    public enum SearchAreaComparison
    {
        CenterInsideBoundingBox,
        BoundingBoxOverlap,
        TargetBoundingBoxInsideBoundingBox
    }
}