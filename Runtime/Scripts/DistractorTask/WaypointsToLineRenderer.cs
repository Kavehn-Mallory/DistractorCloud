using System.Linq;
using UnityEngine;

namespace DistractorClouds.DistractorTask
{
    [RequireComponent(typeof(LineRenderer))]
    public class WaypointsToLineRenderer : MonoBehaviour
    {

        [SerializeField] private TextAsset waypoints;

        private void Start()
        {
            CreatePath();
        }

        [ContextMenu("Create path")]
        private void CreatePath()
        {
            var waypointData = WaypointLoader.LoadWaypoints(waypoints).Select(w => new Vector3(w.x, w.y, w.z)).ToArray();
            var lineRenderer = GetComponent<LineRenderer>();
            
            Debug.Log($"Setting positions: {waypointData.Length}");
            lineRenderer.positionCount = waypointData.Length;
            lineRenderer.SetPositions(waypointData);
        }
    }
}