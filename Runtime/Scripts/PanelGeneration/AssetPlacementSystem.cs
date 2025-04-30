using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;
using Task = System.Threading.Tasks.Task;

namespace DistractorClouds.PanelGeneration
{
    public class AssetPlacementSystem : MonoBehaviour
    {
        
        
        [SerializeField]
        private string pathToSamplePointAsset = "Assets/ScriptableObjects";

        [SerializeField]
        private string samplePointAssetName = "SamplePointAsset";

        [SerializeField]
        private bool generateSamplePointAsset;
        
        [SerializeField]
        private SamplePointAssetCreationSettings samplePointAssetCreationSettings;

        private NativeList<float2> _poissonSamplePoints;





#if UNITY_EDITOR

        private void Start()
        {
            Debug.Log("Start starts");
            CreateAsset();
            Debug.Log("Start ends");
        }

        private async void CreateAsset()
        {
            if (generateSamplePointAsset)
            {
                generateSamplePointAsset = false;
                await CreateSamplePointsAsync((uint)Random.Range(1, 100000));
                var samplePoints = new List<float3>();
                BlueNoiseGeneration(ref samplePoints);
                var asset = ScriptableObject.CreateInstance<SamplePointAsset>();
                asset.samplePoints = samplePoints.ToArray();
                _poissonSamplePoints.Dispose();
                CreateAsset(asset, pathToSamplePointAsset, samplePointAssetName, samplePointAssetCreationSettings);
                Debug.Log("Asset created");
            }
        }

        /*private void OnValidate()
        {
            if (generateSamplePointAsset)
            {
                generateSamplePointAsset = false;
                CreateSamplePointAsset();
            }
        }*/

        private void CreateSamplePointAsset()
        {
            var instance = ScriptableObject.CreateInstance<SamplePointAsset>();
            
            //Fill asset with values
            CreateSamplePoints(ref instance);


            if (!Directory.Exists(pathToSamplePointAsset))
            {
                Directory.CreateDirectory(pathToSamplePointAsset);
            }
            
            AssetDatabase.CreateAsset(instance, $"{pathToSamplePointAsset}/{samplePointAssetName}.asset");
            instance.scale = samplePointAssetCreationSettings.radius;
            instance.boundingBoxSize = new int2(samplePointAssetCreationSettings.noiseMapWidth,
                samplePointAssetCreationSettings.noiseMapHeight);
            AssetDatabase.SaveAssets();
        }

        private static void CreateAsset(SamplePointAsset samplePointAsset, string path, string fileName, SamplePointAssetCreationSettings settings)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            
            AssetDatabase.CreateAsset(samplePointAsset, $"{path}/{fileName}.asset");
            samplePointAsset.scale = settings.radius;
            samplePointAsset.boundingBoxSize = new int2(settings.noiseMapWidth,
                settings.noiseMapHeight);
            AssetDatabase.SaveAssets();
        }
        
        #endif
        
        
        private Task CreateSamplePointsAsync(uint seed)
        {
            //_poissonSamplePoints.Clear();
            return Task.Run(() =>
            {
                
                PoissonDiskCreationTask(seed);
            });
            
        }
        

        private void CreateSamplePoints(ref SamplePointAsset asset)
        {
            var samplePoints = new List<float3>();
            var poissonHandle = PoissonDiskCreation(ref samplePoints);
            poissonHandle.Complete();
            BlueNoiseGeneration(ref samplePoints);
            asset.samplePoints = samplePoints.ToArray();
            _poissonSamplePoints.Dispose();
        }

        private JobHandle PoissonDiskCreation(ref List<float3> samplePoints)
        {
            _poissonSamplePoints = new NativeList<float2>(Allocator.Persistent);
            return SetupPoissonDiscSampleJob(_poissonSamplePoints);
        }
        
        private void PoissonDiskCreationTask(uint seed)
        {
            _poissonSamplePoints = new NativeList<float2>(Allocator.Persistent);
            var cellSize = samplePointAssetCreationSettings.radius / math.sqrt(2);
            var gridWidth = Mathf.CeilToInt(samplePointAssetCreationSettings.noiseMapWidth / cellSize);
            var gridHeight = Mathf.CeilToInt(samplePointAssetCreationSettings.noiseMapHeight / cellSize);
            var grid = new NativeArray<int>(gridWidth * gridHeight, Allocator.Persistent);
            var activeList = new NativeList<float2>(Allocator.Persistent);


            var poissonDiscJob = new PoissonDiscSamplingJob
            {
                Grid = grid,
                Points = _poissonSamplePoints,
                ActiveList = activeList,
                SampleRegionSize = new float2(samplePointAssetCreationSettings.noiseMapWidth, samplePointAssetCreationSettings.noiseMapHeight),
                Random = new Unity.Mathematics.Random(seed),
                NumSamplesBeforeRejection = samplePointAssetCreationSettings.numSamplesBeforeRejection,
                Radius = samplePointAssetCreationSettings.radius,
                CellSize = cellSize,
                GridWidth = gridWidth
            };

            poissonDiscJob.Execute();

            activeList.Dispose();
            grid.Dispose();
            
        }
        
        
        
        
        
        private JobHandle SetupPoissonDiscSampleJob(NativeList<float2> poissonSamplePoints)
        {
            
            var cellSize = samplePointAssetCreationSettings.radius / math.sqrt(2);
            var gridWidth = Mathf.CeilToInt(samplePointAssetCreationSettings.noiseMapWidth / cellSize);
            var gridHeight = Mathf.CeilToInt(samplePointAssetCreationSettings.noiseMapHeight / cellSize);
            var grid = new NativeArray<int>(gridWidth * gridHeight, Allocator.Persistent);
            var activeList = new NativeList<float2>(Allocator.Persistent);


            var poissonDiscJob = new PoissonDiscSamplingJob
            {
                Grid = grid,
                Points = poissonSamplePoints,
                ActiveList = activeList,
                SampleRegionSize = new float2(samplePointAssetCreationSettings.noiseMapWidth, samplePointAssetCreationSettings.noiseMapHeight),
                Random = new Unity.Mathematics.Random((uint)Random.Range(1, 100000)),
                NumSamplesBeforeRejection = samplePointAssetCreationSettings.numSamplesBeforeRejection,
                Radius = samplePointAssetCreationSettings.radius,
                CellSize = cellSize,
                GridWidth = gridWidth
            };
            
            return activeList.Dispose(grid.Dispose(poissonDiscJob.Schedule()));
        }
        
        
        private struct PoissonDiscSamplingJob : IJob
        {
            public NativeArray<int> Grid;
            public int GridWidth;
            public float2 SampleRegionSize;
            public NativeList<float2> Points;
            public NativeList<float2> ActiveList;
            public Unity.Mathematics.Random Random;
            public int NumSamplesBeforeRejection;
            public float Radius;
            public float CellSize;
            
            public void Execute()
            {

                var gridHeight = Grid.Length / GridWidth;
                var squaredRadius = Radius * Radius;
                ActiveList.Add(new float2(SampleRegionSize.x / 2f, SampleRegionSize.y /2f));
                while (!ActiveList.IsEmpty)
                {
                    var spawnIndex = Random.NextInt(0, ActiveList.Length);
                    var spawnCenter = ActiveList[spawnIndex];
                    var candidateAccepted = false;
                    for (var i = 0; i < NumSamplesBeforeRejection; i++)
                    {
                        var angle = Random.NextInt() * math.PI * 2;
                        var direction = new float2(math.sin(angle), math.cos(angle));
                        var candidate = spawnCenter + direction * Random.NextFloat(Radius, 2 * Radius);
                        if (IsValid(candidate, Points, CellSize, squaredRadius, Grid, SampleRegionSize, GridWidth, gridHeight))
                        {
                            Points.Add(candidate);
                            ActiveList.Add(candidate);
                            var index = FlattenGridPosition((int)(candidate.x / CellSize),
                                (int)(candidate.y / CellSize),
                                GridWidth);
                            Grid[index] = Points.Length;
                            candidateAccepted = true;
                            break;
                        }
                    }

                    if (!candidateAccepted)
                    {
                        ActiveList.RemoveAt(spawnIndex);
                    }
                    
                }
            }
            

            private static bool IsValid(float2 candidate, NativeList<float2> points, float cellSize, float squaredRadius,
                NativeArray<int> grid, float2 sampleRegionSize, int width, int height)
            {
                if (candidate.x >= 0 && candidate.x < sampleRegionSize.x && candidate.y >= 0 && candidate.y < sampleRegionSize.y)
                {
                    var cellX = (int)(candidate.x / cellSize);
                    var cellY = (int)(candidate.y / cellSize);
                    var searchStartX = math.max(0, cellX - 2);
                    var searchEndX = math.min(cellX + 2, width - 1);
                    var searchStartY = math.max(0, cellY - 2);
                    var searchEndY = math.min(cellY + 2, height - 1);

                    for (var x = searchStartX; x <= searchEndX; x++)
                    {
                        for (var y = searchStartY; y <= searchEndY; y++)
                        {
                            //if value wasn't set, we receive -1. Otherwise the number of points at that time (-1 returns index in list)
                            var pointIndex = grid[FlattenGridPosition(x, y, width)] - 1;
                            if (pointIndex == -1)
                            {
                                continue;
                            }
                            var sqDistance = math.distancesq(candidate, points[pointIndex]);
                            if (sqDistance < squaredRadius)
                            {
                                return false;
                            }
                        }
                    }
                    return true;
                }

                return false;
            }

            private static int FlattenGridPosition(int x, int y, int width)
            {
                return x + y * width;
            }
        }
        
        private void BlueNoiseGeneration(ref List<float3> samplePoints)
        {
            GenerateBlueNoise(_poissonSamplePoints, ref samplePoints, samplePointAssetCreationSettings.noiseMapHeight,
                samplePointAssetCreationSettings.noiseMapWidth, 30);
        }
        
        private void GenerateBlueNoise(NativeArray<float2> samplePoints, ref List<float3> thresholdPoints, int height, int width, int samplesPerPoint)
        {
            
            var remainingPoints = samplePoints.ToArray().ToList();

            var currentPoint = remainingPoints[0];
            remainingPoints.RemoveAt(0);
            var placeholder = 1;

            //todo fix this quickly that doesnt make any sense whatsoever
            for (var i = 0; i < remainingPoints.Count - 1; i++)
            {
                var currentBest = -1;
                var smallestSquaredDistance = 0f;

                if (remainingPoints.Count > samplesPerPoint)
                {
                    
                    for (var sample = 0; sample < samplesPerPoint; sample++)
                    {
                        var randomElement = Random.Range(0, remainingPoints.Count);
                        var squaredDistance = CalculateShortestDistance(currentPoint, remainingPoints[randomElement],
                            width, height);
                        if (squaredDistance > smallestSquaredDistance)
                        {
                            currentBest = randomElement;
                            smallestSquaredDistance = squaredDistance;
                        }
                    }
                }
                else
                {
                    for (var sampleIndex = 0; sampleIndex < remainingPoints.Count; sampleIndex++)
                    {
                        var sample = remainingPoints[sampleIndex];
                        var squaredDistance = CalculateShortestDistance(currentPoint, sample, width, height);
                        if (squaredDistance > smallestSquaredDistance)
                        {
                            currentBest = sampleIndex;
                            smallestSquaredDistance = squaredDistance;
                        }
                    }
                }
                var thresholdValue = placeholder * 2 / (float)samplePoints.Length;
                placeholder++;
                var bestPoint = remainingPoints[currentBest];
                thresholdPoints.Add(new float3(bestPoint.x, bestPoint.y, thresholdValue));
                //Debug.Log($"Threshold Value: {thresholdValue}");



                currentPoint = bestPoint;
                remainingPoints.RemoveAt(currentBest);

            }
            
        }
        
        private static float CalculateShortestDistance(float2 currentPoint, float2 sample, int width, int height)
        {
            var xDistance = math.abs(currentPoint.x - sample.x);
            var yDistance = math.abs(currentPoint.y - sample.y);

            if (xDistance > width / 2f)
            {
                xDistance = width - xDistance;
            }

            if (yDistance > height / 2f)
            {
                yDistance = height - yDistance;
            }

            return xDistance * xDistance + yDistance * yDistance;
        }
    }

}