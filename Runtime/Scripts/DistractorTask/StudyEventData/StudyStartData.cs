using System;
using DistractorClouds.Core;

namespace DistractorClouds.DistractorTask.StudyEventData
{
    [Serializable]
    public struct StudyStartData : IStudyEventData
    {
        public float TimeStamp { get; set; }
    }
}