using UnityEngine;
using System.Collections.Generic;

public class RoadNetworkGenerator : MonoBehaviour
{
    public GameObject straightRoadPrefab;
    public GameObject crossIntersectionPrefab;
    public int mainRoadLength = 20;
    public int maxBranchDepth = 1;
    public int crossIntersectionInterval = 5;

    private List<Transform> openConnectionPoints = new List<Transform>();
    private HashSet<Vector3Int> occupiedPositions = new HashSet<Vector3Int>();
    private Dictionary<Vector3Int, string> roadTypeAtPosition = new Dictionary<Vector3Int, string>();
    private int consecutiveStraightRoads = 0;
    private const float tileSize = 10f; // assuming 10 units per road segment

    [ContextMenu("Generate Road Network")]
    public void GenerateRoadNetwork()
    {
        // Cleanup
        foreach (Transform child in transform)
        {
            DestroyImmediate(child.gameObject);
        }

        openConnectionPoints.Clear();
        occupiedPositions.Clear();
        roadTypeAtPosition.Clear();
        consecutiveStraightRoads = 0;

        // Start with one straight road
        GameObject firstRoad = Instantiate(straightRoadPrefab, Vector3.zero, Quaternion.Euler(-90, 0, 0), transform);
        Vector3Int firstPos = ToGridPosition(firstRoad.transform.position);
        occupiedPositions.Add(firstPos);
        roadTypeAtPosition[firstPos] = "Straight";

        Transform endPoint = GetConnectionPoint(firstRoad, "ConnectionPoint_2");
        if (endPoint != null) openConnectionPoints.Add(endPoint);
        consecutiveStraightRoads++;

        // Build the main road
        for (int i = 0; i < mainRoadLength; i++)
        {
            if (openConnectionPoints.Count == 0) break;

            Transform connection = openConnectionPoints[0];
            openConnectionPoints.RemoveAt(0);

            GameObject prefabToUse = ChooseRoadType();

            Vector3 targetPosition = connection.position + connection.forward * tileSize;
            Vector3Int gridPos = ToGridPosition(targetPosition);

            if (occupiedPositions.Contains(gridPos))
            {
                Debug.LogWarning($"Skipped placement at {gridPos} due to overlap");
                continue;
            }

            GameObject newRoad = Instantiate(prefabToUse, Vector3.zero, Quaternion.identity, transform);
            Transform attachPoint = GetClosestConnectionPoint(newRoad, connection);

            if (attachPoint == null)
            {
                DestroyImmediate(newRoad);
                continue;
            }

            AlignToConnection(newRoad.transform, attachPoint, connection);

            occupiedPositions.Add(gridPos);
            roadTypeAtPosition[gridPos] = prefabToUse == straightRoadPrefab ? "Straight" : "Cross";

            if (prefabToUse == straightRoadPrefab)
            {
                consecutiveStraightRoads++;
            }
            else // Cross Intersection
            {
                consecutiveStraightRoads = 0;
                GenerateBranch(newRoad, attachPoint, 0);
            }

            // Add connection points except the one we used
            foreach (Transform cp in newRoad.transform)
            {
                if (cp != attachPoint)
                {
                    Vector3Int cpGrid = ToGridPosition(cp.position + cp.forward * tileSize);
                    if (!occupiedPositions.Contains(cpGrid))
                        openConnectionPoints.Add(cp);
                }
            }
        }
    }

    void GenerateBranch(GameObject crossIntersection, Transform usedPoint, int depth)
    {
        if (depth >= maxBranchDepth) return;

        foreach (Transform cp in crossIntersection.transform)
        {
            if (cp == usedPoint) continue;

            Vector3 targetPos = cp.position + cp.forward * tileSize;
            Vector3Int gridPos = ToGridPosition(targetPos);

            if (occupiedPositions.Contains(gridPos)) continue;

            GameObject branchRoad = Instantiate(straightRoadPrefab, Vector3.zero, Quaternion.identity, transform);
            Transform attach = GetClosestConnectionPoint(branchRoad, cp);

            if (attach == null)
            {
                DestroyImmediate(branchRoad);
                continue;
            }

            AlignToConnection(branchRoad.transform, attach, cp);
            occupiedPositions.Add(gridPos);
            roadTypeAtPosition[gridPos] = "Straight";
        }
    }

    GameObject ChooseRoadType()
    {
        return (consecutiveStraightRoads >= crossIntersectionInterval) ? crossIntersectionPrefab : straightRoadPrefab;
    }

    Transform GetConnectionPoint(GameObject roadPiece, string pointName)
    {
        foreach (Transform child in roadPiece.transform)
        {
            if (child.name.Equals(pointName, System.StringComparison.OrdinalIgnoreCase))
                return child;
        }
        return null;
    }

    Transform GetClosestConnectionPoint(GameObject roadPiece, Transform target)
    {
        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (Transform child in roadPiece.transform)
        {
            float dist = Vector3.Distance(child.position, target.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = child;
            }
        }
        return closest;
    }

    void AlignToConnection(Transform roadTransform, Transform roadStart, Transform targetConnection)
    {
        Quaternion rotation = Quaternion.FromToRotation(roadStart.forward, -targetConnection.forward);
        roadTransform.rotation = rotation * roadTransform.rotation;

        Vector3 euler = roadTransform.eulerAngles;
        euler.x = -90f;
        roadTransform.rotation = Quaternion.Euler(euler);

        Vector3 offset = targetConnection.position - roadStart.position;
        roadTransform.position += offset;
    }

    Vector3Int ToGridPosition(Vector3 position)
    {
        return new Vector3Int(
            Mathf.RoundToInt(position.x / tileSize),
            Mathf.RoundToInt(position.y / tileSize),
            Mathf.RoundToInt(position.z / tileSize)
        );
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (roadTypeAtPosition == null) return;

        foreach (var entry in roadTypeAtPosition)
        {
            Gizmos.color = Color.green;
            if (entry.Value == "Conflict")
                Gizmos.color = Color.red;

            Vector3 worldPos = new Vector3(entry.Key.x * tileSize, entry.Key.y * tileSize, entry.Key.z * tileSize);
            Gizmos.DrawWireCube(worldPos, Vector3.one * tileSize * 0.9f);
        }
    }
#endif
}
