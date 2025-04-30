using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using Random = UnityEngine.Random;

namespace DistractorClouds.PanelGeneration
{
    [RequireComponent(typeof(BoxCollider))]
    [AddComponentMenu("DistractorCloud/PanelGenerator")]
    public class PanelGenerator : MonoBehaviour
    {


        public SamplePointAsset samplePointAsset;

        public SplineInstantiate.InstantiableItem[] itemsToInstantiate = Array.Empty<SplineInstantiate.InstantiableItem>();

        [Tooltip("Determines the number of objects per square meter for the given object")]
        public Vector2 density = new Vector2(2, 2);

        public int seed;

        private BoxCollider _bounds;

        private GameObject[] _instantiatedObjects;

        private float _maxProbability;
    
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _bounds = GetComponent<BoxCollider>();
            Random.InitState(seed);
            var actualDensity = Random.Range(density.x, density.y);

            var area = _bounds.size.x * _bounds.size.y;
            var objectCount = (int)math.round(actualDensity * area);

            _instantiatedObjects = new GameObject[objectCount];

            if (itemsToInstantiate.Length == 0)
            {
                Debug.LogWarning("No objects specified");
                return;
            }
            EnsureItemsValidity();

            var offset = transform.position + _bounds.center - _bounds.size / 2f;
            
            var clone = samplePointAsset.ScaleSamplePoints(100f * _bounds.size.x);
            var maxOffset = clone.samplePoints.Length / itemsToInstantiate.Length;
            var startPoint = Random.Range(0, clone.samplePoints.Length);
            for (int i = 0; i < objectCount; i++)
            {
                var samplePoint = clone.samplePoints[startPoint];
                
                SpawnPrefab(i, offset + new Vector3(samplePoint.x / 100f, samplePoint.y / 100f, 0));
                
                startPoint += Random.Range(1, maxOffset);
                startPoint %= clone.samplePoints.Length;
            }
            
            Debug.Log($"Generated {objectCount} objects");
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
        
        int GetPrefabIndex()
        {
            var prefabChoice = Random.Range(0, _maxProbability);
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
        
        
        private bool SpawnPrefab(int index, Vector3 position)
        {


            var prefabIndex = itemsToInstantiate.Length == 1 ? 0 : GetPrefabIndex();
            var currentItem = itemsToInstantiate[prefabIndex];

            if (currentItem.Prefab == null)
                return false;

            if (index < _instantiatedObjects.Length)
            {
#if UNITY_EDITOR
                var assetType = PrefabUtility.GetPrefabAssetType(currentItem.Prefab);
                if (assetType == PrefabAssetType.MissingAsset)
                {
                    Debug.LogError($"Trying to instantiate a missing asset for item index [{prefabIndex}].", this);
                    return false;
                }

                if (assetType != PrefabAssetType.NotAPrefab && !Application.isPlaying)
                    _instantiatedObjects[index] = PrefabUtility.InstantiatePrefab(currentItem.Prefab, transform) as GameObject;
                else
#endif
                    _instantiatedObjects[index] = Instantiate(currentItem.Prefab, transform);

#if UNITY_EDITOR
                _instantiatedObjects[index].hideFlags |= HideFlags.HideAndDontSave;
                // Retrieve current static flags to pass them along on created instances
                var staticFlags = GameObjectUtility.GetStaticEditorFlags(gameObject);
                GameObjectUtility.SetStaticEditorFlags(_instantiatedObjects[index], staticFlags);
#endif
                
                
                _instantiatedObjects[index].transform.SetPositionAndRotation(position, Quaternion.identity);

                return true;
            }
            return false;
        }
    }
}
