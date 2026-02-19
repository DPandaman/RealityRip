// spline-based trajectory planning for drone paths

using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

public class PathArchitect : MonoBehaviour
{
    // spline container for bezier interpolation
    public SplineContainer splineContainer;

    // optional: include drone pos as first knot
    public Transform droneTransform;

    // cached ref for visualization
    private SplineInstantiate splineInstantiate;

    void Awake(){
        if (splineContainer != null)
            splineInstantiate = splineContainer.GetComponent<SplineInstantiate>();
    }

    public void BuildRescuePath(List<Transform> waypoints, bool optimizeOrder = false)
    {
        if (splineContainer == null){
            Debug.LogError("PathArchitect: spline container missing");
            return;
        }

        if (waypoints == null || waypoints.Count == 0){
            Debug.LogWarning("PathArchitect: no waypoints provided");
            return;
        }

        // optionally reorder waypoints using nearest-neighbor heuristic
        List<Transform> ordered = optimizeOrder ? NearestNeighborOrder(waypoints) : waypoints;

        Spline spline = splineContainer.Spline;
        spline.Clear();

        // start from drone position if assigned
        if (droneTransform != null){
            Vector3 toFirst = (ordered[0].position - droneTransform.position).normalized;
            float dist = Vector3.Distance(droneTransform.position, ordered[0].position) * 0.33f;
            BezierKnot startKnot = new BezierKnot(droneTransform.position);
            startKnot.TangentIn = -toFirst * dist;
            startKnot.TangentOut = toFirst * dist;
            spline.Add(startKnot);
        }

        for (int i = 0; i < ordered.Count; i++)
        {
            BezierKnot knot = new BezierKnot(ordered[i].position);

            // compute tangents from neighbors for smooth C1 curves
            Vector3 tangent;
            float scale;

            if (ordered.Count == 1){
                tangent = Vector3.forward;
                scale = 1f;
            }
            else if (i == 0){
                // first: point toward next waypoint
                Vector3 diff = ordered[1].position - ordered[0].position;
                tangent = diff.normalized;
                scale = diff.magnitude * 0.33f;
            }
            else if (i == ordered.Count - 1){
                // last: follow from previous waypoint
                Vector3 diff = ordered[i].position - ordered[i - 1].position;
                tangent = diff.normalized;
                scale = diff.magnitude * 0.33f;
            }
            else{
                // middle: average direction for continuity
                Vector3 diff = ordered[i + 1].position - ordered[i - 1].position;
                tangent = diff.normalized;
                scale = diff.magnitude * 0.25f;
            }

            knot.TangentIn = -tangent * scale;
            knot.TangentOut = tangent * scale;
            spline.Add(knot);
        }

        // refresh visual representation
        if (splineInstantiate != null)
            splineInstantiate.UpdateInstances();

        Debug.Log($"PathArchitect: trajectory computed with {spline.Count} knots");
    }

    List<Transform> NearestNeighborOrder(List<Transform> waypoints)
    {
        List<Transform> remaining = new List<Transform>(waypoints);
        List<Transform> ordered = new List<Transform>();

        // start from drone position if available, otherwise first waypoint
        Vector3 current = droneTransform != null ? droneTransform.position : remaining[0].position;

        while (remaining.Count > 0)
        {
            float bestDist = float.MaxValue;
            int bestIdx = 0;

            for (int i = 0; i < remaining.Count; i++)
            {
                float dist = Vector3.Distance(current, remaining[i].position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIdx = i;
                }
            }

            ordered.Add(remaining[bestIdx]);
            current = remaining[bestIdx].position;
            remaining.RemoveAt(bestIdx);
        }

        return ordered;
    }

    // wipe existing path
    public void ClearPath(){
        if (splineContainer == null) return;
        splineContainer.Spline.Clear();

        if (splineInstantiate != null)
            splineInstantiate.UpdateInstances();
    }

    // sample position along path (t = 0 to 1)
    public Vector3 GetPositionOnPath(float t){
        if (splineContainer == null || splineContainer.Spline.Count == 0)
            return Vector3.zero;

        return (Vector3)SplineUtility.EvaluatePosition(splineContainer.Spline, Mathf.Clamp01(t));
    }
}
