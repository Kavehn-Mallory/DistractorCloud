using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DistractorClouds.DistractorTask;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace DistractorClouds.PanelGeneration
{
    [RequireComponent(typeof(SplineContainer))]
    public class SplineGenerationFromWaypoints : MonoBehaviour
    {

        [Range(0, 1)]
        public float tension;

        public float distanceFromPath;

        public TextAsset textAsset;

        private SplineContainer _splineContainer;
        
        public void BuildSpline()
        {
            if (!_splineContainer)
            {
                _splineContainer = GetComponent<SplineContainer>();
            }
            var waypoints = WaypointLoader.LoadWaypoints(textAsset);
            RebuildSpline(ref _splineContainer, waypoints, tension, distanceFromPath);
            Debug.Log("Spline was generated", this);
        }
        
        /*private void Start()
        {
            if (!_splineContainer)
            {
                _splineContainer = GetComponent<SplineContainer>();
            }

            var waypoints = LoadWaypoints(textAsset);
            RebuildSpline(ref _splineContainer, waypoints, tension, distanceFromPath);
            
            
        }*/



        private static void RebuildSpline(ref SplineContainer splineContainer, List<float3> waypoints,float tension = 0.5f, float distanceFromPath = 1f)
        {
            // Before setting spline knots, reduce the number of sample points.
           // SplineUtility.ReducePoints(m_Stroke, m_Reduced, m_PointReductionEpsilon);

            var alternativeWaypoints = new List<float3>();
            var spline = splineContainer.Spline;

            // Assign the reduced sample positions to the Spline knots collection. Here we are constructing new
            // BezierKnots from a single position, disregarding tangent and rotation. The tangent and rotation will be
            // calculated automatically in the next step wherein the tangent mode is set to "Auto Smooth."
            spline.Knots = waypoints.Select(x => new BezierKnot(x));

            var all = new SplineRange(0, spline.Count);

            // Sets the tangent mode for all knots in the spline to "Auto Smooth."
            spline.SetTangentMode(all, TangentMode.AutoSmooth);

            // Sets the tension parameter for all knots. Note that the "Tension" parameter is only applicable to
            // "Auto Smooth" mode knots.
            spline.SetAutoSmoothTension(all, tension);

            if (distanceFromPath == 0)
            {
                return;
            }


        }
    }
}