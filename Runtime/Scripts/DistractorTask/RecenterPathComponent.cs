using System;
using DistractorClouds.DistractorTask.StudyEventData;
using UnityEngine;

namespace DistractorClouds.DistractorTask
{
    public class RecenterPathComponent : MonoBehaviour
    {

        [SerializeField]
        private Transform rigCamera;




        public SplineRepositioningData RecenterPath()
        {
            var splineRepositioningData = new SplineRepositioningData
            {
                OldPosition = rigCamera.position,
                OldOrientation = rigCamera.rotation
            };
            transform.SetPositionAndRotation(rigCamera.position, rigCamera.rotation);

            splineRepositioningData.NewPosition = rigCamera.position;
            splineRepositioningData.NewOrientation = rigCamera.rotation;
            return splineRepositioningData;
        }
    }
}