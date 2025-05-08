using System;
using DistractorClouds.Noise;
using DistractorClouds.PanelGeneration;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace DistractorClouds.DistractorTask
{
    public partial class DistractorUserStudySetup : MonoBehaviour
    {

        [SerializeField]
        private SplineContainer[] easyPaths = Array.Empty<SplineContainer>();
        
        [SerializeField]
        private SplineContainer[] difficultPaths = Array.Empty<SplineContainer>();

        [SerializeField]
        private BlueNoiseSamplePointAsset[] blueNoiseSamplePointAssets;

        [SerializeField]
        private float lowDensity = 20f;
        [SerializeField]
        private float highDensity = 40f;

        [SerializeField]
        private float2 spacing = new float2(6, 4);

        [SerializeField]
        private float height = 1f;

        [SerializeField]
        private float minDistance = 0.1f;

        [SerializeField]
        private int seed;


        private ClosestSplinePointGeneration _splinePointGeneration;


        public AssetSpawnPoint[] InitializeTrial(PathComplexity pathComplexity, TaskLoad taskLoad)
        {
            var containers = Array.Empty<SplineContainer>();
            switch (pathComplexity)
            {
                case PathComplexity.Easy:
                    containers = StartSimplePath(taskLoad);
                    break;
                case PathComplexity.Difficult:
                    containers = StartDifficultPath(taskLoad);
                    break;
            }

           return ClosestSplinePointGeneration.CreateAssetSpawnPoints(ref containers,
                ref blueNoiseSamplePointAssets, spacing, height, minDistance, taskLoad == TaskLoad.Low ? lowDensity :
                highDensity, seed);
           
        }

        private SplineContainer[] StartDifficultPath(TaskLoad taskLoad)
        {
            foreach (var path in easyPaths)
            {
                path.gameObject.SetActive(false);
            }
            foreach (var path in difficultPaths)
            {
                path.gameObject.SetActive(true);
            }

            return difficultPaths;
            
        }

        private SplineContainer[] StartSimplePath(TaskLoad taskLoad)
        {
            foreach (var path in difficultPaths)
            {
                path.gameObject.SetActive(false);
            }
            foreach (var path in easyPaths)
            {
                path.gameObject.SetActive(true);
            }

            return easyPaths;
        }
    }

    [Serializable]
    public struct PathComponent
    {
        public SplineContainer path;
        public float densityModifier;
    }

    public enum PathComplexity
    {
        Easy,
        Difficult
    }

    public enum TaskLoad
    {
        Low,
        High
    }
}