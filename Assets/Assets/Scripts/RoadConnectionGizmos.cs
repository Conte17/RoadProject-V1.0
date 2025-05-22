using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class RoadConnectionGizmo : MonoBehaviour
{
    public float sphereSize = 0.1f;

    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        DrawConnectionPoint("ConnectionPoint_1", Color.blue, "");
        DrawConnectionPoint("ConnectionPoint_2", Color.red, "");
        DrawConnectionPoint("ConnectionPoint_3", Color.green, "");
        DrawConnectionPoint("ConnectionPoint_4", Color.yellow, "");
#endif
    }

#if UNITY_EDITOR
    void DrawConnectionPoint(string pointName, Color color, string label)
    {
        Transform point = transform.Find(pointName);
        if (point != null)
        {
            Gizmos.color = color;
            Gizmos.DrawSphere(point.position, sphereSize);
            Handles.Label(point.position + Vector3.up * 0.1f, label);
        }
    }
#endif
}
