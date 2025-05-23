using UnityEngine;
using System.Collections.Generic;

public class GridBasedRoadGenerator : MonoBehaviour
{
    public GameObject straightRoadPrefab;
    public GameObject crossIntersectionPrefab;
    public GameObject treePrefab;

    public int mainRoadLength = 30;
    public int gridSpacing = 10;
    public float branchChance = 0.4f;

    [Range(0f, 1f)]
    public float treeDensity = 0.5f;
    public float treeOffset = 5f;

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

            bool isGridIntersection = currentGrid.x % gridSpacing == 0 && currentGrid.y % gridSpacing == 0;
            bool placeCrossIntersection = isGridIntersection && Random.value < 0.3f;

            GameObject prefab = placeCrossIntersection ? crossIntersectionPrefab : straightRoadPrefab;

            if (PlaceRoad(prefab, currentGrid, direction))
            {
                placed++;

                if (prefab == crossIntersectionPrefab)
                {
                    Vector3[] allDirs = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
                    bool added = false;

                    foreach (var dir in allDirs)
                    {
                        if (dir == -direction) continue;

                        Vector2Int nextGrid = currentGrid + DirToGrid(dir);
                        if (grid.ContainsKey(nextGrid)) continue;

                        if (!added || Random.value < branchChance)
                        {
                            openConnections.Enqueue((nextGrid, dir));
                            added = true;
                        }
                    }
                }
                else
                {
                    Vector2Int nextGrid = currentGrid + DirToGrid(direction);
                    if (!grid.ContainsKey(nextGrid))
                        openConnections.Enqueue((nextGrid, direction));
                }
            }
        }

        Debug.Log($"Road generation completed. Total placed: {placed}");
    }

    bool PlaceRoad(GameObject prefab, Vector2Int gridPos, Vector3 direction)
    {
        if (grid.ContainsKey(gridPos)) return false;

        Vector3 worldPos = new Vector3(gridPos.x, 0, gridPos.y) * gridSpacing;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
        rotation *= Quaternion.Euler(-90, 0, 0);

        GameObject road = Instantiate(prefab, worldPos, rotation, transform);
        grid[gridPos] = road;

        SpawnTrees(worldPos, direction);

        return true;
    }

    void SpawnTrees(Vector3 roadPos, Vector3 roadDir)
    {
        if (treePrefab == null || treeDensity <= 0f) return;

        Vector3 sideDir = Vector3.Cross(Vector3.up, roadDir.normalized);

        for (int i = -1; i <= 1; i += 2)
        {
            if (Random.value > treeDensity) continue;

            Vector3 baseOffset = sideDir * i * treeOffset;
            Vector3 jitter = new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
            Vector3 offset = baseOffset + jitter;
            Vector3 treePos = roadPos + offset;

            Quaternion treeRot = Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f);

            Instantiate(treePrefab, treePos, treeRot, transform);
        }
    }

    Vector2Int DirToGrid(Vector3 dir)
    {
        return Vector2Int.RoundToInt(new Vector2(dir.x, dir.z));
    }
}
