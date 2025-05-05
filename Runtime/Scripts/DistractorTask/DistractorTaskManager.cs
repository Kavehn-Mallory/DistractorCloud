using System;
using System.Collections.Generic;
using System.Globalization;
using DistractorClouds.DistractorTask.StudyEventData;
using DistractorClouds.PanelGeneration;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace DistractorClouds.DistractorTask
{
    [RequireComponent(typeof(SearchAreaHandler))]
    public class DistractorTaskManager : MonoBehaviour
    {
        [SerializeField, Header("Distractor Task Settings"), Space(5f)]
        private Camera targetCamera;

        [SerializeField]
        private ClosestSplinePointGeneration pointGenerator;

        [SerializeField]
        private Material defaultMaterial;
        [SerializeField]
        private Material targetMaterial;

        [Header("Trial Configuration")] [SerializeField]
        private int trialCount = 20;

        [SerializeField]
        private GameObject[] trialGroups = Array.Empty<GameObject>();

        
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

        private int _currentGroup;

        private int _groupCount;

        private int _currentTrialCount;

        /// <summary>
        /// Is set to false as soon as we have reached the end of the spline with trials left 
        /// </summary>
        private bool _movingForwardOnSpline = true;

        private List<ClosestSplinePointGeneration.InstantiatedPointCloudObject> _instantiatedPointCloudObjects = new();
        private float _maxLength;

        
        private RecenterPathComponent _recenterPathComponent;
        
        
        private SearchAreaHandler _searchAreaHandler;

        private static string CurrentTime => DateTime.Now.ToString("HH-mm-ss-ffff", CultureInfo.InvariantCulture);

        #endregion
        
        private void OnEnable()
        {
            InputHandler.Instance.OnRecenter += RepositionSpline;
        }

        private void RepositionSpline()
        {
            var splineRepositioningData = _recenterPathComponent.RecenterPath();
            splineRepositioningData.TimeStamp = CurrentTime;
            OnSplineRepositioning(splineRepositioningData);
            
        }

        private void OnDisable()
        {
            InputHandler.Instance.OnRecenter -= RepositionSpline;
        }
        
        private void OnStudyStart()
        {
            var studyStartData = new StudyStartData
            {
                TimeStamp = CurrentTime
            };
            OnStudyStartEvent.Invoke(studyStartData);
            onStudyStartEvent.Invoke(studyStartData);
        }

        private void OnSelectionPressed(bool validSelection, Vector3 targetPosition, Vector3 controllerPosition, Quaternion controllerDirection)
        {
            var selectionPressedData = new SelectionData
            {
                TimeStamp = CurrentTime,
                IsValidSelection = validSelection,
                TargetPosition = targetPosition,
                ControllerDirection = controllerDirection,
                ControllerPosition = controllerPosition
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


        private void OnSplineRepositioning(SplineRepositioningData repositioningData)
        {
            OnSplineRepositioningEvent.Invoke(repositioningData);
            onSplineRepositioningEvent.Invoke(repositioningData);
        }
        
        private void Start()
        {
            _searchAreaHandler = GetComponent<SearchAreaHandler>();
            _recenterPathComponent = GetComponent<RecenterPathComponent>();
            InputHandler.Instance.OnBumperDown += TrySelectDistractor;
            
        }

        private void GeneratePointCloudData()
        {
            _instantiatedPointCloudObjects.AddRange(pointGenerator.InstantiatedGameObjects);
            _instantiatedPointCloudObjects.Sort(ComparePointCloudObjectsByPosition);
            _groupCount = pointGenerator.GroupCount;
            _maxLength = pointGenerator.MaxLength;
            _currentGroup = 0;
            _currentTrialCount = 0;
            _movingForwardOnSpline = true;
        }

        private static int ComparePointCloudObjectsByPosition(ClosestSplinePointGeneration.InstantiatedPointCloudObject x,
            ClosestSplinePointGeneration.InstantiatedPointCloudObject y)
        {
            return x.splinePosition.CompareTo(y.splinePosition);
        }
        

        public void StartDistractorCloudTask(int trialGroup)
        {
            _currentGroup = 0;
            _currentTrialCount = 0;
            _movingForwardOnSpline = true;
            GeneratePointCloudData();
            ChooseTargetDistractorInGroup();
            OnStudyStart();
            Debug.Log($"First object was selected");
        }


        private void TrySelectDistractor()
        {
            if (!_targetRenderer)
            {
#if UNITY_EDITOR
                StartDistractorCloudTask(0);
#endif
                return;
            }
            if (IsTargetInView(_targetRenderer?.GetComponent<Collider>(), targetCamera, _searchAreaHandler))
            {
                OnSelectionPressed(true, _targetRenderer.transform.position, InputHandler.Instance.PointerPosition, InputHandler.Instance.PointerRotation);
                //ChooseTargetDistractor();
                ChooseTargetDistractorInGroup();
                Debug.Log($"Object is {Visible}");
                return;
            }
            OnSelectionPressed(false, _targetRenderer.transform.position, InputHandler.Instance.PointerPosition, InputHandler.Instance.PointerRotation);
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
        
        
        

        private void ChooseTargetDistractorInGroup()
        {
            if (ReachedEndOfSpline(_currentGroup, _groupCount - 1, _movingForwardOnSpline))
            {
                //turn around
                _movingForwardOnSpline = !_movingForwardOnSpline;
                OnSplineEndReached(_movingForwardOnSpline ? 0f : 1f);
                _currentGroup -= 2;
            }

            if (_currentTrialCount == trialCount)
            {
                OnStudyEnd();
                return;
            }
            
            var groupStartPosition = (_currentGroup / (float)_groupCount) * _maxLength;
            var groupEndPosition = (_currentGroup + 1) /  (float)_groupCount * _maxLength;

            var groupStartIndex = 0;
            var groupEndIndex = 0;

            for (int i = 0; i < _instantiatedPointCloudObjects.Count; i++)
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

            
            _currentTrialCount += 1;
            _currentGroup = _movingForwardOnSpline ? _currentGroup + 1 : _currentGroup - 1;
            SwapMaterial(_targetRenderer, defaultMaterial);
            var index = Random.Range(groupStartIndex, groupEndIndex);
            _targetRenderer = GetRenderer(index);
            SwapMaterial(_targetRenderer, targetMaterial);
        }

        private bool ReachedEndOfSpline(int currentGroup, int groupCount, bool movingForwardOnSpline)
        {
            if (_movingForwardOnSpline)
            {
                return currentGroup == groupCount;
            }

            return currentGroup == 0;
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