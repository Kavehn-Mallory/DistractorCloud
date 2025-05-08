using UnityEngine;

namespace Samples.Distractor_Clouds.Object_Pooling_Sample.Runtime
{
    public class MultiChildObjectComponent : BaseMultiObjectComponent
    {
        public override void PickObject(float probability)
        {
            EnsureItemsValidity();
            var index = GetIndexForProbability(items, probability * MaxProbability);

            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                if (i == index)
                {
                    item.prefab.gameObject.SetActive(true);
                    continue;
                }
                item.prefab.gameObject.SetActive(false);
            }
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
                    else if (!item.prefab.transform.IsChildOf(transform))
                    {
                        Debug.LogWarning($"Object {item.prefab} needs to be a child of the {nameof(MultiChildObjectComponent)}. Otherwise it cannot be used.", this);
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