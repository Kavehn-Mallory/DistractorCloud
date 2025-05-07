using System;
using System.Collections;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using Random = UnityEngine.Random;

namespace DistractorClouds.Noise
{
    public class BlueNoiseAssetGenerator : MonoBehaviour
    {

        [Header("Blue Noise Settings")]
        
        [SerializeField]
        private float2 assetDimensions = new float2(1f, 1f);

        [SerializeField] private float minDistance = 0.01f;

        [SerializeField] private int k = 30;

        [SerializeField] private bool useGrowingK;

        [SerializeField] private int numberOfAssetsToGenerate = 5;
        
        
        [Space(10f), Header("Sample Point Asset Settings")]
        
        [SerializeField]
        private string pathToSamplePointAsset = "Assets/ScriptableObjects";

        [SerializeField]
        private string samplePointAssetName = "SamplePointAsset";
        
        private JobHandle[] _jobHandles = Array.Empty<JobHandle>();

        private BlueNoiseJob[] _blueNoiseJobs = Array.Empty<BlueNoiseJob>();

#if UNITY_EDITOR
        

        
        [ContextMenu("Generate Blue Noise Asset")]
        private void GenerateBlueNoise()
        {
            if (_jobHandles.Length != 0)
            {
                Debug.LogError("Job is still running");
                return;
            }

            foreach (var job in _blueNoiseJobs)
            {
                if (job.IsCreated)
                {
                    job.Dispose();
                }
            }

            _jobHandles = new JobHandle[numberOfAssetsToGenerate];
            _blueNoiseJobs = new BlueNoiseJob[numberOfAssetsToGenerate];
            Random.InitState((int)DateTime.Now.Ticks);
            for (int i = 0; i < numberOfAssetsToGenerate; i++)
            {
                var blueNoiseJob = new BlueNoiseJob();
            
                
                blueNoiseJob.Init((uint)(Random.value * uint.MaxValue), assetDimensions.x, assetDimensions.y, minDistance, k, useGrowingK);
            
                _blueNoiseJobs[i] = blueNoiseJob;
                _jobHandles[i] = blueNoiseJob.Schedule();
                
            }
            StartCoroutine(WaitForJobSeriesCompletion());
        }

        private IEnumerator WaitForJobSeriesCompletion()
        {
            yield return new WaitWhile(() => _jobHandles.Any(handle => !handle.IsCompleted));
            OnJobSeriesCompletion();
        }

        private void OnJobSeriesCompletion()
        {
            for (var i = 0; i < _jobHandles.Length; i++)
            {
                var handle = _jobHandles[i];
                handle.Complete();
                var job = _blueNoiseJobs[i];
                var samplePoints = job.Result.ToArray(Allocator.Temp);
            
            
                var asset = ScriptableObject.CreateInstance<BlueNoiseSamplePointAsset>();
                asset.samplePoints = samplePoints.ToArray();
                var noiseSettings = job.Settings;
                asset.radius = noiseSettings.Radius;
                asset.dimensions = new float2(noiseSettings.Width, noiseSettings.Height);
                job.Dispose();
                CreateAsset(asset, pathToSamplePointAsset, samplePointAssetName + $"{i}");
                
            }
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
        
#endif

    }
    
    

}