using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace DistractorClouds.Noise
{

    public class BlueNoiseSamplePointAsset : ScriptableObject
    {
        public float3[] samplePoints;

        public float2 dimensions;

        public float radius;

        public BlueNoiseSamplePointAsset ScaleAsset(float newRadius)
        {

            if (radius == 0)
            {
                radius = 1f;
            }

            var copy = Instantiate(this);
            var scalingFactor = newRadius / radius;
            copy.radius = newRadius;

            copy.dimensions = dimensions * scalingFactor;

            for (var i = 0; i < samplePoints.Length; i++)
            {
                var samplePoint = samplePoints[i];
                samplePoint = new float3(samplePoint.x * scalingFactor, samplePoint.y * scalingFactor, samplePoint.z);
                copy.samplePoints[i] = samplePoint;
            }

            return copy;

        }

        public BlueNoiseSamplePointAsset FilterAsset(float4 region)
        {
            var copy = Instantiate(this);

            var filteredPoints = new List<float3>();

            foreach (var samplePoint in samplePoints)
            {
                if (math.all(samplePoint.xy >= region.xy) && math.all(samplePoint.xy <= region.zw))
                {
                    var modifiedPoint = samplePoint.xy - region.xy;
                    filteredPoints.Add(new float3(modifiedPoint, samplePoint.z));
                }
            }

            copy.samplePoints = samplePoints;
            copy.dimensions = new float2(region.z - region.x, region.w - region.y);
            copy.samplePoints = filteredPoints.ToArray();

            return copy;
        }
    }
}