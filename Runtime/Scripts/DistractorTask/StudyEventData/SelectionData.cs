using System;
using DistractorClouds.Core;
using UnityEngine;

namespace DistractorClouds.DistractorTask.StudyEventData
{
    [Serializable]
    public struct SelectionData : IStudyEventData
    {
        public string TimeStamp { get; set; }
        public bool IsValidSelection { get; set; }
        public Vector3 TargetPosition { get; set; }
    }
}