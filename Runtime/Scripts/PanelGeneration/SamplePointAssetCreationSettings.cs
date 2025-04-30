using System;
using UnityEngine;

namespace DistractorClouds.PanelGeneration
{
    [Serializable]
    public class SamplePointAssetCreationSettings
    {
        [SerializeField]
        public float radius = 10;

        [SerializeField]
        public int noiseMapHeight = 64;

        [SerializeField]
        public int noiseMapWidth = 64;
        
        [SerializeField]
        public int numSamplesBeforeRejection = 10000;
    }
}