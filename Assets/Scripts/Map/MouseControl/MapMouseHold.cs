using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public class MapMouseHold : MapMouseEditor
{
    HashSet<int> buildMapId = new HashSet<int>();
    public MapMouseHold(MapController mapController, MapPatternImporter patternImporter) : base(mapController, patternImporter)
    {

    }

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
                MapUtility.CalCellIndexByMousePosition(mapController.CenterPosition, mapController.MapSizeX, mapController.MapSizeZ, rayCastLayer, ref xIndex, ref zIndex);
                if (xIndex >= 0 && zIndex >= 0)
                {
                    int id = MapUtility.CoordinateToId(xIndex, zIndex, mapController.MapSizeX);
                    if (!buildMapId.Contains(id))
                    {
                        float yPos = MapUtility.CalCellHeightPosition(mapController.MapDataCollection, xIndex, zIndex, cellData);
                        if (mapController.MapDataCollection.CanBuildOnTheMap(xIndex, zIndex, yPos, cellData.Size))
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

                            //Add id that the cell can not be builded
                            int leftX = xIndex - ((int)Mathf.Abs(cellData.Size.x)) / 2;
                            int rightX = xIndex + ((int)Mathf.Abs(cellData.Size.x - 1)) / 2;
                            int downZ = zIndex - ((int)Mathf.Abs(cellData.Size.z)) / 2;
                            int upZ = zIndex + ((int)Mathf.Abs(cellData.Size.z - 1)) / 2;

                            for (int i = leftX; i <= rightX; i++)
                            {
                                for (int j = downZ; j <= upZ; j++)
                                {
                                    buildMapId.Add(MapUtility.CoordinateToId(i, j, mapController.MapSizeX));
                                }
                            }
                        }

                        AddMapIndex(xIndex, zIndex, mapIndexList);
                    }
                }
            }
            else if (dataMode == DataMode.ERASE)
            {
                GameObject hitObject = MapUtility.GetRayCastMapObjectByMousePosition(rayCastLayer);
                if (hitObject != null)
                {
                    mapController.EraseCellData(hitObject.GetComponent<AssetCellData>());
                }
            }
            else if (dataMode == DataMode.CAN_MOVE || (dataMode == DataMode.CAN_NOT_MOVE))
            {
                int xIndex = -1;
                int zIndex = -1;
                MapUtility.CalCellIndexByMousePosition(mapController.CenterPosition, mapController.MapSizeX, mapController.MapSizeZ, rayCastMapLayer, ref xIndex, ref zIndex);
                if (xIndex >= 0 && zIndex >= 0)
                {
                    if (mapController.SetCellMovable(xIndex, zIndex, dataMode == DataMode.CAN_MOVE ? true : false))
                    {
                        AddMapIndex(xIndex, zIndex, mapIndexList);
                    }
                }
            }
            else if (dataMode == DataMode.ADD_PLAYER || dataMode == DataMode.ERASE_PLAYER || dataMode == DataMode.ADD_ENEMY || dataMode == DataMode.ERASE_ENEMY)
            {
                int xIndex = -1;
                int zIndex = -1;
                MapUtility.CalCellIndexByMousePosition(mapController.CenterPosition, mapController.MapSizeX, mapController.MapSizeZ, rayCastMapLayer, ref xIndex, ref zIndex);
                if (xIndex >= 0 && zIndex >= 0)
                {
                    AddMapIndex(xIndex, zIndex, mapIndexList);
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            buildMapId.Clear();
        }
    }
}
