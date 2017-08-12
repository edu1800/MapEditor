using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapMouseEditor
{
    protected int rayCastLayer;
    protected int rayCastMapLayer;
    protected MapController mapController;
    protected MapPatternImporter patternImporter;

    public MapMouseEditor(MapController mapController, MapPatternImporter patternImporter)
    {
        rayCastLayer = 1 | 1 << LayerMask.NameToLayer("Map");
        rayCastMapLayer = 1;

        this.mapController = mapController;
        this.patternImporter = patternImporter;
    }

    public virtual void Action(string prefabName, GameObject cellGo, AssetCellData cellData, DataMode dataMode, bool isObstacle, List<MapIndex> mapIndexList)
    {
        return;
    }

    protected void AddMapIndex(int x, int z, List<MapIndex> mapIndexList)
    {
        mapIndexList.Add(new MapIndex(x, z));
    }

    public virtual void Clear()
    {

    }
}
