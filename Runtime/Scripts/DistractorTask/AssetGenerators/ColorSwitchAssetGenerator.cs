using DistractorClouds.DistractorTask.Distractors;
using DistractorClouds.PanelGeneration;
using UnityEngine;

namespace DistractorClouds.DistractorTask.AssetGenerators
{
    public class ColorSwitchAssetGenerator : GenericAssetGenerator<ColorSwitchComponent>
    {

        private Transform _parentTransform;
        private ColorSwitchComponent _prefab;
        
        public ColorSwitchAssetGenerator(ColorSwitchComponent prefab, Transform parentTransform, int distractorLayer)
        {
            _prefab = prefab;
            _parentTransform = parentTransform;
            DistractorLayer = distractorLayer;
        }

        public override Transform ParentTransform => _parentTransform;
        public override ColorSwitchComponent Prefab => _prefab;

        public override void OnDestroyObject(ColorSwitchComponent obj)
        {
            Object.Destroy(obj.gameObject);
        }

        public override void OnReleaseObject(ColorSwitchComponent obj)
        {
            obj.DeselectObject();
            obj.gameObject.SetActive(false);
        }

        public override void OnGetObject(ColorSwitchComponent obj)
        {
            obj.gameObject.SetActive(true);
        }
        
    }
}