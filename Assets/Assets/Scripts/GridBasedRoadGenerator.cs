using UnityEngine;
using System.Collections.Generic;

public class GridBasedRoadGenerator : MonoBehaviour
{
    public GameObject straightRoadPrefab;
    public GameObject crossIntersectionPrefab;
    public int mainRoadLength = 30;
    public int gridSpacing = 10;
    public float branchChance = 0.4f; // 40% chance to branch from cross intersections

    private Dictionary<Vector2Int, GameObject> grid = new Dictionary<Vector2Int, GameObject>();
    private Queue<(Vector2Int gridPos, Vector3 direction)> openConnections = new Queue<(Vector2Int, Vector3)>();

    [ContextMenu("Generate Randomized Road Network")]
    public void GenerateRoadNetwork()
    {
        foreach (Transform child in transform)
            DestroyImmediate(child.gameObject);

        grid.Clear();
        openConnections.Clear();

        Vector2Int startGrid = Vector2Int.zero;
        Vector3 startDirection = Vector3.forward;

        PlaceRoad(straightRoadPrefab, startGrid, startDirection);
        openConnections.Enqueue((startGrid + DirToGrid(startDirection), startDirection));

        int placed = 0;
        while (openConnections.Count > 0 && placed < mainRoadLength)
        {
            var (currentGrid, direction) = openConnections.Dequeue();

            if (grid.ContainsKey(currentGrid)) continue;

            bool isIntersection = currentGrid.x % gridSpacing == 0 && currentGrid.y % gridSpacing == 0;
            GameObject prefab = isIntersection ? crossIntersectionPrefab : straightRoadPrefab;

            if (PlaceRoad(prefab, currentGrid, direction))
            {
                placed++;

                if (prefab == crossIntersectionPrefab)
                {
                    Vector3[] allDirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

                    foreach (var dir in allDirs)
                    {
                        if (dir == -direction || Random.value > branchChance) continue; // avoid backtracking
                        Vector2Int nextGrid = currentGrid + DirToGrid(dir);
                        if (!grid.ContainsKey(nextGrid))
                            openConnections.Enqueue((nextGrid, dir));
                    }
                }
                else
                {
                    // Continue straight
                    Vector2Int nextGrid = currentGrid + DirToGrid(direction);
                    if (!grid.ContainsKey(nextGrid))
                        openConnections.Enqueue((nextGrid, direction));
                }
            }
        }
    }

    bool PlaceRoad(GameObject prefab, Vector2Int gridPos, Vector3 direction)
    {
        if (grid.ContainsKey(gridPos))
            return false;

        Vector3 worldPos = new Vector3(gridPos.x, 0, gridPos.y) * gridSpacing;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        rotation *= Quaternion.Euler(-90, 0, 0); // flatten

        GameObject road = Instantiate(prefab, worldPos, rotation, transform);
        grid[gridPos] = road;
        return true;
    }

    Vector2Int DirToGrid(Vector3 dir)
    {
        return Vector2Int.RoundToInt(new Vector2(dir.x, dir.z));
    }
}