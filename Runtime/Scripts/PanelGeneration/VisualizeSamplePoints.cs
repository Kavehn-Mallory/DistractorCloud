using DistractorClouds.Noise;
using UnityEngine;

namespace DistractorClouds.PanelGeneration
{
    public class VisualizeSamplePoints : MonoBehaviour
    {
        [SerializeField]
        private BlueNoiseSamplePointAsset samplePointAsset;

        [SerializeField]
        private GameObject spawnObject;
        

        [ContextMenu("Visualize Asset")]
        private void VisualizeAsset()
        {
            var parent = VisualizeSamplePointAsset(samplePointAsset, spawnObject);
            parent.transform.SetParent(this.transform, true);
        }


        private void Start()
        {
            if (samplePointAsset)
            {
                VisualizeAsset();
            }
        }
        
        private static GameObject VisualizeSamplePointAsset(BlueNoiseSamplePointAsset samplePointAsset, GameObject spawnObject)
        {

            var parentObject = new GameObject("SamplePoints");
            parentObject.transform.SetPositionAndRotation(new Vector3(), Quaternion.identity);
            var collider = parentObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(samplePointAsset.dimensions.x, samplePointAsset.dimensions.y,
                samplePointAsset.radius);
            collider.center = collider.size / 2f;
            foreach (var samplePoint in samplePointAsset.samplePoints)
            {
                Instantiate(spawnObject, new Vector3((samplePoint.x), samplePoint.y, 0),
                    Quaternion.identity, parentObject.transform);
            }

            return parentObject;
        }
        
        
    }
}