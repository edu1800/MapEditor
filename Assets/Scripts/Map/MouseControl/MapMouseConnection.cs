using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapMouseConnection : MapMouseEditor
{
    enum MapDir
    {
        UP,
        LEFT,
        DOWN,
        RIGHT
    }

    HashSet<int> buildMapId = new HashSet<int>();
    Dictionary<int, float> totalBuildMapId = new Dictionary<int, float>();
    GameObject dummyParent;
    Object dummyObject;
    bool isPress = false;
    float buildHeight = 0f;

    public MapMouseConnection(MapController mapController, MapPatternImporter patternImporter, Object dummyObject) : base(mapController, patternImporter)
    {
        this.dummyObject = dummyObject;
        dummyParent = new GameObject("Dummy");
    }

    public override void Clear()
    {
     //   totalBuildMapId.Clear();
    }

    //cellGo -> default gameObject
    //prefabName -> Map Connection prefab
    public override void Action(string prefabName, GameObject cellGo, AssetCellData cellData, DataMode dataMode, bool isObstacle, List<MapIndex> mapIndexList)
    {
        mapIndexList.Clear();
        base.Action(prefabName, cellGo, cellData, dataMode, isObstacle, mapIndexList);

        if (Input.GetMouseButton(0))
        {
            if (dataMode == DataMode.ADD)
            {
                int xIndex = -1;
                int zIndex = -1;
                MapUtility.CalCellIndexByMousePosition(mapController.CenterPosition, mapController.MapSizeX, mapController.MapSizeZ, rayCastMapLayer, ref xIndex, ref zIndex);
                if (xIndex >= 0 && zIndex >= 0)
                {
                    int id = MapUtility.CoordinateToId(xIndex, zIndex, mapController.MapSizeX);
                    if (!buildMapId.Contains(id))
                    {
                        if (!isPress)
                        {
                            if (totalBuildMapId.ContainsKey(id))
                            {
                                buildHeight = totalBuildMapId[id];
                            }
                            else
                            {
                                buildHeight = MapUtility.CalCellHeightPosition(mapController.MapDataCollection, xIndex, zIndex, cellData);
                            }
                        }

                        if (mapController.MapDataCollection.CanBuildOnTheMap(xIndex, zIndex, buildHeight, cellData.Size) || totalBuildMapId.ContainsKey(id))
                        {
                            buildMapId.Add(id);

                            //Draw dummy object
                            GameObject dummy = Object.Instantiate(dummyObject) as GameObject;
                            dummy.transform.parent = dummyParent.transform;
                            Vector3 pos = mapController.GetCellPosition(xIndex, zIndex);
                            dummy.transform.position = new Vector3(pos.x, buildHeight, pos.z);

                            isPress = true;
                        }
                    }
                }
            }
            else if (dataMode == DataMode.ERASE)
            {
                GameObject hitObject = MapUtility.GetRayCastMapObjectByMousePosition(rayCastLayer);
                if (hitObject != null)
                {
                    AssetCellData cd = hitObject.GetComponent<AssetCellData>();
                    mapController.EraseCellData(cd);
                    totalBuildMapId.Remove(cd.MapObj.Id);
                }
            }
        }
        else if (Input.GetMouseButtonUp(0) && isPress)
        {
            if (buildMapId.Count == 0)
            {
                return;
            }

            MapConnection mapConn = (Resources.Load(MapSetting.MAP_CONNECT_TILE_FOLDER_NAME + prefabName.GetPathWidthoutExtension()) as GameObject).GetComponent<MapConnection>();
            checkIfNeedReBuild(mapController);
            
            List<MapDir> neighBorList = new List<MapDir>();
            foreach (var item in buildMapId)
            {
                int curXIndex = -1;
                int curZIndex = -1;
                MapUtility.IdToCoordinate(item, mapController.MapSizeX, ref curXIndex, ref curZIndex);

                getNeighborDir(curXIndex, curZIndex, mapController, neighBorList);
                Quaternion rot = Quaternion.identity;
                string p = getPrefabName(neighBorList, cellGo, mapConn, ref rot);

                if (!isObstacle)
                {
                    mapController.UpdateCellData(p, curXIndex, curZIndex, buildHeight, cellData.Size, rot);
                }
                else
                {
                    mapController.UpdateCellData(p, curXIndex, curZIndex, buildHeight, cellData.Size, rot, UnitType.OBSTACLE);
                }
                AddMapIndex(curXIndex, curZIndex, mapIndexList);

                totalBuildMapId[item] = buildHeight;
            }

            buildMapId.Clear();
            isPress = false;
            for (int i = dummyParent.transform.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(dummyParent.transform.GetChild(i).gameObject);
            }
        }
    }

    void checkIfNeedReBuild(MapController mapController)
    {
        int mapSizeX = mapController.MapSizeX;
        int mapSizeZ = mapController.MapSizeZ;

        List<int> newId = new List<int>();
        foreach (var item in buildMapId)
        {
            int curXIndex = -1;
            int curZIndex = -1;
            MapUtility.IdToCoordinate(item, mapSizeX, ref curXIndex, ref curZIndex);

            checkIfNeedReBuild(newId, curXIndex, curZIndex, mapSizeX, mapSizeZ);
            checkIfNeedReBuild(newId, curXIndex, curZIndex + 1, mapSizeX, mapSizeZ);
            checkIfNeedReBuild(newId, curXIndex, curZIndex - 1, mapSizeX, mapSizeZ);
            checkIfNeedReBuild(newId, curXIndex - 1, curZIndex, mapSizeX, mapSizeZ);
            checkIfNeedReBuild(newId, curXIndex + 1, curZIndex, mapSizeX, mapSizeZ);
        }

        int len = newId.Count;
        for (int i = 0; i < len; i++)
        {
            int id = newId[i];
            int curXIndex = -1;
            int curZIndex = -1;
            MapUtility.IdToCoordinate(newId[i], mapSizeX, ref curXIndex, ref curZIndex);
            //mapController.EraseCellData(curXIndex, curZIndex);

            if (totalBuildMapId.ContainsKey(id))
            {
                float h = totalBuildMapId[id];
                MapObject mo = mapController.MapObjectDataCollection.GetMapObjectById(id);
                int objLen = mo.ObjectDataList.Count;
                for (int j = 0; j < objLen; j++)
                {
                    if (mo.ObjectDataList[j].Height == h)
                    {
                        mapController.EraseCellData(mo.ObjectDataList[j].Go.GetComponent<AssetCellData>());
                        break;
                    }
                }
            }
            
            buildMapId.Add(id);
        }
    }

    void checkIfNeedReBuild(List<int> newId, int xIndex, int zIndex, int mapSizeX, int mapSizeZ)
    {
        if (isCurrentMapIndex(xIndex, zIndex, mapSizeX, mapSizeZ))
        {
            int id = MapUtility.CoordinateToId(xIndex, zIndex, mapSizeX);
            if (totalBuildMapId.ContainsKey(id))
            {
                newId.Add(id);
            }
        }
    }

    void getNeighborDir(int xIndex, int zIndex, MapController mapController, List<MapDir> result)
    {
        result.Clear();
        int mapSizeX = mapController.MapSizeX;
        int mapSizeZ = mapController.MapSizeZ;

        if (isCurrentMapIndex(xIndex, zIndex + 1, mapSizeX, mapSizeZ))
        {
            if (isAddNeighborList(xIndex, zIndex + 1, mapSizeX))
            {
                result.Add(MapDir.UP);
            }
        }

        if (isCurrentMapIndex(xIndex - 1, zIndex, mapSizeX, mapSizeZ))
        {
            if (isAddNeighborList(xIndex - 1, zIndex, mapSizeX))
            {
                result.Add(MapDir.LEFT);
            }
        }

        if (isCurrentMapIndex(xIndex, zIndex - 1, mapSizeX, mapSizeZ))
        {
            if (isAddNeighborList(xIndex, zIndex - 1, mapSizeX))
            {
                result.Add(MapDir.DOWN);
            }
        }

        if (isCurrentMapIndex(xIndex + 1, zIndex, mapSizeX, mapSizeZ))
        {
            if (isAddNeighborList(xIndex + 1, zIndex, mapSizeX))
            {
                result.Add(MapDir.RIGHT);
            }
        }
    }

    bool isCurrentMapIndex(int xIndex, int zIndex, int mapSizeX, int mapSizeZ)
    {
        if (xIndex >= 0 && xIndex < mapSizeX && zIndex >= 0 && zIndex < mapSizeZ)
        {
            return true;
        }

        return false;
    }

    bool isAddNeighborList(int xIndex, int zIndex, int mapSizeX)
    {
        int neighborId = MapUtility.CoordinateToId(xIndex, zIndex, mapSizeX);
        if (buildMapId.Contains(neighborId) || totalBuildMapId.ContainsKey(neighborId))
        {
            return true;
        }

        return false;
    }

    string getPrefabName(List<MapDir> mapDirList, GameObject curCellGo, MapConnection mapConnectionData, ref Quaternion rotation)
    {
        string s = "";
        int neighborCount = mapDirList.Count;

        switch (neighborCount)
        {
            case 0:
                s = getAssetPath(mapConnectionData.Default.Go);
                rotation = Quaternion.Euler(0, mapConnectionData.Default.Angle, 0) * curCellGo.transform.rotation;
                break;
            case 1:
                s = getAssetPath(mapConnectionData.End.Go);
                rotation = Quaternion.Euler(0, mapConnectionData.End.Angle, 0) * getRotationByMapDir(mapDirList[0]);
                break;
            case 2:
                {
                    if (Mathf.Abs(mapDirList[0] - mapDirList[1]) == 2)
                    {
                        s = getAssetPath(mapConnectionData.Forward.Go);
                        if (mapDirList[0] == MapDir.UP || mapDirList[0] == MapDir.DOWN)
                        {
                            rotation = Quaternion.Euler(0, mapConnectionData.Forward.Angle, 0) * Quaternion.identity;
                        }
                        else
                        {
                            rotation = Quaternion.Euler(0, mapConnectionData.Forward.Angle, 0) * Quaternion.Euler(0, 90, 0);
                        }
                    }
                    else
                    {
                        s = getAssetPath(mapConnectionData.Turn.Go);
                        rotation = Quaternion.Euler(0, mapConnectionData.Turn.Angle, 0) * getTurnRotationByMapDir(mapDirList[0], mapDirList[1]);
                    }
                }
                break;
            case 3:
                s = getAssetPath(mapConnectionData.Tform.Go);
                rotation = Quaternion.Euler(0, mapConnectionData.Tform.Angle, 0) * getRotationByMapDir(mapDirList[0], mapDirList[1], mapDirList[2]);
                break;
            case 4:
                s = getAssetPath(mapConnectionData.Cross.Go);
                rotation = Quaternion.Euler(0, mapConnectionData.Cross.Angle, 0) * Quaternion.identity;
                break;
            default:
                break;
        }

        return s;
    }

    string getAssetPath(Object o)
    {
#if UNITY_EDITOR
        string s = UnityEditor.AssetDatabase.GetAssetPath(o);
        return s.Substring(s.IndexOf("Assets/Resources/") + 17).GetPathWidthoutExtension();
#else
        return "";
#endif
    }

    Quaternion getRotationByMapDir(MapDir dir)
    {
        Quaternion q = Quaternion.identity;
        switch (dir)
        {
            case MapDir.UP:               
                break;
            case MapDir.DOWN:
                q = Quaternion.Euler(0, 180, 0);
                break;
            case MapDir.LEFT:
                q = Quaternion.Euler(0, 270, 0);
                break;
            case MapDir.RIGHT:
                q = Quaternion.Euler(0, 90, 0);
                break;
            default:
                break;
        }
        return q;
    }

    Quaternion getTurnRotationByMapDir(MapDir dir1, MapDir dir2)
    {
        Quaternion q = Quaternion.identity;

        if (dir1 == MapDir.UP && dir2 == MapDir.LEFT)
        {
            
        }
        else if (dir1 == MapDir.UP && dir2 == MapDir.RIGHT)
        {
            q = Quaternion.Euler(0, 90, 0);
        }
        else if (dir2 == MapDir.DOWN && dir1 == MapDir.LEFT)
        {
            q = Quaternion.Euler(0, 270, 0);
        }
        else if (dir1 == MapDir.DOWN && dir2 == MapDir.RIGHT)
        {
            q = Quaternion.Euler(0, 180, 0);
        }
        return q;
    }

    Quaternion getRotationByMapDir(MapDir dir1, MapDir dir2, MapDir dir3)
    {
        Quaternion q = Quaternion.identity;
        if (dir1 == MapDir.UP)
        {
            if (dir2 == MapDir.LEFT && dir3 == MapDir.RIGHT)
            {
                
            }
            else if (dir2 == MapDir.DOWN && dir3 == MapDir.RIGHT)
            {
                q = Quaternion.Euler(0, 90, 0);
            }
            else if (dir2 == MapDir.LEFT && dir3 == MapDir.DOWN)
            {
                q = Quaternion.Euler(0, 270, 0);
            }
        }
        else if (dir1 == MapDir.LEFT && dir2 == MapDir.DOWN && dir3 == MapDir.RIGHT)
        {
            q = Quaternion.Euler(0, 180, 0);
        }
        return q;
    }
}
