using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapMouseRandomizer : MapMouseSquare
{
    public MapMouseRandomizer(MapController mapController, MapPatternImporter patternImporter, GameObject squareBuildCubePrefab) : base(mapController, patternImporter, squareBuildCubePrefab)
    {
    }

    public override void Action(string prefabName, GameObject cellGo, AssetCellData cellData, DataMode dataMode, bool isObstacle, List<MapIndex> mapIndexList)
    {
        mapIndexList.Clear();

        if (string.IsNullOrEmpty(prefabName))
        {
            return;
        }

        if (dataMode == DataMode.ERASE)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isPress = true;
            mousePos = Input.mousePosition;
        }

        if (isPress && Input.GetMouseButtonUp(0))
        {
            isPress = false;
            Vector3 curMousePos = Input.mousePosition;

            if (curMousePos != mousePos)
            {
                //Get cell index
                int curXIndex = -1;
                int curZIndex = -1;
                MapUtility.CalCellIndexByMousePosition(curMousePos, mapController.CenterPosition, mapController.MapSizeX, mapController.MapSizeZ, rayCastMapLayer, ref curXIndex, ref curZIndex);

                int lastXIndex = -1;
                int lastZIndex = -1;
                MapUtility.CalCellIndexByMousePosition(mousePos, mapController.CenterPosition, mapController.MapSizeX, mapController.MapSizeZ, rayCastMapLayer, ref lastXIndex, ref lastZIndex);

                if (curXIndex < lastXIndex)
                {
                    Swap(ref curXIndex, ref lastXIndex);
                }

                if (curZIndex < lastZIndex)
                {
                    Swap(ref curZIndex, ref lastZIndex);
                }

                if (isInTheMap(curXIndex, curZIndex, mapController) && isInTheMap(lastXIndex, lastZIndex, mapController))
                {
                    List<GameObject> randomList = (Resources.Load(MapSetting.MAP_RANDOM_TILE_FOLDER_NAME + prefabName.GetPathWidthoutExtension()) as GameObject).GetComponent<MapRandomList>().GoList;
                    int randomCount = randomList.Count;
 
                    //calculate the number of the cells that we can build     
                    int xCount = (int)((curXIndex - lastXIndex + 1) / cellData.Size.x);
                    int zCount = (int)((curZIndex - lastZIndex + 1) / cellData.Size.z);
                    if (xCount > 0 && zCount > 0)
                    {
                        //because the cell chosen by the mouse does not point to the center of the cell, so we need to calclute the index to the centero of the cell 
                        int startXIndex = lastXIndex + ((int)Mathf.Abs(cellData.Size.x - 1));
                        int startZIndex = lastZIndex + ((int)Mathf.Abs(cellData.Size.z - 1));

                        for (int i = 0; i < xCount; i++)
                        {
                            int xIndex = startXIndex + i * (int)cellData.Size.x;
                            for (int j = 0; j < zCount; j++)
                            {
                                int zIndex = startZIndex + j * (int)cellData.Size.z;

                                int goIndex = Random.Range(0, randomCount);
                                AssetCellData cd = randomList[goIndex].GetComponent<AssetCellData>();
                                float yPos = MapUtility.CalCellHeightPosition(mapController.MapDataCollection, xIndex, zIndex, cd);

                                if (dataMode == DataMode.ADD)
                                {
                                    string p = getAssetPath(randomList[goIndex]);
                                    if (!isObstacle)
                                    {
                                        mapController.UpdateCellData(p, xIndex, zIndex, yPos, cd.Size, Quaternion.Euler(0f, Random.Range(0, 3) * 90f, 0f));
                                    }
                                    else
                                    {
                                        mapController.UpdateCellData(p, xIndex, zIndex, yPos, cd.Size, Quaternion.Euler(0f, Random.Range(0, 3) * 90f, 0f), UnitType.OBSTACLE);
                                    }
                                    AddMapIndex(xIndex, zIndex, mapIndexList);
                                }
                                else if (dataMode == DataMode.CAN_MOVE || dataMode == DataMode.CAN_NOT_MOVE)
                                {
                                    bool canMove = dataMode == DataMode.CAN_MOVE ? true : false;
                                    if (mapController.SetCellMovable(xIndex, zIndex, canMove))
                                    {
                                        AddMapIndex(xIndex, zIndex, mapIndexList);
                                    }
                                }
                                else if (dataMode == DataMode.ADD_PLAYER || dataMode == DataMode.ERASE_PLAYER || dataMode == DataMode.ADD_ENEMY || dataMode == DataMode.ERASE_ENEMY)
                                {
                                    AddMapIndex(xIndex, zIndex, mapIndexList);
                                }
                            }
                        }
                    }
                }
            }
        }

        DrawQuad(mapController, cellData);
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

    public override void Clear()
    {
        base.Clear();
        squareUI.SetActive(false);
    }
}
