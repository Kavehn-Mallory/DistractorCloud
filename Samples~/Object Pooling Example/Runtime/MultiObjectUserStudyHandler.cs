using DistractorClouds.DistractorTask;
using DistractorClouds.PanelGeneration;
using UnityEngine;

namespace Samples.Distractor_Clouds.Object_Pooling_Sample.Runtime
{
    public class MultiObjectUserStudyHandler : BaseUserStudyHandler<MultiObjectAssetGenerator, BaseMultiObjectComponent>
    {
        
        [ContextMenu("Start Easy Low")]
        public void StartEasyTrialLow()
        {
            StartTrial(PathComplexity.Easy, TaskLoad.Low);
        }
        
        [ContextMenu("Start Easy High")]
        public void StartEasyTrialHigh()
        {
            StartTrial(PathComplexity.Easy, TaskLoad.High);
        }
        
        [ContextMenu("Start Difficult Low")]
        public void StartDifficultTrialLow()
        {
            StartTrial(PathComplexity.Difficult, TaskLoad.Low);
        }
        
        [ContextMenu("Start Difficult High")]
        public void StartDifficultTrialHigh()
        {
            StartTrial(PathComplexity.Difficult, TaskLoad.High);
        }
        
        public override void DeselectTarget(GroupObject<BaseMultiObjectComponent> assetGeneratorActiveObject)
        {
            var colorSwitchComponents = assetGeneratorActiveObject.ActiveObject.GetComponentsInChildren<ColorSwitchComponent>();

            foreach (var colorSwitchComponent in colorSwitchComponents)
            {
                colorSwitchComponent.DeselectObject();
            }
        }

        public override void SelectTarget(GroupObject<BaseMultiObjectComponent> assetGeneratorActiveObject)
        {
            var colorSwitchComponents = assetGeneratorActiveObject.ActiveObject.GetComponentsInChildren<ColorSwitchComponent>();

            foreach (var colorSwitchComponent in colorSwitchComponents)
            {
                colorSwitchComponent.SelectObject();
            }
        }
    }
}