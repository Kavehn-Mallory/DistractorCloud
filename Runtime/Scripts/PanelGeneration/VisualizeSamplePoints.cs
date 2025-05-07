using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DistractorClouds.PanelGeneration
{
    public class VisualizeSamplePoints : MonoBehaviour
    {
        [SerializeField]
        private SamplePointAsset samplePointAsset;

        [SerializeField]
        private GameObject spawnObject;

        [SerializeField, Range(20, 30)]
        private float targetDensity;

        [SerializeField]
        private float minDistanceBetweenObjects = 0.5f;


        private JobHandle _blueNoiseJobHandle;
        private BlueNoiseJob _blueNoiseJob;
        
        private void Start()
        {
            

            if (_blueNoiseJob.IsCreated)
            {
                _blueNoiseJob.Dispose();
            }
            _blueNoiseJob = new BlueNoiseJob();
            
            _blueNoiseJob.Init((uint)(Random.value * uint.MaxValue), 100f, 100f, minDistanceBetweenObjects * 100f);


            /*_blueNoiseJob.DebugInit();
            _blueNoiseJob.Execute();
            _blueNoiseJob.Dispose();
            
            return;*/
            _blueNoiseJobHandle = _blueNoiseJob.Schedule();

            
            //VisualizeSamplePointAsset(samplePointAsset, targetDensity, spawnObject);
            
        }

        private void Update()
        {

            if (_blueNoiseJobHandle.IsCompleted)
            {
                _blueNoiseJobHandle.Complete();
                var samplePoints = _blueNoiseJob.Result.ToArray(Allocator.Temp);
                Debug.Log($"Done.Generated {samplePoints.Length} sample points");
                VisualizeSamplePointsFromGenerator(samplePoints, spawnObject);
                _blueNoiseJob.Dispose();
                this.enabled = false;
            }
            
        }

        private void OnDisable()
        {
            _blueNoiseJob.Dispose();
        }

        private static void VisualizeSamplePointAsset(SamplePointAsset samplePoints, float targetDensity, GameObject spawnObject)
        {
            var scaledAsset = samplePoints.AlternativeScaleSamplePoints(new int2(100, 100));

            var density = scaledAsset.samplePoints.Length;

            var width = density / targetDensity;

            foreach (var samplePoint in scaledAsset.samplePoints)
            {
                Instantiate(spawnObject, new Vector3((samplePoint.x / 100f) * width, samplePoint.y / 100f, 0),
                    Quaternion.identity);
            }
        }
        
        private static void VisualizeSamplePointsFromGenerator(NativeArray<float2> samplePoints, GameObject spawnObject)
        {

            foreach (var samplePoint in samplePoints)
            {
                Instantiate(spawnObject, new Vector3((samplePoint.x / 100f), samplePoint.y / 100f, 0),
                    Quaternion.identity);
            }
        }
        
        
    }
}