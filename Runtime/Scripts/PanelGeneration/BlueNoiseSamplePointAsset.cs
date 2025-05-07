using Unity.Mathematics;
using UnityEngine;

namespace DistractorClouds.PanelGeneration
{

    public class BlueNoiseSamplePointAsset : ScriptableObject
    {
        public float2[] samplePoints;

        public float2 dimensions;

        public float radius;
    }
}