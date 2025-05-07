using System;
using System.Collections.Generic;
using System.Linq;
using DistractorClouds.Attributes;
using DistractorClouds.Noise;
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
        
        [HideInInspector]
        public SamplePointAsset samplePointAsset;

        public BlueNoiseSamplePointAsset[] blueNoiseSamplePoints;

        [SerializeField]
        private SplineContainer[] splineContainer = Array.Empty<SplineContainer>();

        [Layer]
        public int distractorLayer = 3;
        
        [Tooltip("Determines the number of objects per square meter for the given object")]
        public float density = 20f;

        public float minDistance = 0.1f;

        public float height = 1f;

        [Tooltip("Spacing of object sections. X value is filled with objects, y value is empty between sections")]
        public Vector2 spacing = new Vector2(6, 4);
        public int seed;
        
        [SerializeField]
        private Material[] debugMaterials;
        
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
            public int group;
            public GameObject instantiatedGameObject;
        }

        private void Start()
        {
            //CreateDistractors();
            var assetSpawnPoints = CreateAssetSpawnPoints(ref splineContainer, ref blueNoiseSamplePoints, spacing, height, minDistance, density, seed);
            var ownTransform = this.transform;
            Debug.Log(assetSpawnPoints.Length);
            SpawnDistractors(ref assetSpawnPoints, ref itemsToInstantiate, ref ownTransform, distractorLayer);
        }

        public static AssetSpawnPoint[] CreateAssetSpawnPoints(ref SplineContainer[] splineContainer,
            ref BlueNoiseSamplePointAsset[] samplePointAssets, float2 spacing, float height, float minRadius,
            float actualDensity, int seed)
        {
            var splineGenerationFromWaypoint = splineContainer
                .Where(spline => spline.GetComponent<SplineGenerationFromWaypoints>())
                .Select(s => s.GetComponent<SplineGenerationFromWaypoints>()).ToList();

            foreach (var spline in splineGenerationFromWaypoint)
            {
                spline.BuildSpline();
            }
            
            Random.InitState(seed);
            
            //todo put that somewhere else
            //EnsureItemsValidity();
            
            if ( splineContainer.Length == 0)
            {
                Debug.LogWarning("No objects or splines specified");
                return Array.Empty<AssetSpawnPoint>();
            }
            
            
            var maxObjectEstimate = (int)math.round(actualDensity * splineContainer.Length * splineContainer[0].CalculateLength());
            var createdObjects = new List<AssetSpawnPoint>(maxObjectEstimate);
            

            
            var groupCount = 0;

            foreach (var spline in splineContainer)
            {
                var splineLength = spline.CalculateLength();
                groupCount = math.max(groupCount, (int)math.floor(splineLength / (spacing.x + spacing.y)));
            }
            
            
            foreach (var spline in splineContainer)
            {

                var samplePointAsset = samplePointAssets[Random.Range(0, samplePointAssets.Length)];
                
                if (samplePointAsset.dimensions.y < height)
                {
                    throw new NotImplementedException();
                }

                var heightDif = samplePointAsset.dimensions.y - height;
            
                var startPoint = new float2(0, Random.Range(0, heightDif));

                var scaledAsset = samplePointAsset.ScaleAsset(minRadius);

                scaledAsset = scaledAsset.FilterAsset(new float4(startPoint, scaledAsset.dimensions.x, startPoint.y + height));


           
            
            
                var samplePointDensity = scaledAsset.samplePoints.Length /
                                         (scaledAsset.dimensions.x * scaledAsset.dimensions.y);
                
                //width that the sample point asset should cover -> we can't make it smaller than the actual width or we are violating the radius requirement (in that case the density is not feasible anyways 
                var samplePointAssetWidth = math.max((samplePointDensity / actualDensity) * scaledAsset.dimensions.x, scaledAsset.dimensions.x);
                Debug.Log(samplePointAssetWidth);

                var splineLength = spline.CalculateLength();
                
                var repeatsOfSamplePointAsset = splineLength / samplePointAssetWidth;
                
                Debug.Log(repeatsOfSamplePointAsset);
                Debug.Log($"Remaining points: {scaledAsset.samplePoints.Length}");
                int repeats = (int)math.ceil(repeatsOfSamplePointAsset);
                
                var pointsGenerated = 0;

                var percentageOfSplinePerAsset = 1f / repeatsOfSamplePointAsset;
                
                for (int currentRepeat = 0; currentRepeat < repeats; currentRepeat++)
                {
                    var currentTStart = percentageOfSplinePerAsset * currentRepeat;

                    foreach (var samplePoint in scaledAsset.samplePoints)
                    {
                        var lerpValue = samplePoint.x / scaledAsset.dimensions.x;
                        var t = math.lerp(currentTStart, currentTStart + percentageOfSplinePerAsset, lerpValue);

                        var currentGroup = (int)(t * (splineLength / (spacing.x + spacing.y)));
                        
                        if (!IsPointPositionValid(t, splineLength, spacing, currentGroup, groupCount))
                        {
                            continue;
                        }

                        pointsGenerated++;

                        var position = spline.EvaluatePosition(t);
                        position.y += samplePoint.y;
                        
                        createdObjects.Add(new AssetSpawnPoint()
                        {
                            T = t,
                            Position = position,
                            ProbabilityValue = samplePoint.z,
                            Group = currentGroup
                        });

                        
                    }
                    
                }

                var groupsForSpline = splineLength / (spacing.x + spacing.y);
                Debug.Log($"Density for spline {pointsGenerated / (groupsForSpline * spacing.x * height)}");
            }
            

            return createdObjects.ToArray();
        }

        public static InstantiatedPointCloudObject[] SpawnDistractors(ref AssetSpawnPoint[] spawnPoints,
            ref SplineInstantiate.InstantiableItem[] itemsToInstantiate, ref Transform parentTransform, int distractorLayer)
        {

            var maxProbability = EnsureItemsValidity(ref itemsToInstantiate, parentTransform);
            var result = new InstantiatedPointCloudObject[spawnPoints.Length];
            for (var i = 0; i < spawnPoints.Length; i++)
            {
                var spawnPoint = spawnPoints[i];
                result[i] = (new InstantiatedPointCloudObject
                {
                    splinePosition = spawnPoint.T,
                    instantiatedGameObject = SpawnPrefab(ref itemsToInstantiate, spawnPoint.ProbabilityValue, spawnPoint.Position,
                        parentTransform, maxProbability, distractorLayer),
                    group = spawnPoint.Group
                });
            }

            return result;
        }

        
        
        [Obsolete]
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
            var actualDensity = density;
            


            EnsureItemsValidity();
            
            
            
            if (itemsToInstantiate.Length == 0 || splineContainer.Length == 0)
            {
                Debug.LogWarning("No objects or splines specified", this);
                _instantiatedObjects = Array.Empty<InstantiatedPointCloudObject>();
                return;
            }
            var maxObjectEstimate = (int)math.round(actualDensity * splineContainer.Length * splineContainer[0].CalculateLength());
            var createdObjects = new List<InstantiatedPointCloudObject>(maxObjectEstimate);
            

            /*var scaledAsset =
                samplePointAsset.ScaleSamplePoints((100f * samplePointAsset.scale) /
                                                   samplePointAsset.boundingBoxSize.x);*/
            
            var scaledAsset =
                samplePointAsset.AlternativeScaleSamplePoints(new int2(100, 100));
            var samplePointDensity =
                scaledAsset.samplePoints.Length; //we already scaled the asset to one square meter (but in cm)
            var samplePointAssetPerMeter = actualDensity / samplePointDensity;
            
            var points = new NativeArray<float3>(scaledAsset.samplePoints, Allocator.TempJob);
            var listOfPoints = new NativeList<float3>(scaledAsset.samplePoints.Length, Allocator.TempJob);


            var lowY = scaledAsset.samplePoints.Select(sample => sample.y).Min();
            var highY = scaledAsset.samplePoints.Select(sample => sample.y).Max();
            var yOffset = (highY - lowY) / 100f;
            
            var maxLength = 0f;

            foreach (var spline in splineContainer)
            {
                var splineLength = spline.CalculateLength();
                GroupCount = math.max(GroupCount, (int)math.floor(splineLength / (spacing.x + spacing.y)));
            }
            
            foreach (var spline in splineContainer)
            {
                
                var splineLength = spline.CalculateLength();
                

                maxLength = math.max(splineLength, maxLength);

                
                
                var repeatsOfSamplePointAsset = splineLength * samplePointAssetPerMeter;

                int fullRepeats = (int)math.floor(repeatsOfSamplePointAsset);
                

                var percentageOfSplinePerAsset = 1f / repeatsOfSamplePointAsset;

                var objectsPerSpline = 0;
                
                var tOffset = 0f;

                for (int i = 0; i < fullRepeats + 1; i++)
                {
                    listOfPoints.AddRange(points);

                    while (listOfPoints.Length > 0)
                    {
                        objectsPerSpline++;
                        var pointIndex = Random.Range(0, listOfPoints.Length);
                        var samplePoint = listOfPoints[pointIndex];
                        listOfPoints.RemoveAt(pointIndex);
                        var lerpValue = samplePoint.x / 100f;
                        var t = math.lerp(tOffset, tOffset + percentageOfSplinePerAsset, lerpValue);

                        var currentGroup = (int)(t * (splineLength / (spacing.x + spacing.y)));
                        
                        if (!IsPointPositionValid(t, splineLength, spacing, currentGroup, GroupCount))
                        {
                            continue;
                        }

                        

                        var position = spline.EvaluatePosition(t);
                        position.y += samplePoint.y / 100f;

                        createdObjects.Add(new InstantiatedPointCloudObject
                        {
                            splinePosition = t,
                            instantiatedGameObject = SpawnPrefab(ref itemsToInstantiate, samplePoint.z, position,
                                transform, _maxProbability, distractorLayer),
                            group = currentGroup
                        });
                    }

                    tOffset += percentageOfSplinePerAsset;
                }
                
                
                Debug.Log($"Spline density: {objectsPerSpline / (splineLength * yOffset)}");
            }
            

            points.Dispose();
            listOfPoints.Dispose();
            
            _instantiatedObjects = createdObjects.ToArray();

            MaxLength = maxLength;
            Debug.Log($"Generated {_instantiatedObjects.Length} objects");
        }

        private static bool IsPointPositionValid(float t, float splineLength, Vector2 spacing, int currentGroup,
            int maxGroupCount)
        {
            var moduloValue = (t * splineLength) % (spacing.x + spacing.y);

            return currentGroup < maxGroupCount && moduloValue <= spacing.x && t is >= 0 and <= 1.0f;
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


        [Obsolete]
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
        
        private static float EnsureItemsValidity(ref SplineInstantiate.InstantiableItem[] itemsToInstantiate, Transform parentTransform)
        {
            float probability = 0;
            for (int i = 0; i < itemsToInstantiate.Length; i++)
            {
                var item = itemsToInstantiate[i];

                if (item.Prefab != null)
                {
                    if (parentTransform.IsChildOf(item.Prefab.transform))
                    {
                        Debug.LogWarning("Instantiating a parent of the SplineInstantiate object itself is not permitted" +
                                         $" ({item.Prefab.name} is a parent of {parentTransform.gameObject.name}).", parentTransform);
                        item.Prefab = null;
                        itemsToInstantiate[i] = item;
                    }
                    else
                        probability += item.Probability;
                }
            }
            return probability;
        }
        
    }
}