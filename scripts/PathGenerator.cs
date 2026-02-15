// uses Unity Splines package to handle text -> path request 

using UnityEngine;
using UnityEngine.Splines; // dependency: unity splines package
using System.Linq;
using System.Collections.Generic;

public class PathArchitect : MonoBehaviour
{
    // container for the catmull-rom/bezier spline interpolation
    public SplineContainer splineContainer; 
    
    public void BuildRescuePath(List<Transform> waypoints)
    {
        // sanity check for null references
        if (splineContainer == null) {
            Debug.LogError("PathArchitect: spline container missing. aborting trajectory generation.");
            return;
        }

        Spline spline = splineContainer.Spline;
        spline.Clear(); // flushing previous trajectory

        // Debug.Log($"PathArchitect: interpolating path for {waypoints.Count} nodes.");

        // NOTE: could add x0 (current drone pos) as the first knot for continuity
        // spline.Add(new BezierKnot(Vector3.zero)); 

        foreach (Transform t in waypoints)
        {
            // defining the control point (knot) in 3D space
            BezierKnot knot = new BezierKnot(t.position);
            
            // enforcing C1 continuity (tangents)
            // setting tangent vectors manually to ensure smooth curvature through the knot
            // avoids sharp discontinuities in the derivative (velocity)
            knot.TangentIn = new Vector3(0, 0, -1f);
            knot.TangentOut = new Vector3(0, 0, 1f);
            
            spline.Add(knot);
        }

        // update the spline mesh instantiation 
        // required to visualize the vector field (arrows/line renderer)
        if(splineContainer.GetComponent<SplineInstantiate>())
        {
            splineContainer.GetComponent<SplineInstantiate>().UpdateInstances(); 
        }
        
        Debug.Log("PathArchitect: trajectory computed.");
    }
}