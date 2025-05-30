using UnityEngine;
using System.Collections.Generic;

public class GridBasedRoadGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject straightRoadPrefab;
    public GameObject crossIntersectionPrefab;
    public GameObject treePrefab;
    public GameObject terrainPrefab;

    [Header("Road Settings")]
    public int mainRoadLength = 30;
    public int gridSpacing = 10;
    public float branchChance = 0.4f;

    [Header("Tree Settings")]
    [Range(0f, 1f)] public float treeDensity = 0.5f;
    public float treeOffset = 5f;

    [Header("Terrain Settings")]
    public float terrainPadding = 20f;

    private Dictionary<Vector2Int, GameObject> grid = new Dictionary<Vector2Int, GameObject>();
    private Queue<(Vector2Int gridPos, Vector3 direction)> openConnections = new Queue<(Vector2Int, Vector3)>();
    private List<Vector3> treeSpawnCandidates = new List<Vector3>();
    private HashSet<Vector2Int> treeOccupiedGrid = new HashSet<Vector2Int>();

    private Transform roadParent;
    private Transform intersectionParent;
    private Transform treeParent;

    [ContextMenu("Generate Randomized Road Network")]
    public void GenerateRoadNetwork()
    {
        // Clean up
        foreach (Transform child in transform)
            DestroyImmediate(child.gameObject);

        grid.Clear();
        openConnections.Clear();
        treeSpawnCandidates.Clear();
        treeOccupiedGrid.Clear();

        // Setup parents
        roadParent = new GameObject("Roads").transform;
        roadParent.parent = transform;

        intersectionParent = new GameObject("Intersections").transform;
        intersectionParent.parent = transform;

        treeParent = new GameObject("Trees").transform;
        treeParent.parent = transform;

        // Start road generation
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

            if (PlaceRoad(prefab, currentGrid, direction, placeCrossIntersection))
            {
                placed++;

                if (placeCrossIntersection)
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

        // Spawn trees only after all roads are placed
        SpawnAllTrees();

        // Snap terrain under network
        if (terrainPrefab != null)
        {
            SnapTerrainUnderNetwork();
        }

        Debug.Log($"Road generation completed. Total placed: {placed}");
    }

    bool PlaceRoad(GameObject prefab, Vector2Int gridPos, Vector3 direction, bool isIntersection = false)
    {
        if (grid.ContainsKey(gridPos)) return false;

        Vector3 worldPos = new Vector3(gridPos.x, 0, gridPos.y) * gridSpacing;
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(-90, 0, 0);

        Transform parent = isIntersection ? intersectionParent : roadParent;
        GameObject road = Instantiate(prefab, worldPos, rotation, parent);
        grid[gridPos] = road;

        QueueTreeSpawns(worldPos, direction);
        return true;
    }

    void QueueTreeSpawns(Vector3 roadPos, Vector3 roadDir)
    {
        if (treePrefab == null || treeDensity <= 0f) return;

        Vector3 sideDir = Vector3.Cross(Vector3.up, roadDir.normalized);

        for (int i = -1; i <= 1; i += 2)
        {
            if (Random.value > treeDensity) continue;

            Vector3 offset = sideDir * i * treeOffset;
            Vector3 jitter = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
            Vector3 treePos = roadPos + offset + jitter;

            treeSpawnCandidates.Add(treePos);
        }
    }

    void SpawnAllTrees()
    {
        foreach (var pos in treeSpawnCandidates)
        {
            Vector2Int gridPos = new Vector2Int(
                Mathf.RoundToInt(pos.x / gridSpacing),
                Mathf.RoundToInt(pos.z / gridSpacing)
            );

            if (grid.ContainsKey(gridPos) || treeOccupiedGrid.Contains(gridPos))
            {
                continue;
            }

            Quaternion treeRot = Quaternion.Euler(-90f, Random.Range(0f, 360f), 0f);
            Instantiate(treePrefab, pos, treeRot, treeParent);
            treeOccupiedGrid.Add(gridPos);
        }
    }

    void SnapTerrainUnderNetwork()
    {
        if (grid.Count == 0) return;

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var pos in grid.Keys)
        {
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
        }

        float width = (maxX - minX + 1) * gridSpacing + terrainPadding * 2;
        float depth = (maxY - minY + 1) * gridSpacing + terrainPadding * 2;
        float centerX = ((minX + maxX + 1) / 2f) * gridSpacing;
        float centerZ = ((minY + maxY + 1) / 2f) * gridSpacing;

        Vector3 centerPosition = new Vector3(centerX, 0, centerZ);
        Vector3 terrainScale = new Vector3(width / 10f, 1f, depth / 10f);

        GameObject terrain = Instantiate(terrainPrefab, centerPosition, Quaternion.identity, transform);
        terrain.transform.localScale = terrainScale;
        terrain.name = "Snapped Terrain";
    }

    Vector2Int DirToGrid(Vector3 dir)
    {
        return Vector2Int.RoundToInt(new Vector2(dir.x, dir.z));
    }
}
