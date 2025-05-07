using System;
using DistractorClouds.Noise;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace DistractorClouds.PanelGeneration
{
    public struct BlueNoiseJob : IJob, IDisposable
    {
            
        public NativeList<float2> Result;
        private NativeList<int> _activeList;
        private NativeArray<int> _backgroundGrid;
        private float _cellSize;
        private int2 _gridDimensions;
        private Unity.Mathematics.Random _random;
        
        public bool IsCreated => Result.IsCreated;

        public BlueNoiseSettings Settings { get; private set; }

        public void Init(uint seed, float width, float height, float radius, int k = 30, bool useGrowingK = true)
        {

            Settings = new BlueNoiseSettings
            {
                Seed = seed,
                Width = width,
                Height = height,
                Radius = radius,
                K = k,
                UseGrowingK = useGrowingK
            };


            if (Result.IsCreated)
            {
                Result.Dispose();
            }
            //todo create collections as variables of the job 
            _cellSize = Settings.Radius / math.sqrt(2);
            var cellCountWidth = (int)math.ceil(Settings.Width / _cellSize);
            var cellCountHeight = (int)math.ceil(Settings.Height / _cellSize);

            _gridDimensions = new int2(cellCountWidth, cellCountHeight);

            var dimensions = new float2(Settings.Width, Settings.Height);
            
            _backgroundGrid = new NativeArray<int>(cellCountWidth * cellCountHeight, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < _backgroundGrid.Length; i++)
            {
                _backgroundGrid[i] = -1;
            }

            Result = new NativeList<float2>(_backgroundGrid.Length, Allocator.Persistent);
            _activeList = new NativeList<int>(_backgroundGrid.Length, Allocator.Persistent);

            _random = new Unity.Mathematics.Random(Settings.Seed);

            var firstCell = _random.NextFloat2() * dimensions;

            //result.Add(firstCell);
            var gridPosition = PositionToBackgroundGrid(firstCell, _cellSize, cellCountWidth);
            
            _backgroundGrid[gridPosition] = Result.Length;
            _activeList.Add(Result.Length);
            Result.Add(firstCell);
        }
            
            
            
            
        public void Execute()
        {
            GenerateBlueNoise(ref _backgroundGrid, ref _activeList, ref Result, ref _random, Settings.Width, Settings.Height, Settings.Radius, _cellSize, _gridDimensions, Settings.K, Settings.UseGrowingK);
        }

        private static void GenerateBlueNoise(ref NativeArray<int> backgroundGrid, ref NativeList<int> activeList,
            ref NativeList<float2> result, ref Unity.Mathematics.Random random, float width, float height, float radius,
            float cellSize, int2 gridDimensions, int k = 30, bool useGrowingK = true)
        {
            while (activeList.Length > 0)
            {
                    
                var index = random.NextInt(0, activeList.Length);
                var element = result[activeList[index]];
                var foundPoint = false;

                var maxIterationCount = useGrowingK ? result.Length * k : k;
                for (int sampleIndex = 0; sampleIndex < maxIterationCount; sampleIndex++)
                {
                    //generate point
                    var samplePoint = element + random.NextRandomPointInside2RadiusCircle(radius);
                    samplePoint.RestrictToGridDimensions(width, height);
                    
                    //check if point is valid 
                    if (IsValid(ref backgroundGrid, ref result, samplePoint, cellSize, gridDimensions, radius))
                    {
                        foundPoint = true;
                        backgroundGrid[PositionToBackgroundGrid(samplePoint, cellSize, gridDimensions.x)] =
                            result.Length;
                        activeList.Add(result.Length);
                        result.Add(samplePoint);
                        
                        break;
                    }
                }

                //remove point from active list
                if (!foundPoint)
                {
                    activeList.RemoveAtSwapBack(index);
                }

            }
                
        }
            
        private static bool IsValid(ref NativeArray<int> backgroundGrid, ref NativeList<float2> points, float2 samplePoint, float cellSize, int2 gridDimensions, float radius)
        {
            var gridPosition = GridCellPosition(samplePoint, cellSize);
            if(backgroundGrid[gridPosition.x + gridDimensions.x * gridPosition.y] != -1)
            {
                return false;
            }
            var topLeft = math.max(0, gridPosition - 2);
            var bottomRight = math.min(gridDimensions, gridPosition + 2);

            for (int y = topLeft.y; y < bottomRight.y; y++)
            {
                for (int x = topLeft.x; x < bottomRight.x; x++)
                {
                    var id = y * gridDimensions.x + x;

                    var gridValue = backgroundGrid[id];

                    if (gridValue == -1)
                    {
                        continue;
                    }

                    var element = points[gridValue];
                    if (math.distancesq(element, samplePoint) < (radius * radius))
                    {
                        return false;
                    }
                }
            }

            return true;
        }


        private static int2 GridCellPosition(float2 position, float cellSize)
        {
            return (int2)math.floor(position / cellSize);
        }
        
        private static int PositionToBackgroundGrid(float2 position, float cellSize, int cellCountWidth)
        {
            var cellPos = (int2)math.floor(position / cellSize);
            return cellPos.y * cellCountWidth + cellPos.x;
        }

        public void Dispose()
        {
            if(Result.IsCreated)
                Result.Dispose();
            if(_activeList.IsCreated)
                _activeList.Dispose();
            if(_backgroundGrid.IsCreated)
                _backgroundGrid.Dispose();
        }


    }

    public struct BlueNoiseSettings
    {
        public uint Seed;
        public float Width;
        public float Height;
        public float Radius;
        public int K;
        public bool UseGrowingK;
    }
}