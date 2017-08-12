using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class MapMouseSquare : MapMouseEditor
{
    protected bool isPress = false;
    protected Vector3 mousePos = Vector2.zero;
    protected GameObject squareUI;

    public MapMouseSquare(MapController mapController, MapPatternImporter patternImporter, GameObject squareBuildCubePrefab) : base(mapController, patternImporter)
    {
        squareUI = GameObject.Instantiate(squareBuildCubePrefab) as GameObject;
        squareUI.SetActive(false);
    }

    public override void Action(string prefabName, GameObject cellGo, AssetCellData cellData, DataMode dataMode, bool isObstacle, List<MapIndex> mapIndexList)
    {
        mapIndexList.Clear();
        base.Action(prefabName, cellGo, cellData, dataMode, isObstacle, mapIndexList);

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
                MapUtility.CalCellIndexByMousePosition(curMousePos, mapController.CenterPosition, mapController.MapSizeX, mapController.MapSizeZ, rayCastLayer, ref curXIndex, ref curZIndex);

                int lastXIndex = -1;
                int lastZIndex = -1;
                MapUtility.CalCellIndexByMousePosition(mousePos, mapController.CenterPosition, mapController.MapSizeX, mapController.MapSizeZ, rayCastLayer, ref lastXIndex, ref lastZIndex);

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
                                float yPos = MapUtility.CalCellHeightPosition(mapController.MapDataCollection, xIndex, zIndex, cellData);

                                if (dataMode == DataMode.ADD)
                                {
                                    if (!string.IsNullOrEmpty(prefabName))
                                    {
                                        if (!isObstacle)
                                        {
                                            mapController.UpdateCellData(MapSetting.MAP_REFAB_FOLDER_NAME + prefabName.GetPathWidthoutExtension(), xIndex, zIndex, yPos,
                                                                cellData.Size, cellGo.transform.rotation);
                                        }
                                        else
                                        {
                                            mapController.UpdateCellData(MapSetting.MAP_REFAB_FOLDER_NAME + prefabName.GetPathWidthoutExtension(), xIndex, zIndex, yPos,
                                                                cellData.Size, cellGo.transform.rotation, UnitType.OBSTACLE);
                                        }
                                    }
                                    else
                                    {
                                        patternImporter.BuildPattern(mapController, xIndex, zIndex, yPos, cellGo.transform.rotation, isObstacle);
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

    protected void Swap<T>(ref T a, ref T b)
    {
        T tmp = a;
        a = b;
        b = tmp;
    }

    protected void DrawQuad(MapController mapController, AssetCellData cellData)
    {
        if (isPress)
        {
            Vector3 curMousePos = Input.mousePosition;

            //Get cell index
            int curXIndex = -1;
            int curZIndex = -1;
            MapUtility.CalCellIndexByMousePosition(curMousePos, mapController.CenterPosition, mapController.MapSizeX, mapController.MapSizeZ, rayCastMapLayer, ref curXIndex, ref curZIndex);

            int lastXIndex = -1;
            int lastZIndex = -1;
            MapUtility.CalCellIndexByMousePosition(mousePos, mapController.CenterPosition, mapController.MapSizeX, mapController.MapSizeZ, rayCastMapLayer, ref lastXIndex, ref lastZIndex);

            //Get cell position
            Vector3 curPos = mapController.GetCellPosition(curXIndex, curZIndex);
            Vector3 lastPos = mapController.GetCellPosition(lastXIndex, lastZIndex);

            //Calulate squre ui's position
            Vector3 centerPos = (curPos + lastPos) * 0.5f;
            centerPos = new Vector3(centerPos.x, cellData.Size.y * 0.5f, centerPos.z + 0.1f);
            Vector3 len = (curPos - lastPos).AbsValue() + Vector3.one;
            len = new Vector3(len.x, cellData.Size.y, len.z);

            squareUI.transform.position = centerPos;
            squareUI.transform.localScale = len;
            squareUI.SetActive(true);
        }
        else
        {
            squareUI.SetActive(false);
        }
    }

    protected bool isInTheMap(int xIndex, int zIndex, MapController mapController)
    {
        return xIndex >= 0 && xIndex < mapController.MapSizeX && zIndex >= 0 && zIndex < mapController.MapSizeZ;
    }

    public override void Clear()
    {
        base.Clear();
        squareUI.SetActive(false);
    }
}
