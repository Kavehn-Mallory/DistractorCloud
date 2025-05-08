using DistractorClouds.DistractorTask;
using DistractorClouds.PanelGeneration;
using UnityEngine;

namespace Samples.Distractor_Clouds.Object_Pooling_Sample.Runtime
{
    public class CustomUserStudyHandler : BaseUserStudyHandler<ColorSwitchAssetGenerator, ColorSwitchComponent>
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

        public override void DeselectTarget(GroupObject<ColorSwitchComponent> assetGeneratorActiveObject)
        {
            assetGeneratorActiveObject.ActiveObject.DeselectObject();
        }

        public override void SelectTarget(GroupObject<ColorSwitchComponent> assetGeneratorActiveObject)
        {
            assetGeneratorActiveObject.ActiveObject.SelectObject();
        }
        
    }
}