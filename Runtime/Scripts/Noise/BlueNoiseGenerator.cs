using System;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace DistractorClouds.Noise
{
    public static class BlueNoiseGenerator
    {
        
        
        
        
        public static NativeArray<float2> Generate2DBlueNoise(uint seed, float width, float height, float radius, int k = 30)
        {
            var cellSize = radius / math.sqrt(2);
            var cellCountWidth = (int)math.ceil(width / cellSize);
            var cellCountHeight = (int)math.ceil(height / cellSize);

            var gridDimensions = new int2(cellCountWidth, cellCountHeight);

            var dimensions = new float2(width, height);
            
            var backgroundGrid = new NativeArray<int>(cellCountWidth * cellCountHeight, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < backgroundGrid.Length; i++)
            {
                backgroundGrid[i] = -1;
            }

            var result = new NativeList<float2>(backgroundGrid.Length, Allocator.Temp);
            var activeList = new NativeList<int>(Allocator.Temp);

            var random = new Random(seed);

            var firstCell = random.NextFloat2() * dimensions;

            //result.Add(firstCell);
            var gridPosition = PositionToBackgroundGrid(firstCell, cellSize, cellCountWidth);
            
            backgroundGrid[gridPosition] = result.Length;
            activeList.Add(result.Length);
            result.Add(firstCell);

            while (activeList.Length > 0)
            {
                var index = random.NextInt(0, activeList.Length);
                var element = result[activeList[index]];
                var foundPoint = false;

                for (int sampleIndex = 0; sampleIndex < k; sampleIndex++)
                {
                    //generate point
                    var samplePoint = element + random.NextRandomPointInside2RadiusCircle(radius);
                    samplePoint.RestrictToGridDimensions(width, height);
                    
                    //check if point is valid 
                    if (IsValid(ref backgroundGrid, ref result, samplePoint, cellSize, gridDimensions, radius))
                    {
                        foundPoint = true;
                        backgroundGrid[PositionToBackgroundGrid(sampleIndex, cellSize, gridDimensions.x)] =
                            result.Length;
                        activeList.Add(result.Length);
                        result.Add(samplePoint);
                        
                        break;
                    }
                }

                if (!foundPoint)
                {
                    activeList.RemoveAt(index);
                }
                
                //remove point
            }
            

            return result.ToArray(Allocator.Temp);
        }

        private static bool IsValid(ref NativeArray<int> backgroundGrid, ref NativeList<float2> points, float2 samplePoint, float cellSize, int2 gridDimensions, float radius)
        {
            var gridPosition = GridCellPosition(samplePoint, cellSize);
            var topLeft = math.max(0, gridPosition - 2);
            var bottomRight = math.min(gridDimensions - 1, gridPosition + 2);

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
                    if (math.distancesq(element, samplePoint) < radius)
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
        
        
        
    }
}