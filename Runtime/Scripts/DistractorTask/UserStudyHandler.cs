using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DistractorClouds.DistractorTask.StudyEventData;
using DistractorClouds.PanelGeneration;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace DistractorClouds.DistractorTask
{
    public abstract partial class BaseUserStudyHandler<T, TS> : MonoBehaviour where T : GenericAssetGenerator<TS> where TS : MonoBehaviour
    {
        public T assetGenerator;

        
        [SerializeField, Header("Distractor Task Settings"), Space(5f)]
        private Camera targetCamera;
        
        [Header("Trial Configuration")] [SerializeField]
        private int trialCount = 20;
        
        [SerializeField]
        private DistractorUserStudySetup setupComponent;

        [SerializeField]
        private bool spawnAllGroupsAtBeginning = true;

        [SerializeField]
        private int maxGroupCountToSpawn = 2;

        [SerializeField, Header("Debug"), Space(30f), Tooltip("Enable this to allow every click to skip the actual selection test")]
        private bool enableDebugModeAndOverrideSelection = false;
        
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

        private int _currentTarget;
        
        private RecenterPathComponent _recenterPathComponent;
        
        private SearchAreaHandler _searchAreaHandler;

        private AssetSpawnPoint[] _assetSpawnPoints;

        private DistractorGroupTrialEnumerator _groupTrialEnumerator;
        
        private static string CurrentTime => DateTime.Now.ToString("HH-mm-ss-ffff", CultureInfo.InvariantCulture);

        #endregion

        protected virtual void Start()
        {
            if (!setupComponent && TryGetComponent(out setupComponent))
            {
                Debug.LogError($"Missing {nameof(DistractorUserStudySetup)} component", this);
                this.enabled = false;
            }
            
            _searchAreaHandler = GetComponent<SearchAreaHandler>();
            _recenterPathComponent = GetComponent<RecenterPathComponent>();
            
            
        }

        private void OnDestroy()
        {
            assetGenerator.Dispose();
        }

        protected virtual void OnEnable()
        {
            InputHandler.Instance.OnRecenter += RepositionSpline;
            InputHandler.Instance.OnBumperDown += TrySelectDistractor;
        }
        
        protected virtual void OnDisable()
        {
            InputHandler.Instance.OnRecenter -= RepositionSpline;
            InputHandler.Instance.OnBumperDown -= TrySelectDistractor;
        }
        
        private void RepositionSpline()
        {
            Debug.Log("Reposition spline");
            var splineRepositioningData = _recenterPathComponent.RecenterPath();
            splineRepositioningData.TimeStamp = CurrentTime;
            OnSplineRepositioning(splineRepositioningData);
            
        }

        #region StudyEventMethods

        
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

        #endregion
        

        public virtual void StartTrial(PathComplexity pathComplexity, TaskLoad taskLoad)
        {
            _currentTarget = -1;
            _assetSpawnPoints = setupComponent.InitializeTrial(pathComplexity, taskLoad);
            
            assetGenerator.SpawnObjectsForPath(_assetSpawnPoints);
            _groupTrialEnumerator =
                new DistractorGroupTrialEnumerator(trialCount, _assetSpawnPoints[^1].Group + 1, maxGroupCountToSpawn);
            Debug.Log($"Number of groups: {_assetSpawnPoints[^1].Group + 1}");
            OnStudyStart();
            SelectNextTarget();
        }
        
        public virtual void TrySelectDistractor()
        {
            if (_currentTarget < 0 || _currentTarget >= assetGenerator.ActiveObjects.Count)
            {
                return;
            }

            

            var target = assetGenerator.ActiveObjects[_currentTarget];
            
#if UNITY_EDITOR
            if (enableDebugModeAndOverrideSelection)
            {
                OnSelectionPressed(true, target.ActiveObject.transform.position, InputHandler.Instance.PointerPosition, InputHandler.Instance.PointerRotation);
                //ChooseTargetDistractor();
                SelectNextTarget();
                return;
            }
#endif
            var targetCollider = target.ActiveObject.GetComponent<Collider>();
            if (IsTargetInView(targetCollider, targetCamera, _searchAreaHandler))
            {
                OnSelectionPressed(true, target.ActiveObject.transform.position, InputHandler.Instance.PointerPosition, InputHandler.Instance.PointerRotation);
                //ChooseTargetDistractor();
                SelectNextTarget();
                return;
            }
            Debug.Log("Object not in view");
            OnSelectionPressed(false, target.ActiveObject.transform.position, InputHandler.Instance.PointerPosition, InputHandler.Instance.PointerRotation);
            
        }

        private void SelectNextTarget()
        {

            DeselectCurrentTarget(_currentTarget);

            if (_groupTrialEnumerator.MoveNext())
            {
                if (_groupTrialEnumerator.ReachedEndOfSpline)
                {
                    OnSplineEndReached(_groupTrialEnumerator.MovingForwardOnSpline ? 1f : 0f);
                }
                var groupData = _groupTrialEnumerator.Current;
                if (!spawnAllGroupsAtBeginning)
                {
                    assetGenerator.SpawnObjectsForPath(_assetSpawnPoints, groupData.GroupRange.x, groupData.GroupRange.y);
                }

                
                var groupIndices = FindStartAndEndGroupIndex(assetGenerator.ActiveObjects, groupData.CurrentGroup);
                _currentTarget = Random.Range(groupIndices.x, groupIndices.y);
                var nextTarget = assetGenerator.ActiveObjects[_currentTarget];
                Debug.Log($"Current group: {groupData.CurrentGroup}", nextTarget.ActiveObject);
                
                Assert.AreEqual(nextTarget.Group, groupData.CurrentGroup);
                SelectTarget(nextTarget);
                return;
            }

            EndStudy();


        }

        private void EndStudy()
        {
            Debug.Log("Study is over");
            assetGenerator.Reset();
            _currentTarget = -1;
            OnStudyEnd();
            _groupTrialEnumerator.Reset();
        }

        private void DeselectCurrentTarget(int currentTarget)
        {
            if (currentTarget < 0 || currentTarget > assetGenerator.ActiveObjects.Count)
            {
                return;
            }

            DeselectTarget(assetGenerator.ActiveObjects[currentTarget]);
        }

        public abstract void DeselectTarget(GroupObject<TS> assetGeneratorActiveObject);
        public abstract void SelectTarget(GroupObject<TS> assetGeneratorActiveObject);

        private static int2 FindStartAndEndGroupIndex(List<GroupObject<TS>> spawnPoints, int group)
        {
            var groupStartIndex = 0;
            var groupEndIndex = spawnPoints.Count;

            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (spawnPoints[i].Group == group)
                {
                    groupStartIndex = i;
                    break;
                }
            }

            for (int i = groupStartIndex; i < spawnPoints.Count; i++)
            {
                if (spawnPoints[i].Group > group)
                {
                    groupEndIndex = i;
                    break;
                }
            }

            Debug.Log($"Range: {groupStartIndex} to {groupEndIndex}");
            return new int2(groupStartIndex, groupEndIndex);
        }
        

        private static bool IsTargetInView(Collider target, Camera targetCamera, SearchAreaHandler searchAreaHandler)
        {
            Debug.Log("Checking visibility");
            if (!target)
            {
                Debug.Log("Object does not exist");
                return false;
            }
            var planes = GeometryUtility.CalculateFrustumPlanes(targetCamera);
            if (GeometryUtility.TestPlanesAABB(planes, target.bounds))
            {
                Debug.Log("Object on screen");
                //object is on screen, check if in center 
                //return IsTargetInSearchArea(camera, searchArea.xy, searchArea.zw)
                return searchAreaHandler.IsTargetInSearchArea(targetCamera, target);

            }
            Debug.Log("Object not on screen");

            return false;
        }
    }
    
}