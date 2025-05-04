using System;
using System.Collections.Generic;
using System.Globalization;
using DistractorClouds.DistractorTask.StudyEventData;
using DistractorClouds.PanelGeneration;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace DistractorClouds.DistractorTask
{
    [RequireComponent(typeof(SearchAreaHandler))]
    public class DistractorTaskManager : MonoBehaviour
    {
        [Header("Distractor Task Settings")][Space(5f)]
        public Camera targetCamera;

        public ClosestSplinePointGeneration pointGenerator;
        
        public Material defaultMaterial;
        public Material targetMaterial;

        
        private SearchAreaHandler _searchAreaHandler;

        
        #region EventHooks

        
        public Action<StudyStartData> OnStudyStartEvent = delegate {};
        [Header("Event Hooks")][Space(5f)]
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

        private int _currentGroup;

        private int _groupCount;

        private List<ClosestSplinePointGeneration.InstantiatedPointCloudObject> _instantiatedPointCloudObjects = new();
        private float _maxLength;


        private static string CurrentTime => DateTime.Now.ToString("HH-mm-ss-ffff", CultureInfo.InvariantCulture);

        #endregion

        
        private void OnStudyStart()
        {
            var studyStartData = new StudyStartData
            {
                TimeStamp = CurrentTime
            };
            OnStudyStartEvent.Invoke(studyStartData);
            onStudyStartEvent.Invoke(studyStartData);
        }

        private void OnSelectionPressed(bool validSelection, Vector3 targetPosition)
        {
            var selectionPressedData = new SelectionData
            {
                TimeStamp = CurrentTime,
                IsValidSelection = validSelection,
                TargetPosition = targetPosition
            };
            
            OnSelectionPressedEvent.Invoke(selectionPressedData);
            onSelectionPressedEvent.Invoke(selectionPressedData);
        }
        
        private void OnStudyEnd()
        {
            var studyEndData = new StudyEndData()
            {
                TimeStamp = CurrentTime
            };
            
            OnStudyEndEvent.Invoke(studyEndData);
            onStudyEndEvent.Invoke(studyEndData);
        }
        
        private void OnSplineEndReached(float splinePosition)
        {
            var studyPathEndPointReachedData = new StudyPathEndPointReachedData()
            {
                TimeStamp = CurrentTime,
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
                TimeStamp = CurrentTime,
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
            _searchAreaHandler = GetComponent<SearchAreaHandler>();
            FindFirstObjectByType<InputHandler>().OnBumperDown += TrySelectDistractor;
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



        public void StartDistractorCloudTask()
        {
            GeneratePointCloudData();
            ChooseTargetDistractorInGroup(0);
            OnStudyStart();
            Debug.Log($"First object was selected");
        }


        private void TrySelectDistractor()
        {
            if (!_targetRenderer)
            {
#if UNITY_EDITOR
                StartDistractorCloudTask();
#endif
                return;
            }
            if (IsTargetInView(_targetRenderer?.GetComponent<Collider>(), targetCamera, _searchAreaHandler))
            {
                OnSelectionPressed(true, _targetRenderer.transform.position);
                //ChooseTargetDistractor();
                ChooseTargetDistractorInGroup(0);
                Debug.Log($"Object is {Visible}");
                return;
            }
            OnSelectionPressed(false, _targetRenderer.transform.position);
            Debug.Log($"Object is {NotVisible}");
            
        }

        

        private static bool IsTargetInView(Collider target, Camera camera, SearchAreaHandler searchAreaHandler)
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
                return searchAreaHandler.IsTargetInSearchArea(camera, target);

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
    
}