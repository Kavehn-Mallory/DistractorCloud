using System.Collections.Generic;
using System.Globalization;
using DistractorClouds.PanelGeneration;
using Unity.Mathematics;
using UnityEngine;

namespace DistractorClouds.DistractorTask
{
    public static class WaypointLoader
    {
        
        private const string XValue = "X";
        private const string YValue = "Y";
        private const string ZValue = "Z";
        
        public static List<float3> LoadWaypoints(TextAsset asset)
        {
            var fileData = asset.text;
            var csvDictionary = CSVReader.Read(fileData);

            var waypoints = new List<float3>();
            foreach (var line in csvDictionary)
            {
                var x = float.Parse(line[XValue], CultureInfo.InvariantCulture);
                var y = float.Parse(line[YValue], CultureInfo.InvariantCulture);
                var z = float.Parse(line[ZValue], CultureInfo.InvariantCulture);
                waypoints.Add(new float3(x, y, z));
            }

            return waypoints;
        }
    }
}