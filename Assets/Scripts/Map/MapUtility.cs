using UnityEngine;
using System.Collections;

public static class MapUtility
{
    static public void IdToCoordinate(int id, int cellSizeX, ref int xIndex, ref int zIndex)
    {
        zIndex = id / cellSizeX;
        xIndex = id % cellSizeX;
    }

    static public int CoordinateToId(int x, int z, int cellSizeX)
    {
        return z * cellSizeX + x;
    }

    /// <summary>
    /// 計算grid的cellX = 0, cellZ = 0的3d位置 
    /// </summary>
    /// <param name="centerPos"></param>
    /// <param name="MapSizeX"></param>
    /// <param name="MapSizeZ"></param>
    /// <returns></returns>
    static public Vector3 CalTopLeftCellPosition(Vector3 centerPos, int MapSizeX, int MapSizeZ)
    {
        Vector3 cellSize = new Vector3(MapSizeX, 0f, MapSizeZ);
        Vector3 pos = centerPos - cellSize * 0.5f * MapSetting.CELL_UNIT_SIZE;
        pos = new Vector3(pos.x, centerPos.y, pos.z);
        return pos;
    }

    /// <summary>
    /// 計算cell(xIndex, zIndex)的3d位置
    /// </summary>
    /// <param name="mapData"></param>
    /// <param name="topLeftPos"></param>
    /// <param name="xIndex"></param>
    /// <param name="zIndex"></param>
    /// <returns></returns>
    static public Vector3 CalCellPosition(MapData mapData, Vector3 topLeftPos, int xIndex, int zIndex)
    {
        return CalCellPosition(mapData, topLeftPos, xIndex, zIndex, Vector3.one);
    }

    /// <summary>
    /// 計算cell(xIndex, zIndex)的3d位置
    /// </summary>
    /// <param name="mapData"></param>
    /// <param name="topLeftPos"></param>
    /// <param name="xIndex"></param>
    /// <param name="zIndex"></param>
    /// <param name="assetSize"></param>
    /// <returns></returns>
    static public Vector3 CalCellPosition(MapData mapData, Vector3 topLeftPos, int xIndex, int zIndex, Vector3 assetSize)
    {
        Vector3 pos = topLeftPos + new Vector3(xIndex * MapSetting.CELL_UNIT_SIZE, 0f, zIndex * MapSetting.CELL_UNIT_SIZE);
        //奇數Cell，x、z要位移0.5，偶數Cell則不需要
        //if x = 4, (x % 2) * 0.5f = 0
        //if x = 5, (x % 2) * 0.5f = 0.5f
        pos += assetSize.GetCellSize().ModuleValue(2).MultipleValue(Vector3.one * 0.5f);

        if (mapData == null)
        {
            pos.Set(pos.x, topLeftPos.y, pos.z);
        }
        else
        {
            CellData cell = mapData[xIndex, zIndex];
            if (cell != null)
            {
                pos.Set(pos.x, cell.GetHeightest(), pos.z);
            }
            else
            {
                pos.Set(pos.x, topLeftPos.y, pos.z);
            }
        }

        return pos;
    }

    /// <summary>
    /// 計算包含asset在內的cell(xIndex, zIndex)的高度，即cell目前最高 + asset的中心點高度
    /// </summary>
    /// <param name="mapData"></param>
    /// <param name="xIndex"></param>
    /// <param name="zIndex"></param>
    /// <param name="cellData"></param>
    /// <returns></returns>
    static public float CalCellHeightPosition(MapData mapData, int xIndex, int zIndex, AssetCellData cellData)
    {
        float cellHeight = mapData.GetCell(xIndex, zIndex).GetHeightest();
        return cellHeight + cellData.Size.y * 0.5f - cellData.Center.y;
    }

    /// <summary>
    /// 計算包含asset在內的cell(xIndex, zIndex)的高度，即cell目前最高 + asset的中心點高度
    /// </summary>
    /// <param name="mapData"></param>
    /// <param name="xIndex"></param>
    /// <param name="zIndex"></param>
    /// <param name="cellData"></param>
    /// <param name="curPosition"></param>
    /// <returns></returns>
    static public Vector3 CalCellHeightPosition(MapData mapData, int xIndex, int zIndex, AssetCellData cellData, Vector3 curPosition)
    {
        float yPos = CalCellHeightPosition(mapData, xIndex, zIndex, cellData);
        return new Vector3(curPosition.x, yPos, curPosition.z);
    }

    /// <summary>
    /// 利用global的3d位置，取得該位置的cell index
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="centerPosition"></param>
    /// <param name="MapSizeX"></param>
    /// <param name="MapSizeZ"></param>
    /// <param name="xIndex"></param>
    /// <param name="zIndex"></param>
    static public void GetCellIndexBy3dPosition(Vector3 pos, Vector3 centerPosition, int MapSizeX, int MapSizeZ, ref int xIndex, ref int zIndex)
    {
        Vector3 diffCenterPosition = pos - centerPosition;
        diffCenterPosition /= MapSetting.CELL_UNIT_SIZE;

        xIndex = Mathf.FloorToInt(diffCenterPosition.x + MapSizeX * 0.5f);
        zIndex = Mathf.FloorToInt(diffCenterPosition.z + MapSizeZ * 0.5f);
    }

    //x and z are the center of cell
    /// <summary>
    /// 計算此asset的第一個cell(因為asset可能不是只佔一個cell，它的大小為assetSize)
    /// 在地圖上是位於哪個cell
    /// </summary>
    /// <param name="x = 在地圖上位於哪個cell，此asset的pivot在中心"></param>
    /// <param name="z = 在地圖上位於哪個cell，此asset的pivot在中心"></param>
    /// <param name="assetSize"></param>
    /// <param name="xIndex"></param>
    /// <param name="zIndex"></param>
    static public void CalCellIndexByCenter(int x, int z, Vector3 assetSize, ref int xIndex, ref int zIndex)
    {
        Vector3 numberOfCell = assetSize.GetCellSize();
        Vector3 halfOfCell = (numberOfCell / 2).FloorValue();
        Vector3 topLeftCell = new Vector3(x + halfOfCell.x - numberOfCell.x + (numberOfCell.x % 2), 0, z + halfOfCell.z - numberOfCell.z + (numberOfCell.z % 2));
        xIndex = (int)topLeftCell.x;
        zIndex = (int)topLeftCell.z;
    }

    /// <summary>
    /// 依照mouse的位置，計算出所在的cell index
    /// </summary>
    /// <param name="centerPosition"></param>
    /// <param name="mapSizeX"></param>
    /// <param name="mapSizeZ"></param>
    /// <param name="rayCastLayer"></param>
    /// <param name="xIndex"></param>
    /// <param name="zIndex"></param>
    static public void CalCellIndexByMousePosition(Vector3 centerPosition, int mapSizeX, int mapSizeZ, int rayCastLayer, ref int xIndex, ref int zIndex)
    {
        CalCellIndexByMousePosition(Input.mousePosition, centerPosition, mapSizeX, mapSizeZ, rayCastLayer, ref xIndex, ref zIndex);
    }

    /// <summary>
    /// 依照mouse的位置，計算出所在的cell index
    /// </summary>
    /// <param name="mousePosition"></param>
    /// <param name="centerPosition"></param>
    /// <param name="MapSizeX"></param>
    /// <param name="MapSizeZ"></param>
    /// <param name="rayCastLayer"></param>
    /// <param name="xIndex"></param>
    /// <param name="zIndex"></param>
    static public void CalCellIndexByMousePosition(Vector3 mousePosition, Vector3 centerPosition, int MapSizeX, int MapSizeZ, int rayCastLayer, ref int xIndex, ref int zIndex)
    {
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000, rayCastLayer))
        {
            Debug.DrawLine(Camera.main.transform.position, hit.point);
            Debug.Log(hit.point + " " + hit.collider.name);
            GetCellIndexBy3dPosition(hit.point, centerPosition, MapSizeX, MapSizeZ, ref xIndex, ref zIndex);
        }
    }

    /// <summary>
    /// Ray cast，找出cast哪個物件
    /// </summary>
    /// <param name="layerMask"></param>
    /// <returns></returns>
    static public GameObject GetRayCastMapObjectByMousePosition(int layerMask)
    {
        return GetRayCastMapObjectByMousePosition(Input.mousePosition, layerMask);
    }

    /// <summary>
    /// Ray cast，找出cast哪個物件
    /// </summary>
    /// <param name="mousePosition"></param>
    /// <param name="layerMask"></param>
    /// <returns></returns>
    static public GameObject GetRayCastMapObjectByMousePosition(Vector3 mousePosition, int layerMask)
    {
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000, layerMask);

        int len = hits.Length;
        for (int i = 0; i < len; i++)
        {
            GameObject o = hits[i].transform.gameObject;
            if (o.name != "Grid")
            {
                return o;
            }
        }

        return null;
    }

    static public Collider[] GetGameObjectByPosition(Vector3 position, float radius, int layerMask)
    {
        return Physics.OverlapSphere(position, radius, layerMask);
    }

    static public Vector3 GetCellSize(this Vector3 assetSize)
    {
        return new Vector3(Mathf.RoundToInt(assetSize.x), assetSize.y, Mathf.RoundToInt(assetSize.z));
    } 
}

public static class Vector3Extension
{
    static public Vector3 MultipleValue(this Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    static public Vector3 ModuleValue(this Vector3 a, int module)
    {
        return new Vector3(a.x % module, a.y % module, a.z % module);
    }

    static public Vector3 FloorValue(this Vector3 a)
    {
        return new Vector3(Mathf.Floor(a.x), Mathf.Floor(a.y), Mathf.Floor(a.z));
    }

    static public Vector3 AbsValue(this Vector3 a)
    {
        return new Vector3(Mathf.Abs(a.x), Mathf.Abs(a.y), Mathf.Abs(a.z));
    }
}

public static class StringExtension
{
    static public string GetPathWidthoutExtension(this string s)
    {
        int index = s.LastIndexOf('.');
        if (index >= 0)
        {
            return s.Substring(0, index);
        }

        return s;
    }
}
