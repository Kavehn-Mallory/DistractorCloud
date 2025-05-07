using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace DistractorClouds.Noise
{
    [BurstCompile]
    public struct BlueNoiseJob : IJob, IDisposable
    {
            
        public NativeList<float3> Result;
        private NativeList<float2> _samplePoints;
        private NativeList<int> _activeList;
        private NativeArray<int> _backgroundGrid;
        private float _cellSize;
        private int2 _gridDimensions;
        private Unity.Mathematics.Random _random;
        
        public bool IsCreated => Result.IsCreated;

        public BlueNoiseSettings Settings { get; private set; }

        public void Init(uint seed, float width, float height, float radius, int k = 30, bool useGrowingK = true, WrapMode wrapMode = WrapMode.WrapAround)
        {

            Settings = new BlueNoiseSettings
            {
                Seed = seed,
                Width = width,
                Height = height,
                Radius = radius,
                K = k,
                UseGrowingK = useGrowingK,
                WrapMode = wrapMode
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

            Result = new NativeList<float3>(_backgroundGrid.Length, Allocator.Persistent);
            _samplePoints = new NativeList<float2>(_backgroundGrid.Length, Allocator.Persistent);
            _activeList = new NativeList<int>(_backgroundGrid.Length, Allocator.Persistent);

            _random = new Unity.Mathematics.Random(Settings.Seed);

            var firstCell = _random.NextFloat2() * dimensions;

            //result.Add(firstCell);
            var gridPosition = PositionToBackgroundGrid(firstCell, _cellSize, cellCountWidth);
            
            _backgroundGrid[gridPosition] = _samplePoints.Length;
            _activeList.Add(_samplePoints.Length);
            _samplePoints.Add(firstCell);
        }
            
            
            
        [BurstCompile]
        public void Execute()
        {
            GenerateSamplePoints(ref _backgroundGrid, ref _activeList, ref _samplePoints, ref _random, Settings.Width, Settings.Height, Settings.Radius, _cellSize, _gridDimensions, Settings.WrapMode, Settings.K, Settings.UseGrowingK);
            GenerateBlueNoise(ref _samplePoints, ref Result, Settings.Width, Settings.Height, ref _random, Settings.K);
        }

        private static void GenerateBlueNoise(ref NativeList<float2> samplePoints, ref NativeList<float3> result, float settingsWidth, float settingsHeight, ref Random random, int k)
        {
            //pick start element -> choose k elements and pick the one with the largest distance -> assign next value while list is not empty 

            var currentIndex = random.NextInt(0, samplePoints.Length);
            var currentElement = samplePoints[currentIndex];

            var elementCount = (float)samplePoints.Length;
            
            result.Add(new float3(currentElement, samplePoints.Length / elementCount));
            samplePoints.RemoveAtSwapBack(currentIndex);


            while (samplePoints.Length > 0)
            {
                float currentDistanceSq = float.MaxValue;
                int currentBestIndex = -1;
                for (int i = 0; i < math.min(samplePoints.Length, k); i++)
                {
                    currentIndex = random.NextInt(0, samplePoints.Length);

                    var nextElement = samplePoints[currentIndex];

                    var distanceSq = math.distancesq(currentElement, nextElement);
                    if (distanceSq < currentDistanceSq)
                    {
                        currentBestIndex = currentIndex;
                        currentDistanceSq = distanceSq;
                    }
                }

                currentElement = samplePoints[currentBestIndex];
                result.Add(new float3(currentElement, samplePoints.Length / elementCount));
                samplePoints.RemoveAtSwapBack(currentBestIndex);
                
            }
        }

        private static void GenerateSamplePoints(ref NativeArray<int> backgroundGrid, ref NativeList<int> activeList,
            ref NativeList<float2> result, ref Random random, float width, float height, float radius,
            float cellSize, int2 gridDimensions, WrapMode wrapMode, int k = 30, bool useGrowingK = true)
        {

            var dimensions = new float2(width, height);
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
                    samplePoint = RestrictToGridDimensions(samplePoint, width, height, wrapMode);
                    
                    //check if point is valid 
                    if (IsValid(ref backgroundGrid, ref result, samplePoint, cellSize, gridDimensions, dimensions, radius, wrapMode))
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
            
        
        private static bool IsValid(ref NativeArray<int> backgroundGrid, ref NativeList<float2> points, float2 samplePoint, float cellSize, int2 gridDimensions, float2 dimensions, float radius, WrapMode wrapMode)
        {
            var gridPosition = GridCellPosition(samplePoint, cellSize);
            if(backgroundGrid[gridPosition.x + gridDimensions.x * gridPosition.y] != -1)
            {
                return false;
            }
            var topLeft = RestrictToBackgroundGrid(gridPosition - 2, gridDimensions, wrapMode);
            
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    var yId = (topLeft.y + y) % gridDimensions.y;
                    var xId = (topLeft.x + x) % gridDimensions.x;
                    var id = yId * gridDimensions.x + xId;

                    var gridValue = backgroundGrid[id];

                    if (gridValue == -1)
                    {
                        continue;
                    }

                    var element = points[gridValue];
                    if (IsInRange(element, samplePoint, radius, dimensions))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        
        //[BurstCompile(FloatPrecision.High, FloatMode.Strict)]
        private static bool IsInRange(float2 pointA, float2 pointB, float radius, float2 dimensions)
        {
            var distanceX = math.abs(pointA.x - pointB.x);
            var distanceY = math.abs(pointA.y - pointB.y);
            
            distanceX = math.min(distanceX, math.abs(dimensions.x - distanceX));
            distanceY = math.min(distanceY, math.abs(dimensions.y - distanceY));


            /*radius += float.Epsilon;
            double radiusSq = radius * radius;
            return math.distance(distanceX, distanceY) < radius;*/
            return math.lengthsq(new float2(distanceX, distanceY)) <= (radius * radius);
        }

        private static int2 RestrictToBackgroundGrid(int2 gridPosition, int2 maxGridPosition, WrapMode wrapMode)
        {
            return wrapMode switch
            {
                WrapMode.Clamp => math.max(0, math.min(gridPosition, maxGridPosition)),
                WrapMode.WrapAround => (gridPosition + maxGridPosition) % maxGridPosition,
                _ => throw new ArgumentOutOfRangeException(nameof(wrapMode), wrapMode, null)
            };
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
        
        private static float2 RestrictToGridDimensions(float2 value, float width, float height,
            WrapMode mode = WrapMode.WrapAround) => mode switch
        {
            WrapMode.Clamp => math.max(0, math.min(value, new float2(width, height))),
            WrapMode.WrapAround => WrapAround(value, new float2(width, height)),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        private static float2 WrapAround(float2 value, float2 dimensions)
        {
            return (value + dimensions) % dimensions;
        }



        public void Dispose()
        {
            if(Result.IsCreated)
                Result.Dispose();
            if(_activeList.IsCreated)
                _activeList.Dispose();
            if(_backgroundGrid.IsCreated)
                _backgroundGrid.Dispose();
            if (_samplePoints.IsCreated)
            {
                _samplePoints.Dispose();
            }
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
        public WrapMode WrapMode { get; set; }
    }
    
    public enum WrapMode
    {
        Clamp,
        WrapAround
    }
}