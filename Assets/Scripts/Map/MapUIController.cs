using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public enum MouseMode
{
    CLICK = 0,
    HOLD,
    SQUARE,
    CONTINUOUS,
    RANDIMIZE
}

public enum DataMode
{
    ADD = 0,
    ERASE,
    CAN_NOT_MOVE,
    CAN_MOVE,
    ADD_PLAYER,
    ERASE_PLAYER,
    ADD_ENEMY,
    ERASE_ENEMY
}

public class MapUIController : MonoBehaviour
{
    private enum EditMode
    {
        EDITING = 0,
        UI
    }

    public Dropdown CategoryDropdown;
    public Dropdown CellPrefabDropdown;
    public Dropdown AddOrErseDropdown;
    public Dropdown SceneDropdown;
    public Toggle IsObstacleToggle;
    public GameObject CreateSceneUI;

    MapController mapController;
    GameObject grid;

    Dictionary<string, List<string>> categoryList = new Dictionary<string, List<string>>();
    List<string> igorePathList = new List<string>();

    string curCellPrefabName;
    GameObject curCellGo;
    AssetCellData curCellData;
    int curCellIndexX = -1;
    int curCellIndexZ = 1;
    bool isPrefabOrPattern = true;
    GameObject curPersonGo;

    MapPatternImporter mapPatternImporter = new MapPatternImporter();

    //Debug GameObject
    public GameObject canBuildCubePrefab;
    GameObject canBuildCube = null;
    public GameObject canNotBuildCubePrefab;
    GameObject canNotBuildCube = null;
    public GameObject SquareBuildCubePrefab;
    public GameObject CannotMovablePrefab;
    public GameObject PlayerPrefab;
    public GameObject EnemyPrefab;

    GameObject canNotMoveInst;
    Vector3 farAwayPos = new Vector3(-1000, 0, -1000);

    MouseMode mouseMode = MouseMode.CLICK;
    MapMouseEditor[] mouseControl = null;
    List<MapIndex> mouseClickMapIndex = new List<MapIndex>();

    MapLayersUI mapLayersUI = null;

    EditMode editMode_ = EditMode.UI;
    EditMode editMode
    {
        set
        {
            editMode_ = value;
            switch (editMode_)
            {
                case EditMode.EDITING:
                    GetComponent<MapCameraController>().enabled = true;
                    break;
                case EditMode.UI:
                    GetComponent<MapCameraController>().enabled = false;
                    break;
                default:
                    break;
            }
        }

        get
        {
            return editMode_;
        }
    }

    DataMode dataMode_;
    DataMode dataMode
    {
        set
        {
            dataMode_ = value;

            for (int i = canNotMoveInst.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(canNotMoveInst.transform.GetChild(i).gameObject);
            }

            DestroyCurCellGo();

            switch (dataMode)
            {
                case DataMode.ADD:
                    if (!string.IsNullOrEmpty(curCellPrefabName))
                    {
                        InstantiateCurCellGo();
                    }
                    break;
                case DataMode.ERASE:
                    break;
                case DataMode.CAN_MOVE:
                    drawCannotMoveDebugCubes();
                    break;
                case DataMode.CAN_NOT_MOVE:
                    drawCannotMoveDebugCubes();
                    break;
                case DataMode.ADD_PLAYER:
                    drawPlayersAndEnemies();
                    InstantiateCurPersonGo(true);
                    break;
                case DataMode.ERASE_PLAYER:
                    drawPlayersAndEnemies();
                    break;
                case DataMode.ADD_ENEMY:
                    drawPlayersAndEnemies();
                    InstantiateCurPersonGo(false);
                    break;
                case DataMode.ERASE_ENEMY:
                    drawPlayersAndEnemies();
                    break;
                default:
                    break;
            }
        }
        get
        {
            return dataMode_;
        }
    }
    int rayCastLayer;
    int playerPosCount = 0;
    int enemyPosCount = 0;
    string resourcePath;
    GameObject sceneObjectsParent = null;

	// Use this for initialization
	void Start ()
    {
        resourcePath = Application.dataPath + "/Resources/";
        mapController = GetComponent<MapController>();
        mapLayersUI = GetComponent<MapLayersUI>();
        rayCastLayer = 1;
        addIgorePathList();

        canBuildCube = Instantiate(canBuildCubePrefab, farAwayPos, Quaternion.identity) as GameObject;
        canNotBuildCube = Instantiate(canNotBuildCubePrefab, farAwayPos, Quaternion.identity) as GameObject;
        canNotMoveInst = new GameObject("CanNotMoveDebugCube");
        canNotMoveInst.transform.position = Vector3.zero;
        canNotMoveInst.transform.rotation = Quaternion.identity;

        updateCategoryList();      

        mouseControl = new MapMouseEditor[5];
        mouseControl[0] = new MapMouseClick(mapController, mapPatternImporter);
        mouseControl[1] = new MapMouseHold(mapController, mapPatternImporter);
        mouseControl[2] = new MapMouseSquare(mapController, mapPatternImporter, SquareBuildCubePrefab);
        mouseControl[3] = new MapMouseConnection(mapController, mapPatternImporter, SquareBuildCubePrefab);
        mouseControl[4] = new MapMouseRandomizer(mapController, mapPatternImporter, SquareBuildCubePrefab);

        string[] sceneName = getSceneNames();
        SceneDropdown.options.Clear();
        foreach (var item in sceneName)
        {
            SceneDropdown.options.Add(new Dropdown.OptionData(item));
        }

        DrawGrid();

        OnNeedGameObjectActive(CreateSceneUI);
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (editMode_ == EditMode.EDITING)
        {
            rotateCurObject();
            ModifyMapObjectProperty();
            updateCurCellPosition();
            updateCurPersonPosition();
            KeyEvent();
        }
    }

    void addIgorePathList()
    {
        igorePathList.Clear();
        igorePathList.Add("GridUnit");
    }

    bool isContainIgnorePrefab(string testString)
    {
        int len = igorePathList.Count;
        for (int i = 0; i < len; i++)
        {
            if (testString.Contains(igorePathList[i]))
            {
                return true;
            }
        }

        return false;
    }

    void updateCategoryList()
    {
        categoryList.Clear();

        if (mouseMode == MouseMode.CONTINUOUS)
        {
            string[] paths = Directory.GetFiles(resourcePath + MapSetting.MAP_CONNECT_TILE_FOLDER_NAME, "*.prefab", SearchOption.AllDirectories);
            addCategory(paths, MapSetting.MAP_CONNECT_TILE_FOLDER_NAME, "");
        }
        else if (mouseMode == MouseMode.RANDIMIZE)
        {
            string[] paths = Directory.GetFiles(resourcePath + MapSetting.MAP_RANDOM_TILE_FOLDER_NAME, "*.prefab", SearchOption.AllDirectories);
            addCategory(paths, MapSetting.MAP_RANDOM_TILE_FOLDER_NAME, "");
        }
        else 
        {
            string[] paths = Directory.GetFiles(resourcePath + MapSetting.MAP_REFAB_FOLDER_NAME, "*.prefab", SearchOption.AllDirectories);
            addCategory(paths, MapSetting.MAP_REFAB_FOLDER_NAME, "");

            //add patern
            paths = Directory.GetFiles(resourcePath + MapSetting.MAP_PATTERN_FOLDER_NAME, "*Pattern.txt", SearchOption.AllDirectories);
            addCategory(paths, MapSetting.MAP_PATTERN_FOLDER_NAME, "Pattern_");
        }

        updateCategoryUI();
    }

    void addCategory(string[] paths, string substr, string prefixName)
    {
        int len = paths.Length;
        for (int i = 0; i < len; i++)
        {
            string path = paths[i];
            path = path.Replace('\\', '/');
            //Remove Assets/Resources/
            path = path.Substring(path.IndexOf(substr) + substr.Length);
            if (!isContainIgnorePrefab(path))
            {
                int slash = path.IndexOf('/');
                string categoryName = slash < 0 ? (prefixName + "Other") : (prefixName + path.Substring(0, slash));
                if (!categoryList.ContainsKey(categoryName))
                {
                    categoryList.Add(categoryName, new List<string>());
                }

                categoryList[categoryName].Add(path);
            }
        }
    }

    void updateCategoryUI()
    {
        CategoryDropdown.options.Clear();
        foreach (var item in categoryList)
        {
            CategoryDropdown.options.Add(new Dropdown.OptionData(item.Key));
        }

        CategoryDropdown.value = 0;
        CategoryDropdown.captionText.text = CategoryDropdown.options[CategoryDropdown.value].text;
        updateCellPrefabUI();
    }

    void updateCellPrefabUI()
    {
        CellPrefabDropdown.options.Clear();
        string selectCategoryName = CategoryDropdown.options[CategoryDropdown.value].text;
        List<string> prefabList = categoryList[selectCategoryName];

        int len = prefabList.Count;
        for (int i = 0; i < len; i++)
        {
            CellPrefabDropdown.options.Add(new Dropdown.OptionData(prefabList[i]));
        }

        CellPrefabDropdown.value = 0;
        CellPrefabDropdown.captionText.text = CellPrefabDropdown.options[CellPrefabDropdown.value].text;
        OnCellPrefabDropDownChanged();
    }

    public void OnCategoryDropDownChanged()
    {
        if (CategoryDropdown.value >= 0)
        {
            isPrefabOrPattern = !CategoryDropdown.captionText.text.Contains("Pattern_");
            updateCellPrefabUI();
        }
    }

    public void OnCellPrefabDropDownChanged()
    {
        if (curCellGo != null)
        {
            Destroy(curCellGo);
        }

        curCellPrefabName = CellPrefabDropdown.captionText.text;
        InstantiateCurCellGo();
    }

    public void OnDataModeDropDownChanged(Dropdown dataModeDropDown)
    {      
        dataMode = (DataMode)(dataModeDropDown.value * 2);
        AddOrErseDropdown.value = 0;
        AddOrErseDropdown.captionText.text = AddOrErseDropdown.options[0].text;
    }

    public void OnAddOrEraseDropDownChanged()
    {
        int val = AddOrErseDropdown.value;
        int curMode = (int)dataMode;
        dataMode = (DataMode)(curMode / 2 * 2 + val);
    }


    void InstantiateCurCellGo()
    {
        if (mouseMode == MouseMode.CONTINUOUS)
        {
            GameObject connData = Resources.Load(MapSetting.MAP_CONNECT_TILE_FOLDER_NAME + curCellPrefabName.GetPathWidthoutExtension()) as GameObject;
            curCellGo = Instantiate(connData.GetComponent<MapConnection>().Default.Go);
        }
        else if (mouseMode == MouseMode.RANDIMIZE)
        {
            GameObject ranData = Resources.Load(MapSetting.MAP_RANDOM_TILE_FOLDER_NAME + curCellPrefabName.GetPathWidthoutExtension()) as GameObject;
            curCellGo = Instantiate(ranData.GetComponent<MapRandomList>().GoList[0]);
        }
        else
        {
            if (isPrefabOrPattern)
            {
                curCellGo = Instantiate(Resources.Load(MapSetting.MAP_REFAB_FOLDER_NAME + curCellPrefabName.GetPathWidthoutExtension())) as GameObject;
            }
            else
            {
                string patternName = Path.GetFileNameWithoutExtension(curCellPrefabName);
                mapPatternImporter.ImportPattern(patternName);
                curCellGo = mapPatternImporter.CreatePatternGameObject();
            }
        }

        curCellData = curCellGo.GetComponent<AssetCellData>();
        changeLayersRecursively(curCellGo.transform, MapSetting.IGNORE_RAY_CAST_LAYER_MASK);

        canBuildCube.transform.rotation = curCellGo.transform.rotation;
        canNotBuildCube.transform.rotation = curCellGo.transform.rotation;
    }

    void DestroyCurCellGo()
    {
        if (curCellGo != null)
        {
            Destroy(curCellGo);
        }

        canBuildCube.transform.position = farAwayPos;
        canNotBuildCube.transform.position = farAwayPos;

        if (curPersonGo != null)
        {
            Destroy(curPersonGo);
        }
    }

    void InstantiateCurPersonGo(bool playerOrEnemy)
    {
        if (playerOrEnemy)
        {
            curPersonGo = Instantiate(PlayerPrefab);
            curPersonGo.transform.Find("Num").GetComponent<TextMesh>().text = playerPosCount.ToString();
        }
        else
        {
            curPersonGo = Instantiate(EnemyPrefab);
            curPersonGo.transform.Find("Num").GetComponent<TextMesh>().text = enemyPosCount.ToString();
        }

        curPersonGo.layer = MapSetting.IGNORE_RAY_CAST_LAYER_MASK;
    }

    void drawCannotMoveDebugCubes()
    {
        IEnumerator cellItr = mapController.MapDataCollection.GetMapEnumerator();
        while (cellItr.MoveNext())
        {
            CellData cell = cellItr.Current as CellData;
            if (!cell.IsCanMove())
            {
                drawCannotMoveDebugCube(cell);
            }
        }
    }

    void drawCannotMoveDebugCubes(List<MapIndex> mapIndexList)
    {
        int len = mapIndexList.Count;
        for (int i = 0; i < len; i++)
        {
            drawCannotMoveDebugCube(mapIndexList[i].x, mapIndexList[i].z);
        }
    }

    void drawCannotMoveDebugCube(CellData cell)
    {
        GameObject debugGo = Instantiate(CannotMovablePrefab);
        Vector3 pos = mapController.GetCellPosition(cell.Xpos, cell.Zpos);
        debugGo.transform.parent = canNotMoveInst.transform;
        debugGo.transform.localScale = new Vector3(MapSetting.CELL_UNIT_SIZE, cell.GetHeightest() * 0.5f, MapSetting.CELL_UNIT_SIZE);
        debugGo.transform.position = new Vector3(pos.x, pos.y * 0.5f + 0.1f, pos.z);
        CellId cellId = debugGo.AddComponent<CellId>();
        cellId.CellX = cell.Xpos;
        cellId.CellZ = cell.Zpos;
    }

    void drawCannotMoveDebugCube(int xIndex, int zIndex)
    {
        CellData cell = mapController.MapDataCollection[xIndex, zIndex];
        if (cell != null && !cell.IsCanMove())
        {
            drawCannotMoveDebugCube(cell);
        }
    }

    void destroyCannotMoveDebugCubes(List<MapIndex> mapIndexList)
    {
        int len = mapIndexList.Count;
        for (int i = 0; i < len; i++)
        {
            destroyCannotMoveDebugCube(mapIndexList[i].x, mapIndexList[i].z);
        }
    }

    void destroyCannotMoveDebugCube(int xIndex, int zIndex)
    {
        CellId[] cellsId = canNotMoveInst.GetComponentsInChildren<CellId>();
        CellId first = System.Array.Find(cellsId, c => c.CellX == xIndex && c.CellZ == zIndex);
        if (first != null)
        {
            Destroy(first.gameObject);
        }
    }

    void drawCanOrCanNotBuildCube(int xIndex, int zIndex)
    {
        bool curCellCanBuild = mapController.MapDataCollection.CanBuildOnTheMap(xIndex, zIndex, curCellGo.transform.position.y, curCellData.Size);
        if (curCellCanBuild)
        {
            canBuildCube.transform.position = curCellGo.transform.position + curCellData.Center + new Vector3(0, 0.1f, 0f);
            canBuildCube.transform.localScale = curCellData.Size;
            canNotBuildCube.transform.position = farAwayPos;
        }
        else
        {
            canBuildCube.transform.position = farAwayPos;
            canNotBuildCube.transform.position = curCellGo.transform.position + curCellData.Center + new Vector3(0, 0.1f, 0f);
            canNotBuildCube.transform.localScale = curCellData.Size;
        }
    }

    void drawPlayersAndEnemies()
    {
        IEnumerator playerItr = MapPositioningManager.instance.GetPositioningIterator(MapPositioningType.PLAYER);
        while (playerItr.MoveNext())
        {
            MapPositioningData data = playerItr.Current as MapPositioningData;
            drawPlayer(data.Order, data.Index.x, data.Index.z, data.Rotation);
        }

        IEnumerator enemyItr = MapPositioningManager.instance.GetPositioningIterator(MapPositioningType.ENEMY);
        while (enemyItr.MoveNext())
        {
            MapPositioningData data = enemyItr.Current as MapPositioningData;
            drawEnemy(data.Order, data.Index.x, data.Index.z, data.Rotation);
        }
    }

    void addPlayers(List<MapIndex> mapIndexList, Quaternion rotation)
    {
        int len = mapIndexList.Count;
        for (int i = 0; i < len; i++)
        {
            int x = mapIndexList[i].x;
            int z = mapIndexList[i].z;
            if (MapPositioningManager.instance.GetPositioning(MapPositioningType.PLAYER, x, z) == null)
            {
                MapPositioningManager.instance.AddPositioning(MapPositioningType.PLAYER, playerPosCount, x, z, rotation);
                drawPlayer(playerPosCount, x, z, rotation);
                ++playerPosCount;
            }
        }
    }

    void drawPlayer(int order, int x, int z, Quaternion rotation)
    {
        drawPeople(PlayerPrefab, order, rotation, x, z, true);
    }
    
    void addEnemies(List<MapIndex> mapIndexList, Quaternion rotation)
    {
        int len = mapIndexList.Count;
        for (int i = 0; i < len; i++)
        {
            int x = mapIndexList[i].x;
            int z = mapIndexList[i].z;
            if (MapPositioningManager.instance.GetPositioning(MapPositioningType.ENEMY, x, z) == null)
            {
                MapPositioningManager.instance.AddPositioning(MapPositioningType.ENEMY, enemyPosCount, x, z, rotation); 
                drawEnemy(enemyPosCount, x, z, rotation);
                ++enemyPosCount;
            }
        }
    }

    void drawEnemy(int order, int x, int z, Quaternion rotation)
    {
        drawPeople(EnemyPrefab, order, rotation, x, z, false);
    }

    void drawPeople(Object prefab, int order, Quaternion rotation, int x, int z, bool playerOrEnemy)
    {
        GameObject debugGo = Instantiate(prefab) as GameObject;
        debugGo.transform.Find("Num").GetComponent<TextMesh>().text = order.ToString();
        Vector3 pos = mapController.GetCellPosition(x, z) + new Vector3(0, 1, 0);
        debugGo.transform.parent = canNotMoveInst.transform;
        debugGo.transform.position = pos;
        debugGo.transform.rotation = rotation;
        CellId cellId = debugGo.AddComponent<CellId>();
        cellId.CellX = x;
        cellId.CellZ = z;
        cellId.PlayerOrEnemy = playerOrEnemy;
    }

    void destroyPeople(List<MapIndex> mapIndexList, bool playerOrEnemy)
    {
        int len = mapIndexList.Count;
        for (int i = 0; i < len; i++)
        {
            destroyPerson(mapIndexList[i].x, mapIndexList[i].z, playerOrEnemy);
        }
    }

    void destroyPerson(int xIndex, int zIndex, bool playerOrEnemy)
    {
        bool isRemove = playerOrEnemy ? MapPositioningManager.instance.RemovePositioning(MapPositioningType.PLAYER, xIndex, zIndex) : 
                                        MapPositioningManager.instance.RemovePositioning(MapPositioningType.ENEMY, xIndex, zIndex);
        if (isRemove)
        {
            CellId[] cellsId = canNotMoveInst.GetComponentsInChildren<CellId>();
            CellId first = System.Array.Find(cellsId, c => c.CellX == xIndex && c.CellZ == zIndex && c.PlayerOrEnemy == playerOrEnemy);
            if (first != null)
            {
                Destroy(first.gameObject);
            }
        }
    }

    public void OnMouseModeDropDownChanged(GameObject dropDown)
    {
        MouseMode preMode = mouseMode;
        mouseControl[(int)preMode].Clear();
        mouseMode = (MouseMode)dropDown.GetComponent<Dropdown>().value;

        if (preMode == MouseMode.CONTINUOUS || mouseMode == MouseMode.CONTINUOUS)
        {
            //更新list
            updateCategoryList();
            ((MapMouseConnection)mouseControl[(int)MouseMode.CONTINUOUS]).Clear();  
        }
        else if (preMode == MouseMode.RANDIMIZE || mouseMode == MouseMode.RANDIMIZE)
        {
            updateCategoryList();
        }
    }

    void ModifyMapObjectProperty()
    {
        if (mapController.MapDataCollection != null && mapController.MapObjectDataCollection != null)
        {
            GameObject go = dataMode < DataMode.ADD_PLAYER ? curCellGo : curPersonGo;
            if (isPrefabOrPattern)
            {
                mouseControl[(int)mouseMode].Action(curCellPrefabName, go, curCellData, dataMode, IsObstacleToggle.isOn, mouseClickMapIndex);
            }
            else
            {
                mouseControl[(int)mouseMode].Action("", go, curCellData, dataMode, IsObstacleToggle.isOn, mouseClickMapIndex);
            }
            
            if (mouseClickMapIndex.Count > 0)
            {
                switch (dataMode)
                {
                    case DataMode.ADD:
                        break;
                    case DataMode.ERASE:
                        break;
                    case DataMode.CAN_MOVE:
                        destroyCannotMoveDebugCubes(mouseClickMapIndex);
                        break;
                    case DataMode.CAN_NOT_MOVE:
                        drawCannotMoveDebugCubes(mouseClickMapIndex);
                        break;
                    case DataMode.ADD_PLAYER:
                        addPlayers(mouseClickMapIndex, curPersonGo.transform.rotation);
                        curPersonGo.transform.Find("Num").GetComponent<TextMesh>().text = playerPosCount.ToString();
                        break;;
                    case DataMode.ERASE_PLAYER:
                        destroyPeople(mouseClickMapIndex, true);
                        break;
                    case DataMode.ADD_ENEMY:
                        addEnemies(mouseClickMapIndex, curPersonGo.transform.rotation);
                        curPersonGo.transform.Find("Num").GetComponent<TextMesh>().text = enemyPosCount.ToString();
                        break;
                    case DataMode.ERASE_ENEMY:
                        destroyPeople(mouseClickMapIndex, false);
                        break;
                    default:
                        break;
                }
            }
        }
    }

    public void OnNeedGameObjectActiveOrDeactive(GameObject go)
    {
        go.SetActive(!go.activeSelf);
    }

    public void OnNeedGameObjectActive(GameObject go)
    {
        editMode = EditMode.UI;
        go.SetActive(true);
    }

    public void OnNeedGameObjectDeactive(GameObject go)
    {
        go.SetActive(false);
        editMode = EditMode.EDITING;
    }

    public void OnNewScene(GameObject go)
    {
        string fileName = go.transform.Find("InputField").GetComponent<InputField>().text;
        int mapSizeX = int.Parse(go.transform.Find("SizeXInputField").GetComponent<InputField>().text);
        int mapSizeZ = int.Parse(go.transform.Find("SizeZInputField").GetComponent<InputField>().text);

        if (!string.IsNullOrEmpty(fileName) && mapSizeX > 0 && mapSizeZ > 0)
        {
            mapController.NewMap(fileName, mapSizeX, mapSizeZ);
        }

        DrawGrid();
        OnNeedGameObjectDeactive(go);
    }

    public void OnSaveMapAs(GameObject go)
    {
        string fileName = go.transform.Find("InputField").GetComponent<InputField>().text;
        if (!string.IsNullOrEmpty(fileName))
        {
            mapController.MapFileName = fileName;
            SaveMap();
        }

        OnNeedGameObjectDeactive(go);
    }

    void updateCurCellPosition(bool isForceUpdate = false)
    {
        if (curCellGo != null)
        {
            if (updateGoPosition(curCellGo, isForceUpdate))
            {
                if (mouseMode == MouseMode.CONTINUOUS)
                {
                    Vector3 pos = curCellGo.transform.position;
                    curCellGo.transform.position = new Vector3(pos.x, curCellData.Size.y * 0.5f, pos.z);
                }
                drawCanOrCanNotBuildCube(curCellIndexX, curCellIndexZ);
            }     
        }
    }

    void updateCurPersonPosition(bool isForceUpdate = false)
    {
        if (curPersonGo != null)
        {
            if (updateGoPosition(curPersonGo, isForceUpdate))
            {
                Vector3 pos = curPersonGo.transform.position;
                curPersonGo.transform.position = new Vector3(pos.x, pos.y + 1, pos.z);
            }
        }
    }

    bool updateGoPosition(GameObject go, bool isForceUpdate)
    {
        int xIndex = -1;
        int zIndex = -1;
        MapUtility.CalCellIndexByMousePosition(mapController.CenterPosition, mapController.MapSizeX, mapController.MapSizeZ,
                                               rayCastLayer, ref xIndex, ref zIndex);

        if ((xIndex >= 0 && zIndex >= 0 && xIndex < mapController.MapSizeX && zIndex < mapController.MapSizeZ &&
            (isForceUpdate || xIndex != curCellIndexX || zIndex != curCellIndexZ)))
        {
            go.transform.position = MapUtility.CalCellPosition(mapController.MapDataCollection,
                                                                      MapUtility.CalTopLeftCellPosition(mapController.CenterPosition, mapController.MapSizeX, mapController.MapSizeZ),
                                                                      xIndex, zIndex, curCellData.Size);
            go.transform.position = MapUtility.CalCellHeightPosition(mapController.MapDataCollection, xIndex, zIndex, curCellData, go.transform.position);
            curCellIndexX = xIndex;
            curCellIndexZ = zIndex;
            return true;
        }

        return false;
    }

    void rotateCurObject()
    {
        if (Input.GetMouseButtonDown(1))
        {
            if (curCellGo != null)
            {
                curCellGo.transform.Rotate(Vector3.up, 90);
                curCellData.Rotate(90);

                updateCurCellPosition(true);
            }

            if (curPersonGo != null)
            {
                curPersonGo.transform.Rotate(Vector3.up, 90);
                updateCurPersonPosition(true);
            }
        }
    }

    public void SaveMap()
    {
        if (!string.IsNullOrEmpty(mapController.MapFileName))
        {
            SaveMap(mapController.MapFileName);
            mapLayersUI.SaveLayers(mapController.MapFileName);
        }
        else
        {
            SaveMap("Sample");
            mapLayersUI.SaveLayers("Sample");
        }
    }


    public void SaveMap(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        string path = resourcePath + MapSetting.MAP_DATA_FOLDER_NAME + fileName;
        if (mapController.MapDataCollection != null)
        {
            mapController.MapDataCollection.Save(path + ".txt");
        }

        if (mapController.MapObjectDataCollection != null)
        {
            mapController.MapObjectDataCollection.Save(path + "_Object.txt");
        }

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
    public void ReLoadMap()
    {
        if (!string.IsNullOrEmpty(mapController.MapFileName))
        {
            mapController.ReLoadMap();
            mapLayersUI.LoadLayers(mapController.MapFileName);

            DrawGrid();
        }
    }

    void LoadMap(string fileName)
    {
        mapController.LoadMap(fileName);
        mapLayersUI.LoadLayers(fileName);
        DrawGrid();
    }

    public void ExportPattern()
    {
        if (mapController.MapDataCollection == null || mapController.MapObjectDataCollection == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(mapController.MapFileName))
        {
            int xMin = mapController.MapSizeX;
            int xMax = -1;
            int zMin = mapController.MapSizeZ;
            int zMax = -1;
            findMapBB(ref xMin, ref xMax, ref zMin, ref zMax);

            string path = resourcePath + MapSetting.MAP_PATTERN_FOLDER_NAME + mapController.MapFileName;
            mapController.MapDataCollection.SavePattern(path + "_Pattern.txt", xMin, xMax, zMin, zMax);
            mapController.MapObjectDataCollection.SavePattern(path + "_Pattern_Object.txt", xMin, xMax, zMin, zMax, mapController.MapSizeX);
            SaveMap();

            updateCategoryList();
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }
    }

    public void OnSavePositioningClicked(GameObject go)
    {
        string fileName = go.transform.Find("InputField").GetComponent<InputField>().text;
        if (!string.IsNullOrEmpty(fileName))
        {
            MapPositioningManager.instance.Save(fileName);
        }

        OnNeedGameObjectDeactive(go);
    }

    public void OnLoadPositioningClicked(GameObject go)
    {
        string fileName = go.transform.Find("InputField").GetComponent<InputField>().text;
        if (!string.IsNullOrEmpty(fileName))
        {
            MapPositioningManager.instance.Load(fileName);
            playerPosCount = MapPositioningManager.instance.GetPositioningCount(MapPositioningType.PLAYER);
            enemyPosCount = MapPositioningManager.instance.GetPositioningCount(MapPositioningType.ENEMY);
        }
        OnNeedGameObjectDeactive(go);
    }

    public void OnStartLoadMap()
    {
        string fileName = CreateSceneUI.transform.Find("InputFileName/InputField").GetComponent<InputField>().text;
        string mapFilePath = resourcePath + MapSetting.MAP_DATA_FOLDER_NAME + fileName + ".txt";
        int mapSizeX = -1;
        int.TryParse(CreateSceneUI.transform.Find("InputFileName/SizeXInputField").GetComponent<InputField>().text, out mapSizeX);
        int mapSizeZ = -1;
        int.TryParse(CreateSceneUI.transform.Find("InputFileName/SizeZInputField").GetComponent<InputField>().text, out mapSizeZ);
        
        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        bool mapFileExist = File.Exists(mapFilePath);
        if (!mapFileExist && (mapSizeX < 0 || mapSizeZ < 0))
        {
            return;
        }

        if (mapFileExist)
        {
            LoadMap(fileName);
        }
        else
        {
            mapController.NewMap(fileName, mapSizeX, mapSizeZ);
            mapLayersUI.CreateDefaultLayer();
            DrawGrid();
        }

        OnNeedGameObjectDeactive(CreateSceneUI);
    }

    public void OnUIEditing()
    {
        editMode = EditMode.UI;
    }

    public void OnMapEditing()
    {
        editMode = EditMode.EDITING;
    }

    void DrawGrid()
    {
        if(grid != null)
        {
            Destroy(grid);
        }
        grid = DrawingMap.DrawGrid(mapController.MapSizeX, mapController.MapSizeZ, mapController.CenterPosition, Quaternion.identity);
    }

    void findMapBB(ref int xMin, ref int xMax, ref int zMin, ref int zMax)
    {
        xMin = mapController.MapSizeX;
        xMax = -1;
        zMin = mapController.MapSizeZ;
        zMax = -1;

        //Find bb
        IEnumerator cellDataItr = mapController.MapDataCollection.GetMapEnumerator();
        while (cellDataItr.MoveNext())
        {
            CellData cell = cellDataItr.Current as CellData;
            if (cell.Unit != UnitType.NONE)
            {
                if (cell.Xpos < xMin)
                {
                    xMin = cell.Xpos;
                }

                if (cell.Xpos > xMax)
                {
                    xMax = cell.Xpos;
                }

                if (cell.Zpos < zMin)
                {
                    zMin = cell.Zpos;
                }

                if (cell.Zpos > zMax)
                {
                    zMax = cell.Zpos;
                }
            }
        }
    }

    void changeLayersRecursively(Transform trans, int layer)
    {
        trans.gameObject.layer = layer;
        foreach (Transform child in trans)
        {
            changeLayersRecursively(child, layer);
        }
    }

    void KeyEvent()
    {
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            Vector3 pos = grid.transform.position;
            grid.transform.position = new Vector3(pos.x, pos.y + 0.1f, pos.z);
        }
        else if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            Vector3 pos = grid.transform.position;
            grid.transform.position = new Vector3(pos.x, pos.y - 0.1f, pos.z);
        }
    }

    string[] getSceneNames()
    {
        List<string> temp = new List<string>();
#if UNITY_EDITOR
        foreach (UnityEditor.EditorBuildSettingsScene S in UnityEditor.EditorBuildSettings.scenes)
        {
            if (S.enabled)
            {
                string name = S.path.Substring(S.path.LastIndexOf('/') + 1);
                name = name.Substring(0, name.Length - 6);
                temp.Add(name);
            }
        }
#endif
        return temp.ToArray();
    }

    public void OnSceneDropdownChanged(Dropdown dropdown)
    {
        if (sceneObjectsParent != null)
        {
            Destroy(sceneObjectsParent);
        }

        string sceneName = dropdown.options[dropdown.value].text;
        StartCoroutine(loadLevelAsync(sceneName));
    }

    IEnumerator loadLevelAsync(string sceneName)
    {
        GameObject[] curSceneObjects = FindObjectsOfType<GameObject>();
        yield return null;

        AsyncOperation asyncOperation = Application.LoadLevelAdditiveAsync(sceneName);
        while (!asyncOperation.isDone)
        {
            yield return null;
        }

        yield return null;

        IEnumerable<GameObject> nowObjects = (from o in FindObjectsOfType<GameObject>().Except(curSceneObjects) select o.transform.root.gameObject).Distinct();
        sceneObjectsParent = new GameObject(sceneName);
        sceneObjectsParent.transform.position = Vector3.zero;
        sceneObjectsParent.transform.rotation = Quaternion.identity;

        Material material = new Material(Shader.Find("Transparent/Diffuse"));

        foreach (var item in nowObjects)
        {
            item.transform.parent = sceneObjectsParent.transform;
            Renderer[] rends = item.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rends.Length; i++)
            {
                rends[i].material = material;
                Color c = rends[i].material.color;
                rends[i].material.color = new Color(c.r, c.g, c.b, 0.5f);
            }

            Light[] lights = item.GetComponentsInChildren<Light>();
            for (int i = 0; i < lights.Length; i++)
            {
                lights[i].enabled = false;
            }
            
            ChangeLayers(item, MapSetting.IGNORE_RAY_CAST_LAYER_MASK);
        }
    }

    public void ChangeLayers(GameObject go, string name)
    {
        ChangeLayers(go, LayerMask.NameToLayer(name));
    }

    public void ChangeLayers(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
        {
            ChangeLayers(child.gameObject, layer);
        }
    }

    public void OnMapCenterButtonClicked(GameObject go)
    {
        go.SetActive(true);
        OnNeedGameObjectActive(go);
    }

    public void OnMapCenterModified(GameObject go)
    {
        float x = float.Parse(go.transform.Find("XInputField").GetComponent<InputField>().text);
        float y = float.Parse(go.transform.Find("YInputField").GetComponent<InputField>().text);
        float z = float.Parse(go.transform.Find("ZInputField").GetComponent<InputField>().text);

        mapController.ModifyMapCenterPosition(new Vector3(x, y, z));
        OnNeedGameObjectDeactive(go);
        DrawGrid();
    }
}
