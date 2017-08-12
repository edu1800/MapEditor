using UnityEngine;
using System.Collections;

public static class MapSetting
{
    public const string MAP_DATA_FOLDER_NAME = "MapData/Data/";
    public const string MAP_REFAB_FOLDER_NAME = "MapData/Prefab/";
    public const string MAP_PATTERN_FOLDER_NAME = "MapData/Pattern/";
    public const string MAP_LAYER_FOLDER_NAME = "MapData/Layer/";
    public const string MAP_POSITIONING_FOLDER_NAME = "MapData/Positioning/";
    public const string MAP_CONNECT_TILE_FOLDER_NAME = "MapData/ConnectTile/";
    public const string MAP_RANDOM_TILE_FOLDER_NAME = "MapData/RandomTile/";

    public const int CELL_UNIT_SIZE = 1;
    public const string MAP_OBJECT_DEFAULT_LAYER_NAME = "Default";
    public const int IGNORE_RAY_CAST_LAYER_MASK = 1 << 2;
}
