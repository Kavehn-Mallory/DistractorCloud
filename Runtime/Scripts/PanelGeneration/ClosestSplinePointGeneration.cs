using System;
using System.Collections.Generic;
using System.Linq;
using DistractorClouds.Attributes;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using Random = UnityEngine.Random;

namespace DistractorClouds.PanelGeneration
{
    public class ClosestSplinePointGeneration : MonoBehaviour
    {
        public SamplePointAsset samplePointAsset;

        [SerializeField]
        private SplineContainer[] splineContainer = Array.Empty<SplineContainer>();

        [Layer]
        public int distractorLayer = 3;
        
        [Tooltip("Determines the number of objects per square meter for the given object")]
        public Vector2 density = new Vector2(2, 2);

        [Tooltip("Spacing of object sections. X value is filled with objects, y value is empty between sections")]
        public Vector2 spacing = new Vector2(6, 4);
        public int seed;

        public bool createDistractorsOnStart;
        
        public int GroupCount { get; private set; }
        public float MaxLength { get; private set; }
        
        public SplineInstantiate.InstantiableItem[] itemsToInstantiate = Array.Empty<SplineInstantiate.InstantiableItem>();
        
        private InstantiatedPointCloudObject[] _instantiatedObjects;

        private float _maxProbability;
        

        public InstantiatedPointCloudObject[] InstantiatedGameObjects => _instantiatedObjects;

        [Serializable]
        public struct InstantiatedPointCloudObject
        {
            public float splinePosition;
            public GameObject instantiatedGameObject;
        }

        private void Start()
        {
            CreateDistractors();
        }
        
        public void CreateDistractors()
        {

            var splineGenerationFromWaypoint = splineContainer
                .Where(spline => spline.GetComponent<SplineGenerationFromWaypoints>())
                .Select(s => s.GetComponent<SplineGenerationFromWaypoints>()).ToList();

            foreach (var spline in splineGenerationFromWaypoint)
            {
                spline.BuildSpline();
            }
            
            Debug.Log("Beginning with generation of objects");
            Random.InitState(seed);
            var actualDensity = Random.Range(density.x, density.y);
            


            EnsureItemsValidity();
            
            
            
            if (itemsToInstantiate.Length == 0 || splineContainer.Length == 0)
            {
                Debug.LogWarning("No objects or splines specified", this);
                _instantiatedObjects = Array.Empty<InstantiatedPointCloudObject>();
                return;
            }
            var maxObjectEstimate = (int)math.round(actualDensity * splineContainer.Length * splineContainer[0].CalculateLength());
            var createdObjects = new List<InstantiatedPointCloudObject>(maxObjectEstimate);
            

            var scaledAsset =
                samplePointAsset.ScaleSamplePoints((100f * samplePointAsset.scale) /
                                                   samplePointAsset.boundingBoxSize.x);
            var samplePointDensity =
                scaledAsset.samplePoints.Length; //we already scaled the asset to one square meter (but in cm)
            var samplePointAssetPerMeter = actualDensity / samplePointDensity;
            
            var points = new NativeArray<float3>(scaledAsset.samplePoints, Allocator.TempJob);
            var listOfPoints = new NativeList<float3>(scaledAsset.samplePoints.Length, Allocator.TempJob);

            var maxLength = 0f;
            
            foreach (var spline in splineContainer)
            {
                var splineLength = spline.CalculateLength();
                
                GroupCount = math.max(GroupCount, (int)math.floor(splineLength / (spacing.x + spacing.y)));

                maxLength = math.max(splineLength, maxLength);
                
                
                
                
                var repeatsOfSamplePointAsset = splineLength * samplePointAssetPerMeter;

                int fullRepeats = (int)math.floor(repeatsOfSamplePointAsset);
                

                var percentageOfSplinePerAsset = 1f / repeatsOfSamplePointAsset;


                var tOffset = 0f;

                for (int i = 0; i < fullRepeats + 1; i++)
                {
                    listOfPoints.AddRange(points);

                    while (listOfPoints.Length > 0)
                    {
                        var pointIndex = Random.Range(0, listOfPoints.Length);
                        var samplePoint = listOfPoints[pointIndex];
                        listOfPoints.RemoveAt(pointIndex);
                        var lerpValue = samplePoint.x / 100f;
                        var t = math.lerp(tOffset, tOffset + percentageOfSplinePerAsset, lerpValue);

                        if (!IsPointPositionValid(t, splineLength, spacing))
                        {
                            continue;
                        }


                        var position = spline.EvaluatePosition(t);
                        position.y += samplePoint.y / 100f;

                        createdObjects.Add(new InstantiatedPointCloudObject
                        {
                            splinePosition = t * splineLength,
                            instantiatedGameObject = SpawnPrefab(ref itemsToInstantiate, samplePoint.z, position,
                                transform, _maxProbability, distractorLayer)
                        });
                    }

                    tOffset += percentageOfSplinePerAsset;
                }
            }
            

            points.Dispose();
            listOfPoints.Dispose();
            
            _instantiatedObjects = createdObjects.ToArray();

            MaxLength = maxLength;
            Debug.Log($"Generated {_instantiatedObjects.Length} objects");
        }

        private static bool IsPointPositionValid(float t, float splineLength, Vector2 spacing)
        {
            var moduloValue = (t * splineLength) % (spacing.x + spacing.y);

            return moduloValue <= spacing.x && t is >= 0 and <= 1.0f;
        }


        private static int GetPrefabIndex(ref SplineInstantiate.InstantiableItem[] itemsToInstantiate, float prefabChoice)
        {
            var currentProbability = 0f;
            for (int i = 0; i < itemsToInstantiate.Length; i++)
            {
                if (itemsToInstantiate[i].Prefab == null)
                    continue;

                var itemProbability = itemsToInstantiate[i].Probability;
                if (prefabChoice < currentProbability + itemProbability)
                    return i;

                currentProbability += itemProbability;
            }

            return 0;
        }

        
        private static GameObject SpawnPrefab(ref SplineInstantiate.InstantiableItem[] itemsToInstantiate,
            float prefabChoice, Vector3 position, Transform parentTransform, float maxProbability, int distractorLayer)
        {


            var prefabIndex = itemsToInstantiate.Length == 1 ? 0 : GetPrefabIndex(ref itemsToInstantiate, prefabChoice * maxProbability);
            var currentItem = itemsToInstantiate[prefabIndex];

            if (currentItem.Prefab == null)
                return null;
            
            GameObject gameObject;
#if UNITY_EDITOR
            var assetType = PrefabUtility.GetPrefabAssetType(currentItem.Prefab);
            if (assetType == PrefabAssetType.MissingAsset)
            {
                Debug.LogError($"Trying to instantiate a missing asset for item index [{prefabIndex}].",
                    parentTransform);
                return null;
            }

            
            if (assetType != PrefabAssetType.NotAPrefab && !Application.isPlaying)
                gameObject = PrefabUtility.InstantiatePrefab(currentItem.Prefab, parentTransform) as GameObject;
            else
#endif
                gameObject = Instantiate(currentItem.Prefab, parentTransform);

#if UNITY_EDITOR
            if (gameObject == null)
            {
                return null;
            }

            gameObject.hideFlags |= HideFlags.HideAndDontSave;
            // Retrieve current static flags to pass them along on created instances
            var staticFlags = GameObjectUtility.GetStaticEditorFlags(gameObject);
            GameObjectUtility.SetStaticEditorFlags(gameObject, staticFlags);
#endif
            
            gameObject.transform.SetPositionAndRotation(position, Quaternion.identity);
            gameObject.layer = distractorLayer;

            return gameObject;
        }


        void EnsureItemsValidity()
        {
            float probability = 0;
            for (int i = 0; i < itemsToInstantiate.Length; i++)
            {
                var item = itemsToInstantiate[i];

                if (item.Prefab != null)
                {
                    if (transform.IsChildOf(item.Prefab.transform))
                    {
                        Debug.LogWarning("Instantiating a parent of the SplineInstantiate object itself is not permitted" +
                                         $" ({item.Prefab.name} is a parent of {transform.gameObject.name}).", this);
                        item.Prefab = null;
                        itemsToInstantiate[i] = item;
                    }
                    else
                        probability += item.Probability;
                }
            }
            _maxProbability = probability;
        }
        
    }
}