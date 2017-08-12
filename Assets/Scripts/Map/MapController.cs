using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapController : SingletonMonoBehavior<MapController>
{
    [System.NonSerialized]
    public string MapFileName;
    [System.NonSerialized]
    public int MapSizeX = 64;
    [System.NonSerialized]
    public int MapSizeZ = 64;
    public Vector3 CenterPosition = Vector3.zero;
    public bool IsUsedByEditor = false;

    public MapData MapDataCollection
    {
        private set;
        get;
    }

    public MapObjectData MapObjectDataCollection
    {
        private set;
        get;
    }

    GameObject mapObjectParent;
    //GameObject decalTarget = null;
    List<GameObject> decalAffectObjects = new List<GameObject>();

    // Use this for initialization
    void Awake()
    {
        base.Awake();
    }

	// Update is called once per frame
	void Update ()
    {
   
	}

    public void ReLoadMap()
    {
        if (mapObjectParent != null)
        {
            Destroy(mapObjectParent);
        }

        string path = MapSetting.MAP_DATA_FOLDER_NAME + MapFileName;
        if (MapDataCollection != null)
        {
            MapDataCollection.Load(path);
        }

        if (MapObjectDataCollection != null)
        {
            MapObjectDataCollection.Load(path + "_Object");
        }

        DrawMap();
    }

    public void LoadMap(string fileName)
    {
        MapFileName = fileName;
        string path = MapSetting.MAP_DATA_FOLDER_NAME + MapFileName;
        MapDataCollection = new MapData(path);
        MapSizeX = MapDataCollection.MapSizeX;
        MapSizeZ = MapDataCollection.MapSizeZ;
        CenterPosition = MapDataCollection.MapCenterPosition;

        if (IsUsedByEditor)
        {
            MapObjectDataCollection = new MapObjectData(path + "_Object");
            DrawMap();
        }
       else
        {
            Application.LoadLevelAdditive(fileName);
        }
    }

    public void NewMap(string fileName, int mapSizeX, int mapSizeZ)
    {
        if (mapObjectParent != null)
        {
            Destroy(mapObjectParent);
        }

        MapSizeX = mapSizeX;
        MapSizeZ = mapSizeZ;
        MapFileName = fileName;
        MapDataCollection = new MapData(MapSizeX, MapSizeZ, CenterPosition);
        MapObjectDataCollection = new MapObjectData();
        DrawMap();
    }

    public void DrawMap()
    {
        mapObjectParent = DrawingMap.DrawMap(MapDataCollection, MapObjectDataCollection, CenterPosition, Quaternion.identity);

        if (IsUsedByEditor)
        {
            AssetCellData[] assets = mapObjectParent.GetComponentsInChildren<AssetCellData>();
            int len = assets.Length;
            for (int i = 0; i < len; i++)
            {
                int objectCount = assets[i].MapObj.ObjectDataList.Count;
                for (int j = 0; j < objectCount; j++)
                {
                    onCreateMapObject(assets[i].MapObj.ObjectDataList[j].Go);
                }
            }
        }
    }

    public void ModifyMapCenterPosition(Vector3 center)
    {
        if (mapObjectParent != null)
        {
            Destroy(mapObjectParent);
        }

        MapDataCollection.MapCenterPosition = center;
        IEnumerator cellDataItr = MapDataCollection.GetMapEnumerator();
        while (cellDataItr.MoveNext())
        {
            CellData cell = cellDataItr.Current as CellData;
            cell.UpdateHeightest();
        }
        CenterPosition = center;
        DrawMap();
    }

    /// <summary>
    /// 更新cell資料
    /// </summary>
    /// <param name="prefabName"></param>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="height"></param>
    /// <param name="rotation"></param>
    public void UpdateCellData(string prefabName, int x, int z, float height, Quaternion rotation)
    {
        UpdateCellData(prefabName, x, z, height, Vector3.one,rotation);
    }

    /// <summary>
    /// 更新cell資料
    /// </summary>
    /// <param name="prefabName"></param>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="height"></param>
    /// <param name="assetSize"></param>
    /// <param name="rotation"></param>
    public void UpdateCellData(string prefabName, int x, int z, float height, Vector3 assetSize, Quaternion rotation)
    {
        UpdateCellData(prefabName, x, z, height, assetSize, rotation, UnitType.OBJECTS);
    }

    /// <summary>
    /// 更新cell資料
    /// </summary>
    /// <param name="prefabName"></param>
    /// <param name="x"></param>
    /// <param name="z"></param>
    /// <param name="height"></param>
    /// <param name="assetSize"></param>
    /// <param name="rotation"></param>
    /// <param name="unitType"></param>
    public void UpdateCellData(string prefabName, int x, int z, float height, Vector3 assetSize, Quaternion rotation, UnitType unitType)
    {
        if (MapDataCollection.CanBuildOnTheMap(x, z, height, assetSize))
        {
            int id = MapUtility.CoordinateToId(x, z, MapDataCollection.MapSizeX);
            MapObject mapObject = MapObjectDataCollection.GetMapObjectById(id);
            if (mapObject == null)
            {
                mapObject = MapObjectDataCollection.AddMapObject(id);
            }

            MapObject.ObjectData o = MapObjectDataCollection.AddObjectData(id);
            o.PrefabName = prefabName;
            o.Height = height;
            o.Rotation = rotation;

            MapDataCollection.WriteCellData(x, z, assetSize, unitType, height);

            GameObject obj = DrawingMap.DrawMapObjectData(mapObject, o, CenterPosition, MapSizeX, MapSizeZ, x, z, mapObjectParent.transform);
            onCreateMapObject(obj);
            MapObjectDataCollection.RaiseFinishAddObjectData(o);
        }
    }

    /// <summary>
    /// 清除cell資料
    /// </summary>
    /// <param name="x"></param>
    /// <param name="z"></param>
    public void EraseCellData(int x, int z)
    {
        int id = MapUtility.CoordinateToId(x, z, MapDataCollection.MapSizeX);
        MapObject mapObject = MapObjectDataCollection.GetMapObjectById(id);
        if (mapObject != null)
        { 
            MapObjectDataCollection.RemoveMapObject(id);
        }

        MapDataCollection.WriteCellData(id, UnitType.NONE, Vector3.one);
    }

    /// <summary>
    /// 清除cell資料
    /// </summary>
    /// <param name="assetCellData"></param>
    public void EraseCellData(AssetCellData assetCellData)
    {
        MapObject mapObject = assetCellData.MapObj;
        if (mapObject != null)
        {
            int xIndex = -1;
            int zIndex = -1;
            MapUtility.IdToCoordinate(mapObject.Id, MapSizeX, ref xIndex, ref zIndex);
            MapDataCollection.WriteCellData(xIndex, zIndex, assetCellData.Size, UnitType.NONE, assetCellData.ObjData.Height);
            MapObjectDataCollection.RemoveObjectData(mapObject.Id, assetCellData.ObjData);
        }
    }

    /// <summary>
    /// Raycast a cell and get the cell index
    /// </summary>
    /// <param name="xIndex"></param>
    /// <param name="zIndex"></param>
    /// <param name="layerMask"></param>
    public void GetCellByMousePosition(ref int xIndex, ref int zIndex, int layerMask = 1)
    {
        xIndex = zIndex = -1;
        MapUtility.CalCellIndexByMousePosition(CenterPosition, MapSizeX, MapSizeZ, layerMask, ref xIndex, ref zIndex);
    }

    /// <summary>
    /// Raycast a cell and get the cell index
    /// </summary>
    /// <param name="layerMask"></param>
    /// <returns></returns>
    public MapIndex GetCellByMousePosition(int layerMask = 1 << 8)
    {
        int xIndex = -1;
        int zIndex = -1;
        GetCellByMousePosition(ref xIndex, ref zIndex, layerMask);

        MapIndex m;
        m.x = xIndex;
        m.z = zIndex;
        return m;
    }

    /// <summary>
    /// Set the cell if a person can move.
    /// </summary>
    /// <param name="xIndex"></param>
    /// <param name="zIndex"></param>
    /// <param name="movable"></param>
    /// <returns></returns>
    public bool SetCellMovable(int xIndex, int zIndex, bool movable)
    {
        CellData cell = MapDataCollection[xIndex, zIndex];
        if (cell != null && cell.IsCanMove() != movable)
        {
            if (movable)
            {
                cell.RemoveUnit(UnitType.OBSTACLE);
            }
            else
            {
                cell.AddUnit(UnitType.OBSTACLE);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 依照3d位置，取得cell的資料
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public CellData GetCell(Vector3 pos)
    {
        int x = 0;
        int z = 0;
        MapUtility.GetCellIndexBy3dPosition(pos, CenterPosition, MapSizeX, MapSizeZ, ref x, ref z);
        return MapDataCollection.GetCell(x, z);
    }

    /// <summary>
    /// 依照3d位置，取得cell的index
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="xIndex"></param>
    /// <param name="zIndex"></param>
    public void GetCellIndexBy3dPosition(Vector3 pos, ref int xIndex, ref int zIndex)
    {
        MapUtility.GetCellIndexBy3dPosition(pos, CenterPosition, MapSizeX, MapSizeZ, ref xIndex, ref zIndex);
    }

    /// <summary>
    /// 依照3d位置，取得cell的index
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="xIndex"></param>
    /// <param name="zIndex"></param>
    public MapIndex GetCellIndexBy3dPosition(Vector3 pos)
    {
        int xIndex = -1;
        int zIndex = -1;
        MapUtility.GetCellIndexBy3dPosition(pos, CenterPosition, MapSizeX, MapSizeZ, ref xIndex, ref zIndex);
        MapIndex m;
        m.x = xIndex;
        m.z = zIndex;
        return m;
    }

    /// <summary>
    /// 依照3d位置，取得此cell的3d位置
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public Vector3 GetCellPosition(Vector3 pos)
    {
        int xIndex = -1;
        int zIndex = -1;
        GetCellIndexBy3dPosition(pos, ref xIndex, ref zIndex);
        return GetCellPosition(xIndex, zIndex);
    }

    /// <summary>
    /// 依照3d位置，取得此cell的3d位置
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="assetSize"></param>
    /// <returns></returns>
    public Vector3 GetCellPosition(Vector3 pos, Vector3 assetSize)
    {
        int xIndex = -1;
        int zIndex = -1;
        GetCellIndexBy3dPosition(pos, ref xIndex, ref zIndex);
        return GetCellPosition(xIndex, zIndex, assetSize);
    }

    /// <summary>
    /// 依照3d位置，取得此cell的3d位置
    /// </summary>
    /// <param name="xIndex"></param>
    /// <param name="zIndex"></param>
    /// <returns></returns>
    public Vector3 GetCellPosition(int xIndex, int zIndex)
    {
        if (xIndex < 0 || zIndex < 0 || xIndex >= MapSizeX || zIndex >= MapSizeZ)
        {
            return Vector3.zero;
        }

        return MapUtility.CalCellPosition(MapDataCollection, MapUtility.CalTopLeftCellPosition(CenterPosition, MapSizeX, MapSizeZ), xIndex, zIndex);

    }

    /// <summary>
    /// 依照3d位置，取得此cell的3d位置
    /// </summary>
    /// <param name="xIndex"></param>
    /// <param name="zIndex"></param>
    /// <param name="assetSize"></param>
    /// <returns></returns>
    public Vector3 GetCellPosition(int xIndex, int zIndex, Vector3 assetSize)
    {
        if (xIndex < 0 || zIndex < 0 || xIndex >= MapSizeX || zIndex >= MapSizeZ)
        {
            return Vector3.zero;
        }

        return MapUtility.CalCellPosition(MapDataCollection, MapUtility.CalTopLeftCellPosition(CenterPosition, MapSizeX, MapSizeZ), xIndex, zIndex, assetSize);
    }

    /// <summary>
    /// 將物件放在此cell的上方
    /// </summary>
    /// <param name="xIndex"></param>
    /// <param name="zIndex"></param>
    /// <param name="prefab"></param>
    /// <param name="heightOffset">在地圖的最高點，往上移動多少距離</param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public GameObject PutObjectOnTheMap(int xIndex, int zIndex, Object prefab, float heightOffset = 0.1f, Transform parent = null)
    {
        if (prefab == null || !IsInTheMap(xIndex, zIndex))
        {
            return null;
        }

        Vector3 cellPos = GetCellPosition(xIndex, zIndex);
        GameObject o = Instantiate(prefab, cellPos, Quaternion.identity) as GameObject;
        o.transform.localPosition = new Vector3(o.transform.localPosition.x, cellPos.y + heightOffset, o.transform.localPosition.z);
        o.transform.parent = parent;

        doDecal(o, cellPos);
        return o;
    }

    public void PutObjectOnTheMap(int xIndex, int zIndex, GameObject o, float heightOffset = 0.1f)
    {
        if (!IsInTheMap(xIndex, zIndex))
        {
            return;
        }

        Vector3 cellPos = GetCellPosition(xIndex, zIndex);
        o.transform.localPosition = new Vector3(cellPos.x, cellPos.y + heightOffset, cellPos.z);
        doDecal(o, cellPos);
    }

    public void PutObjectOnTheMap(Vector3 pos, GameObject o)
    {
        o.transform.position = pos;
        doDecal(o, pos);
    }

    void doDecal(GameObject o, Vector3 cellPos)
    {
        Decal decal = o.GetComponent<Decal>();
        if (decal != null)
        {
            o.transform.forward = Vector3.up;
            Collider[] affected = MapUtility.GetGameObjectByPosition(cellPos, 0.5f, 1 << LayerMask.NameToLayer("Decal"));
            decalAffectObjects.Clear();
            int len = affected.Length;
            if (len > 0)
            {
                for (int i = 0; i < len; i++)
                {
                    decalAffectObjects.Add(affected[i].gameObject);
                }

                decal.BuildDecal(decalAffectObjects);
            }
        }
    }

    /// <summary>
    /// Check if the index is within the map 
    /// </summary>
    /// <param name="xIndex"></param>
    /// <param name="zIndex"></param>
    /// <returns></returns>
    public bool IsInTheMap(int xIndex, int zIndex)
    {
        return xIndex >= 0 && xIndex < MapSizeX && zIndex >= 0 && zIndex < MapSizeZ;
    }

    void onCreateMapObject(GameObject go)
    {
        
    }
}
