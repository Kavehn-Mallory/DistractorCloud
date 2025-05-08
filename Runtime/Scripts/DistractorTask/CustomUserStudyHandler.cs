using DistractorClouds.Attributes;
using DistractorClouds.DistractorTask.AssetGenerators;
using DistractorClouds.DistractorTask.Distractors;
using DistractorClouds.PanelGeneration;
using UnityEngine;

namespace DistractorClouds.DistractorTask
{
    public class CustomUserStudyHandler : BaseUserStudyHandler<ColorSwitchAssetGenerator, ColorSwitchComponent>
    {
        
        [SerializeField]
        private ColorSwitchComponent prefab;
        
        [Layer]
        public int distractorLayer = 3;
        
        [SerializeField]
        private Material[] debugMaterials;

        public override void StartTrial(PathComplexity pathComplexity, TaskLoad taskLoad)
        {
            base.StartTrial(pathComplexity, taskLoad);
            IterateGroupsAndMarkAllObjects();
        }

        public void IterateGroupsAndMarkAllObjects()
        {

            Debug.Log("Running this");
            for (int i = 0; i < assetGenerator.ActiveObjects.Count; i++)
            {
                assetGenerator.ActiveObjects[i].ActiveObject.GetComponent<MeshRenderer>().material =
                    debugMaterials[assetGenerator.ActiveObjects[i].Group % debugMaterials.Length];
                
            }
            
            
        }
        
        
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
        
        protected override void Start()
        {
            base.Start();
            assetGenerator = new ColorSwitchAssetGenerator(prefab, this.transform, distractorLayer);
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