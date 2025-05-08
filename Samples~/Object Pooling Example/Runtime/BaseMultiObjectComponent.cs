using System;
using UnityEngine;

namespace Samples.Distractor_Clouds.Object_Pooling_Sample.Runtime
{
    public abstract class BaseMultiObjectComponent : MonoBehaviour
    {
        
        [SerializeField]
        protected InstantiatableItem[] items;
        
        protected float MaxProbability;
        
        public abstract void PickObject(float probability);

        public abstract void EnsureItemsValidity();

        public static int GetIndexForProbability(InstantiatableItem[] items, float probability)
        {
            var currentProbability = 0f;

            for (int i = 0; i < items.Length; i++)
            {
                if (!items[i].prefab)
                {
                    continue;
                }

                currentProbability += items[i].probability;
                if (probability < currentProbability)
                {
                    return i;
                }
            }

            return items.Length - 1;
        }
    }
    
    [Serializable]
    public struct InstantiatableItem
    {
        public GameObject prefab;
        public float probability; 
    }
}