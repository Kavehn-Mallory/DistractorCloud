using System;
using DistractorClouds.Core;
using UnityEngine;

namespace DistractorClouds.DistractorTask.StudyEventData
{
    public struct SplineRepositioningData : IStudyEventData
    {
        public string TimeStamp { get; set; }
        
        public Quaternion OldOrientation { get; set; }
        public Quaternion NewOrientation { get; set; }
        public Vector3 OldPosition { get; set; }
        public Vector3 NewPosition { get; set; }
    }
}