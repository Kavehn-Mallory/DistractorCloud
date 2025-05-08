using UnityEngine;

namespace Samples.Distractor_Clouds.Object_Pooling_Sample.Runtime
{
    [RequireComponent(typeof(MeshRenderer))]
    public class ColorSwitchComponent : MonoBehaviour
    {
        [SerializeField]
        private Material defaultMaterial;
        [SerializeField]
        private Material selectedMaterial;


        private MeshRenderer _meshRenderer;

        private void Awake()
        {
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        public void SelectObject()
        {
            _meshRenderer.material = selectedMaterial;
        }

        public void DeselectObject()
        {
            _meshRenderer.material = defaultMaterial;
        }
    }
}