using System;
using DistractorClouds.Core;

namespace DistractorClouds.DistractorTask.StudyEventData
{
    [Serializable]
    public struct StudyStartData : IStudyEventData
    {
        public string TimeStamp { get; set; }
    }
}