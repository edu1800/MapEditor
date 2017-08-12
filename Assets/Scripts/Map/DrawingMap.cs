using UnityEngine;
using System.Collections;

public static class DrawingMap
{
    static public GameObject DrawMap(MapData map, MapObjectData mapObject, Vector3 pos, Quaternion rot)
    {
        if (map == null || mapObject == null)
            return null;

        return DrawCells(mapObject.GetObjectEnumerator(), map.MapSizeX, map.MapSizeZ, pos);
    }

    static public GameObject DrawGrid(int MapSizeX, int MapSizeZ, Vector3 pos, Quaternion rot)
    {
        Object gridUnit = Resources.Load(MapSetting.MAP_REFAB_FOLDER_NAME + "GridUnit");

        if (gridUnit != null)
        {
            GameObject grid = GameObject.Instantiate(gridUnit, pos, rot) as GameObject;                      
            grid.name = "Grid";
            grid.transform.localScale = new Vector3(MapSizeX, 0.1f, MapSizeZ);
            grid.transform.position = pos;
            grid.transform.rotation = rot;
            grid.GetComponent<Renderer>().material.mainTextureScale = new Vector2(MapSizeX, MapSizeZ);
            return grid;
        }

        return null;
    }

    static public void DrawCells(IEnumerator mapObjectItr, int MapSizeX, int MapSizeZ, Vector3 centerPos, GameObject parent)
    {
        Vector3 topLeftCellPos = MapUtility.CalTopLeftCellPosition(centerPos, MapSizeX, MapSizeZ);
        while (mapObjectItr.MoveNext())
        {
            MapObject mapObj = mapObjectItr.Current as MapObject;
            DrawCellByTopLeftCellPosition(mapObj, topLeftCellPos, MapSizeX, MapSizeZ, parent.transform);
        }
    }

    static public GameObject DrawCells(IEnumerator mapObjectItr, int MapSizeX, int MapSizeZ, Vector3 centerPos)
    {    
        GameObject parent = new GameObject("Map");
        parent.transform.position = Vector3.zero;
        parent.transform.rotation = Quaternion.identity;
        parent.transform.localScale = Vector3.one;

        DrawCells(mapObjectItr, MapSizeX, MapSizeZ, centerPos, parent);
        return parent;
    }

    static public bool DrawCell(MapObject mapObject, Vector3 centerPos, int MapSizeX, int MapSizeZ, Transform parent = null)
    {
        Vector3 topLeftCellPos = MapUtility.CalTopLeftCellPosition(centerPos, MapSizeX, MapSizeZ);
        DrawCellByTopLeftCellPosition(mapObject, topLeftCellPos, MapSizeX, MapSizeZ, parent);
        return true;
    }

    static void DrawCellByTopLeftCellPosition(MapObject mapObject, Vector3 topLeftCellPos, int MapSizeX, int MapSizeZ, Transform parent = null)
    {
        int xIndex = 0;
        int zIndex = 0;
        MapUtility.IdToCoordinate(mapObject.Id, MapSizeX, ref xIndex, ref zIndex);

        int len = mapObject.ObjectDataList.Count;
        for (int i = 0; i < len; i++)
        {
            DrawMapObjectDataTopLeftCellPosition(mapObject, mapObject.ObjectDataList[i], topLeftCellPos, xIndex, zIndex,  parent);         
        }
    }

    static public GameObject DrawMapObjectData(MapObject mapObject, MapObject.ObjectData o, Vector3 centerPos, int MapSizeX, int MapSizeZ, int xIndex, int zIndex, Transform parent = null)
    {
        Vector3 topLeftCellPos = MapUtility.CalTopLeftCellPosition(centerPos, MapSizeX, MapSizeZ);
        return DrawMapObjectDataTopLeftCellPosition(mapObject, o, topLeftCellPos, xIndex, zIndex, parent);
    }

    static GameObject DrawMapObjectDataTopLeftCellPosition(MapObject mapObject, MapObject.ObjectData o, Vector3 topLeftCellPos, int xIndex, int zIndex, Transform parent = null)
    {
        Object obj = Resources.Load(o.PrefabName);
        if (obj != null)
        {
            GameObject go = GameObject.Instantiate(obj) as GameObject;
            go.name = go.name.Substring(0, go.name.LastIndexOf('('));
            AssetCellData assetData = go.GetComponent<AssetCellData>();
            assetData.Rotate(o.Rotation.eulerAngles.y);
            go.transform.parent = parent;
            go.transform.localPosition = MapUtility.CalCellPosition(null, topLeftCellPos, xIndex, zIndex, assetData.Size);
            go.transform.localPosition = new Vector3(go.transform.localPosition.x, o.Height, go.transform.localPosition.z);
            go.transform.rotation = o.Rotation;
            go.layer = LayerMask.NameToLayer("Map");

            o.Go = go;
            assetData.MapObj = mapObject;
            assetData.ObjData = o;

            return go;
        }

        return null;
    }
}
