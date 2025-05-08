using DistractorClouds.PanelGeneration;

namespace Samples.Distractor_Clouds.Object_Pooling_Sample.Runtime
{
    public class MultiObjectAssetGenerator : GenericAssetGenerator<BaseMultiObjectComponent>
    {
        public override void OnReleaseObject(BaseMultiObjectComponent obj)
        {
            obj.gameObject.SetActive(false);
        }

        public override void OnGetObject(BaseMultiObjectComponent obj)
        {
            obj.gameObject.SetActive(true);
        }

        public override void SetProbability(BaseMultiObjectComponent obj, float probability)
        {
            obj.PickObject(probability);
        }
    }
}