using System;
using DistractorClouds.Core;

namespace DistractorClouds.DistractorTask.StudyEventData
{
    public struct StudyPathEndPointReachedData : IStudyEventData
    {
        public string TimeStamp { get; set; }
        /// <summary>
        /// Value ranges between 0 and 1. 0 is the start point. 1 is the end point.
        /// Helps to identify the correct end point
        /// </summary>
        public float SplinePosition { get; set; }
    }
}