using System;
using DistractorClouds.Core;

namespace DistractorClouds.DistractorTask.StudyEventData
{
    [Serializable]
    public struct SelectionData : IStudyEventData
    {
        public string TimeStamp { get; set; }
        public bool IsValidSelection { get; set; }
    }
}