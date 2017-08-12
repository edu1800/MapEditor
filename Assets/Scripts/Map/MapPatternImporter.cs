using UnityEngine;
using System.Collections;

public class MapPatternImporter
{
    MapData mapData = null;
    MapObjectData mapObjectData = null;

    Vector3 assetSize = Vector3.zero;
    string FileName = "";

    public MapPatternImporter()
    {

    }

    public void ImportPattern(string fileName)
    {
        string path = MapSetting.MAP_PATTERN_FOLDER_NAME + fileName;
        mapData = new MapData(path);
        mapObjectData = new MapObjectData(path + "_Object");

        float heightest = 0f;
        IEnumerator mapItr = mapData.GetMapEnumerator();
        while (mapItr.MoveNext())
        {
            CellData cell = mapItr.Current as CellData;
            heightest = heightest < cell.GetHeightest() ? cell.GetHeightest() : heightest;
        }
        assetSize = new Vector3(mapData.MapSizeX, heightest, mapData.MapSizeZ);

        FileName = fileName;
    }

    public GameObject CreatePatternGameObject()
    {
        if (mapData != null && mapObjectData != null)
        {
            GameObject o = new GameObject();
            o.name = FileName;
            DrawingMap.DrawCells(mapObjectData.GetObjectEnumerator(), (int)assetSize.x, (int)assetSize.z, Vector3.zero, o);
            AssetCellData cellData = o.AddComponent<AssetCellData>();
            cellData.Size = assetSize;
            cellData.Center = new Vector3(0, assetSize.y * 0.5f, 0);
            return o;
        }

        return null;      
    }

    public bool CanBuild(MapData mapData, int targetX, int targetZ, float height, Quaternion rotation)
    {
        if (mapData == null || mapObjectData == null)
        {
            return false;
        }

        int x = (int)assetSize.x;
        int z = (int)assetSize.z;
        Rotate(rotation.eulerAngles.y, ref x, ref z);
        return mapData.CanBuildOnTheMap(targetX, targetZ, height, new Vector3(x, assetSize.y, z));
    }

    public void BuildPattern(MapController mapController, int targetX, int targetZ, float height, Quaternion rotation, bool isObstacle)
    {
        if (mapData == null || mapObjectData == null)
        {
            return;
        }
        
        if (CanBuild(mapController.MapDataCollection, targetX, targetZ, height, rotation))
        {
            int oriXSize = (int)assetSize.x;
            int oriZSize = (int)assetSize.z;

            //旋轉
            int xSize = oriXSize;
            int zSize = oriZSize;
            Rotate(rotation.eulerAngles.y, ref xSize, ref zSize);

            int offsetX = targetX - (int)(xSize * 0.5f);
            int offsetZ = targetZ - (int)(zSize * 0.5f);

            IEnumerator mapObjItr = mapObjectData.GetObjectEnumerator();
            while (mapObjItr.MoveNext())
            {
                MapObject mo = mapObjItr.Current as MapObject;

                //計算x,z的值，要用還未旋轉的值去計算
                int xIndex = -1;
                int zIndex = -1;
                MapUtility.IdToCoordinate(mo.Id, oriXSize, ref xIndex, ref zIndex); //get xIndex, zIndex from pattern coordinate
      
                int len = mo.ObjectDataList.Count;
                for (int i = 0; i < len; i++)
                {
                    MapObject.ObjectData objData = mo.ObjectDataList[i];
                    GameObject obj = Resources.Load(objData.PrefabName) as GameObject;
                    AssetCellData cellData = obj.GetComponent<AssetCellData>();

                    Rotate(rotation.eulerAngles.y, oriXSize, oriZSize, cellData.Size.z, ref xIndex, ref zIndex);
                    xIndex += offsetX; //translate to map coordinate
                    zIndex += offsetZ;

                    if (!isObstacle)
                    {
                        mapController.UpdateCellData(objData.PrefabName, xIndex, zIndex, height + objData.Height, cellData.Size, objData.Rotation * rotation);
                    }
                    else
                    {
                        mapController.UpdateCellData(objData.PrefabName, xIndex, zIndex, height + objData.Height, cellData.Size, objData.Rotation * rotation, UnitType.OBSTACLE);
                    }
                }
            }
        }
    }

    public void Rotate(float angle, ref int xIndex, ref int zIndex)
    {
        float x = xIndex;
        float z = zIndex;

        float newX = x * Mathf.Cos(Mathf.Deg2Rad * angle) - z * Mathf.Sin(Mathf.Deg2Rad * angle);
        float newZ = z * Mathf.Cos(Mathf.Deg2Rad * angle) + x * Mathf.Sin(Mathf.Deg2Rad * angle);
        
        xIndex = Mathf.RoundToInt(Mathf.Abs(newX));
        zIndex = Mathf.RoundToInt(Mathf.Abs(newZ));
    }

    public void Rotate(float angle, int sizeX, int sizeZ, float assetSizeZ, ref int xIndex, ref int zIndex)
    {
        float x = xIndex;
        float z = zIndex;
        int count = Mathf.RoundToInt(angle / 90f);
        int assetSize = ((int)Mathf.Abs(assetSizeZ - 1));
        if (assetSize > 0)
        {
            //只有size >= 2，且是偶數大小，才需要補+1
            assetSize = assetSize % 2;
        }

        int size = sizeX;

        for (int i = 0; i < count; i++)
        {
            //計算旋轉後的座標
            float tmp = x;
            x = z;
            z = -tmp + size - 1;
            z += assetSize;

            //每旋轉90度，它的長、寬會變，所以要修改size
            if (i % 2 == 0)
            {
                size = sizeZ;
            }
            else
            {
                size = sizeX;
            }
        }

        xIndex = Mathf.RoundToInt(x);
        zIndex = Mathf.RoundToInt(z);       
    }
}
