using DistractorClouds.PanelGeneration;

namespace Samples.Distractor_Clouds.Object_Pooling_Sample.Runtime
{
    public class ColorSwitchAssetGenerator : GenericAssetGenerator<ColorSwitchComponent>
    {
        
        public override void OnReleaseObject(ColorSwitchComponent obj)
        {
            obj.DeselectObject();
            obj.gameObject.SetActive(false);
        }

        public override void OnGetObject(ColorSwitchComponent obj)
        {
            obj.gameObject.SetActive(true);
        }

        public override void SetProbability(ColorSwitchComponent obj, float probability)
        {
            return;
        }
    }
}