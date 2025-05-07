using Unity.Mathematics;
using UnityEngine;

namespace DistractorClouds.PanelGeneration
{
    public class SamplePointAsset : ScriptableObject
    {
        public float3[] samplePoints;
        public float scale = 1;
        public int2 boundingBoxSize = new int2(64, 64);
        
        public SamplePointAsset ScaleSamplePoints(float newScale)
        {
            if (scale == 0)
            {
                scale = 1;
            }
            var k = newScale / scale;
            var scaledPointAsset = Instantiate(this);
            
            scaledPointAsset.boundingBoxSize = new int2((int)math.ceil(boundingBoxSize.x * k), (int)math.ceil(boundingBoxSize.y * k));
            scaledPointAsset.scale = newScale;
            
            for (var i = 0; i < scaledPointAsset.samplePoints.Length; i++)
            {
                var samplePointPosition = scaledPointAsset.samplePoints[i];
                samplePointPosition = new float3(samplePointPosition.x * k, samplePointPosition.y * k, samplePointPosition.z);
                scaledPointAsset.samplePoints[i] = samplePointPosition;
            }

            
            
            
            return scaledPointAsset;
        }
        
        public SamplePointAsset AlternativeScaleSamplePoints(int2 newScale)
        {
            if (math.all(newScale == int2.zero))
            {
                newScale = 1;
            }
            var k = newScale / (float2)boundingBoxSize;
            var scaledPointAsset = Instantiate(this);
            
            scaledPointAsset.boundingBoxSize = newScale;
            
            for (var i = 0; i < scaledPointAsset.samplePoints.Length; i++)
            {
                var samplePointPosition = scaledPointAsset.samplePoints[i];
                samplePointPosition = new float3(samplePointPosition.x * k.x, samplePointPosition.y * k.y, samplePointPosition.z);
                scaledPointAsset.samplePoints[i] = samplePointPosition;
            }

            
            
            
            return scaledPointAsset;
        }
        
    }
    
}