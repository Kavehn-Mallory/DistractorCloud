using UnityEngine;

namespace Samples.Distractor_Clouds.Object_Pooling_Sample.Runtime
{
    public class MultiPrefabComponent : BaseMultiObjectComponent
    {

        private GameObject _spawnedPrefab;

        public override void PickObject(float probability)
        {
            EnsureItemsValidity();
            if (_spawnedPrefab)
            {
                Destroy(_spawnedPrefab);
            }
            var index = GetIndexForProbability(items, probability * MaxProbability);
            var item = items[index].prefab;

            _spawnedPrefab = Instantiate(item, transform);
            
        }

        public override void EnsureItemsValidity()
        {
            float probability = 0;
            for (int i = 0; i < items.Length; i++)
            {
                var item = items[i];

                if (item.prefab != null)
                {
                    if (transform.IsChildOf(item.prefab.transform))
                    {
                        Debug.LogWarning("Instantiating a parent of the SplineInstantiate object itself is not permitted" +
                                         $" ({item.prefab.name} is a parent of {transform.gameObject.name}).", this);
                        item.prefab = null;
                        items[i] = item;
                    }
                    else
                        probability += item.probability;
                }
            }

            MaxProbability = probability;
        }
    }


}