using System.Collections;
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace DistractorClouds.PanelGeneration
{
    public class BlueNoiseAssetGenerator : MonoBehaviour
    {

        [Header("Blue Noise Settings")]
        
        [SerializeField]
        private float2 assetDimensions = new float2(1f, 1f);

        [SerializeField] private float minDistance = 0.01f;

        [SerializeField] private int k = 30;

        [SerializeField] private bool useGrowingK;
        
        
        [Space(10f), Header("Sample Point Asset Settings")]
        
        [SerializeField]
        private string pathToSamplePointAsset = "Assets/ScriptableObjects";

        [SerializeField]
        private string samplePointAssetName = "SamplePointAsset";
        
        
        private JobHandle _blueNoiseJobHandle;
        private BlueNoiseJob _blueNoiseJob;
        
        
        [ContextMenu("Generate Blue Noise Asset")]
        private void GenerateBlueNoise()
        {
            if (!_blueNoiseJobHandle.IsCompleted)
            {
                Debug.LogError("Job is still running");
            }
            if (_blueNoiseJob.IsCreated)
            {
                _blueNoiseJob.Dispose();
            }
            _blueNoiseJob = new BlueNoiseJob();
            
            _blueNoiseJob.Init((uint)(UnityEngine.Random.value * uint.MaxValue), assetDimensions.x, assetDimensions.y, minDistance, k, useGrowingK);
            
            _blueNoiseJobHandle = _blueNoiseJob.Schedule();
            StartCoroutine(WaitForJobCompletion());

        }

        private IEnumerator WaitForJobCompletion()
        {
            yield return new WaitUntil(() => _blueNoiseJobHandle.IsCompleted);
            OnJobCompletion();
        }

        private void OnJobCompletion()
        {
            _blueNoiseJobHandle.Complete();
            var samplePoints = _blueNoiseJob.Result.ToArray(Allocator.Temp);
            
            
            var asset = ScriptableObject.CreateInstance<BlueNoiseSamplePointAsset>();
            asset.samplePoints = samplePoints.ToArray();
            var noiseSettings = _blueNoiseJob.Settings;
            asset.radius = noiseSettings.Radius;
            asset.dimensions = new float2(noiseSettings.Width, noiseSettings.Height);
            _blueNoiseJob.Dispose();
            CreateAsset(asset, pathToSamplePointAsset, samplePointAssetName);
            Debug.Log("Asset created");
        }

        private static void CreateAsset(BlueNoiseSamplePointAsset samplePointAsset, string path, string fileName)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            AssetDatabase.CreateAsset(samplePointAsset, $"{path}/{fileName}.asset");
            AssetDatabase.SaveAssets();
        }

    }
}