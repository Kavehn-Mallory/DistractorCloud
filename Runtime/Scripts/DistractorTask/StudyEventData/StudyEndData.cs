using System;
using DistractorClouds.Core;

namespace DistractorClouds.DistractorTask.StudyEventData
{
    public struct StudyEndData : IStudyEventData
    {
        public string TimeStamp { get; set; }
    }
}