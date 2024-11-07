using UnityEngine; // Needed for SerializeField, ExecuteInEditMode
using System.Collections.Generic;

using UnityEngine.Tilemaps;
using System.IO;


#if UNITY_EDITOR
using UnityEditor; // Needed for Editor-related functionality
#endif

[System.Serializable]
public class MapData
{
    public List<int[]> mapList = new List<int[]>();
    public int width;
    public int height;
}

public class LevelGenerator : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase tile;

    [Tooltip("The width of each layer of the stack")]
    public int width;
    [Tooltip("The height of each layer of the stack")]
    public int height;

    [SerializeField] // Add SerializeField to make private fields visible in the Unity Inspector
    public List<MapSettings> mapSettings = new List<MapSettings>();

    List<int[,]> mapList = new List<int[,]>();

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.N))
        {
            tilemap.GetComponent<Tilemap>().ClearAllTiles();
            GenerateMap();
        }
    }

    [ExecuteInEditMode] // Ensures that the method can run even in the editor
    public void GenerateMap()
    {
        ClearMap();
        mapList = new List<int[,]>();

        // Work through the List of mapSettings
        for (int i = 0; i < mapSettings.Count; i++)
        {
            int[,] map = new int[width, height];
            float seed;
            if (mapSettings[i].randomSeed)
            {
                seed = Time.time.GetHashCode();
            }
            else
            {
                seed = mapSettings[i].seed.GetHashCode();
            }

            // Generate the map depending on the algorithm selected
            switch (mapSettings[i].algorithm)
            {
                case Algorithm.Perlin:
                    map = MapFunctions.GenerateArray(width, height, true);
                    map = MapFunctions.PerlinNoise(map, seed);
                    break;
                case Algorithm.PerlinSmoothed:
                    map = MapFunctions.GenerateArray(width, height, true);
                    map = MapFunctions.PerlinNoiseSmooth(map, seed, mapSettings[i].interval);
                    break;
                case Algorithm.PerlinCave:
                    map = MapFunctions.GenerateArray(width, height, true);
                    map = MapFunctions.PerlinNoiseCave(map, mapSettings[i].modifier, mapSettings[i].edgesAreWalls);
                    break;
                case Algorithm.RandomWalkTop:
                    map = MapFunctions.GenerateArray(width, height, true);
                    map = MapFunctions.RandomWalkTop(map, seed);
                    map = MapFunctions.SmoothMooreCellularAutomata(map, mapSettings[i].edgesAreWalls, mapSettings[i].smoothAmount);
                    break;
                case Algorithm.RandomWalkTopSmoothed:
                    map = MapFunctions.GenerateArray(width, height, true);
                    map = MapFunctions.RandomWalkTopSmoothed(map, seed, mapSettings[i].interval);
                    break;
                case Algorithm.RandomWalkCave:
                    map = MapFunctions.GenerateArray(width, height, false);
                    map = MapFunctions.RandomWalkCave(map, seed, mapSettings[i].clearAmount);
                    break;
                case Algorithm.RandomWalkCaveCustom:
                    map = MapFunctions.GenerateArray(width, height, false);
                    map = MapFunctions.RandomWalkCaveCustom(map, seed, mapSettings[i].clearAmount);
                    break;
                case Algorithm.CellularAutomataVonNeuman:
                    map = MapFunctions.GenerateCellularAutomata(width, height, seed, mapSettings[i].fillAmount, mapSettings[i].edgesAreWalls);
                    map = MapFunctions.SmoothVNCellularAutomata(map, mapSettings[i].edgesAreWalls, mapSettings[i].smoothAmount);
                    break;
                case Algorithm.CellularAutomataMoore:
                    map = MapFunctions.GenerateCellularAutomata(width, height, seed, mapSettings[i].fillAmount, mapSettings[i].edgesAreWalls);
                    map = MapFunctions.SmoothMooreCellularAutomata(map, mapSettings[i].edgesAreWalls, mapSettings[i].smoothAmount);
                    break;
                case Algorithm.DirectionalTunnel:
                    map = MapFunctions.GenerateArray(width, height, false);
                    map = MapFunctions.DirectionalTunnel(map, mapSettings[i].minPathWidth, mapSettings[i].maxPathWidth, mapSettings[i].maxPathChange, mapSettings[i].roughness, mapSettings[i].windyness);
                    break;
            }
            mapList.Add(map);
        }

        Vector2Int offset = new Vector2Int(-width, -height / 2);

        // Render the first map at the original size
        if (mapList.Count > 0)
        {
            MapFunctions.RenderMapWithOffset(mapList[0], tilemap, tile, offset);
        }

        // Render subsequent maps with half the width below the first map
        offset.x = 0; // Start from the left for the second row

        for (int i = 1; i < mapList.Count; i++)
        {
            // Render the maps with half the width
            if (i == 1)
            {
                MapFunctions.RenderMapWithOffset(MapFunctions.ResizeMap(mapList[i], width / 4 * 3, height), tilemap, tile, offset);
            }
            else if (i == 2)
            {
                offset.x += width / 4 * 3; // Move to the right for the next map
                MapFunctions.RenderMapWithOffset(MapFunctions.ResizeMap(mapList[i], width / 4, height), tilemap, tile, offset);
            }
        }
    }

    public void ClearMap()
    {
        tilemap.ClearAllTiles();
    }

    [System.Serializable]
    public class MapData
    {
        public int[][][] maps;
        public int width;
        public int height;
    }

    public void SaveMaps(string path)
    {
        MapData mapData = new MapData
        {
            width = this.width,
            height = this.height,
            maps = new int[mapList.Count][][]
        };

        for (int i = 0; i < mapList.Count; i++)
        {
            mapData.maps[i] = new int[height][];
            for (int y = 0; y < height; y++)
            {
                mapData.maps[i][y] = new int[width];
                for (int x = 0; x < width; x++)
                {
                    mapData.maps[i][y][x] = mapList[i][x, y];
                }
            }
        }

        string json = JsonUtility.ToJson(mapData);
        File.WriteAllText(path, json);
    }

    public void LoadMaps(string path)
    {
        string json = File.ReadAllText(path);
        MapData mapData = JsonUtility.FromJson<MapData>(json);
        mapList = new List<int[,]>();

        for (int i = 0; i < mapData.maps.Length; i++)
        {
            int[,] map = new int[mapData.height, mapData.width];
            for (int y = 0; y < mapData.height; y++)
            {
                for (int x = 0; x < mapData.width; x++)
                {
                    map[y, x] = mapData.maps[i][y][x];
                }
            }
            mapList.Add(map);
        }

        RenderLoadedMaps();
    }

    private void RenderLoadedMaps()
    {
        Vector2Int offset = new Vector2Int(-width / 2, -height / 2);

        // Render the first map at the original size
        if (mapList.Count > 0)
        {
            MapFunctions.RenderMapWithOffset(mapList[0], tilemap, tile, offset);
        }

        // Render subsequent maps with half the width below the first map
        offset.y -= height;
        offset.x = -width / 2; // Start from the left for the second row

        for (int i = 1; i < mapList.Count; i++)
        {
            // Render the maps with half the width
            if (i == 1)
            {
                MapFunctions.RenderMapWithOffset(MapFunctions.ResizeMap(mapList[i], width / 4 * 3, height), tilemap, tile, offset);
            }
            else if (i == 2)
            {
                offset.x += width / 4 * 3; // Move to the right for the next map
                MapFunctions.RenderMapWithOffset(MapFunctions.ResizeMap(mapList[i], width / 4, height), tilemap, tile, offset);
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(LevelGenerator))]
public class LevelGeneratorStackEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        LevelGenerator levelGen = (LevelGenerator)target;

        List<Editor> mapEditors = new List<Editor>();

        for (int i = 0; i < levelGen.mapSettings.Count; i++)
        {
            if (levelGen.mapSettings[i] != null)
            {
                Editor mapLayerEditor = CreateEditor(levelGen.mapSettings[i]);
                mapEditors.Add(mapLayerEditor);
            }
        }
        if (mapEditors.Count > 0)
        {
            GUILayout.Label("Map Settings", EditorStyles.boldLabel);
            for (int i = 0; i < mapEditors.Count; i++)
            {
                mapEditors[i].OnInspectorGUI();
            }
        }
    }
}
#endif
