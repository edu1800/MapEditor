using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public enum UnitType
{
    NONE = 1 << 0,
    OBSTACLE = 1 << 1,
    OBJECTS = 1 << 2,
    ENEMY = 1 << 3,
    PLAYER = 1 << 4,
    TOMB = 1 << 5,
}

public class CellData
{
    public class CellHeightData
    {
        public float Height;
        public float Size;

        public CellHeightData(float height, float size)
        {
            Height = height;
            Size = size;
        }
    }

    public int Xpos;
    public int Zpos;
    public List<CellHeightData> Height = new List<CellHeightData>();
    public UnitType Unit = UnitType.NONE;
    public MapData MapDataCollection;
    static public float Tolerance = 0.01f;

    float heightest = 0f;

    public CellData(int xPos, int zPos, MapData mapData)
    {
        Xpos = xPos;
        Zpos = zPos;
        Unit = UnitType.NONE;
        MapDataCollection = mapData;
        heightest = MapDataCollection.MapCenterPosition.y;
    }

    public void RemoveHeight(int index)
    {
        Height.RemoveAt(index);
        UpdateHeightest();
    }

    public void RemoveHeight(float height)
    {
        int len = Height.Count;
        for (int i = 0; i < len; i++)
        {
            if (Mathf.Abs(height - Height[i].Height) < Tolerance)
            {
                RemoveHeight(i);
                break;
            }
        }

        UpdateHeightest();
    }

    public void RemoveAllHeight()
    {
        Height.Clear();
        heightest = MapDataCollection.MapCenterPosition.y;
    }

    public void AddHeight(float val, float size)
    {
        Height.Add(new CellHeightData(val, size));
        Height.Sort(delegate(CellHeightData a, CellHeightData b)
        {
            if (Mathf.Abs(a.Height - b.Height) < Tolerance)
            {
                return 0;
            }
            else if (a.Height > b.Height)
            {
                return -1;
            }

            return 1;
        });

        UpdateHeightest();
    }

    public bool IsCanMove()
    {
        return !IsHaveThisUnitType(UnitType.OBSTACLE) && !IsHaveThisUnitType(UnitType.PLAYER) && !IsHaveThisUnitType(UnitType.ENEMY); 
    }

    public void AddUnit(UnitType type)
    {
        if (type == UnitType.NONE)
        {
            Unit = UnitType.NONE;
        }
        else
        {
            RemoveUnit(UnitType.NONE);
            Unit = Unit | type;
        }
    }

    public void RemoveUnit(UnitType type)
    {
        Unit = Unit & (~type);
    }

    public bool IsHaveThisUnitType(UnitType type)
    {
        return (Unit & type) == type;
    }

    public float GetHeightest()
    {
        return heightest;
    }

    public void UpdateHeightest()
    {
        float height = MapDataCollection.MapCenterPosition.y;
        int len = Height.Count;
        for (int i = 0; i < len; i++)
        {
            if (height < Height[i].Height + Height[i].Size * 0.5f)
            {
                height = Height[i].Height + Height[i].Size * 0.5f;
            }
        }

        heightest = height;
    }

    static public CellData operator+(CellData a, int num)
    {
        int curId = MapUtility.CoordinateToId(a.Xpos, a.Zpos, a.MapDataCollection.MapSizeX);
        int id = curId + num;
        return a.MapDataCollection.GetCellById(id);
    }

    static public CellData operator -(CellData a, int num)
    {
        int curId = MapUtility.CoordinateToId(a.Xpos, a.Zpos, a.MapDataCollection.MapSizeX);
        int id = curId - num;
        return a.MapDataCollection.GetCellById(id);
    }

    static public CellData operator *(CellData a, int num)
    {
        int curId = MapUtility.CoordinateToId(a.Xpos, a.Zpos, a.MapDataCollection.MapSizeX);
        int id = curId + a.MapDataCollection.MapSizeX * num;
        return a.MapDataCollection.GetCellById(id);
    }

    static public CellData operator /(CellData a, int num)
    {
        int curId = MapUtility.CoordinateToId(a.Xpos, a.Zpos, a.MapDataCollection.MapSizeX);
        int id = curId - a.MapDataCollection.MapSizeX * num;
        return a.MapDataCollection.GetCellById(id);
    }

    static public CellData operator ++(CellData a)
    {
        return a + 1;
    }

    static public CellData operator --(CellData a)
    {
        return a - 1;
    }
}

public class MapData
{
    CellData[] map;
    public int MapSizeX
    {
        private set;
        get;
    }

    public int MapSizeZ
    {
        private set;
        get;
    }

    public Vector3 MapCenterPosition
    {
        set;
        get;
    }


    public MapData() : this(64, 64, Vector3.zero)
    {
      
    }

    public MapData(int x, int z, Vector3 centerPosition)
    {
        MapCenterPosition = centerPosition;
        initMap(x, z, centerPosition);
    }

    public MapData(string fileName)
    {
        Load(fileName);
    }

    public void WriteCellData(int id, UnitType unitType, Vector3 assetSize, float height = 0)
    {
        if (id >= 0 && id < map.Length)
        {
            map[id].AddUnit(unitType);
            bool isErase = unitType == UnitType.NONE ? true : false;
            if (!isErase)
            {
                map[id].AddHeight(height, assetSize.y);
            }
           else
            {
                //Only the height is higher han the height of the cell, we need to modify the height of the cell
                map[id].RemoveHeight(height);
            }
        }
    }

    public void WriteCellData(int x, int z, UnitType unitType, int height = 0)
    {
        WriteCellData(MapUtility.CoordinateToId(x, z, MapSizeX), unitType, Vector3.one, height);
    }

    public void WriteCellData(int x, int z, Vector3 assetSize, UnitType unitType, float height = 0)
    {
        int xIndex = -1;
        int zIndex = -1;
        MapUtility.CalCellIndexByCenter(x, z, assetSize, ref xIndex, ref zIndex);
        writeCellDataInternal(xIndex, zIndex, assetSize, unitType, height);
    }

    void writeCellDataInternal(int x, int z, Vector3 collider, UnitType unitType, float height = 0)
    {
        Vector3 numberOfCell = collider.GetCellSize();
        for (int i = 0; i < numberOfCell.x; i++)
        {
            for (int j = 0; j < numberOfCell.z; j++)
            {
                int id = MapUtility.CoordinateToId(x + i, z + j, MapSizeX);
                if (id >= 0 && id < map.Length)
                {
                    WriteCellData(id, unitType, collider, height);
                }
            }
        }
    }

    //x and z are the center of cell
    public bool CanBuildOnTheMap(int x, int z, float height, Vector3 collider)
    {
        int xIndex = -1;
        int zIndex = -1;
        MapUtility.CalCellIndexByCenter(x, z, collider, ref xIndex, ref zIndex);
        return canBuildOnTheMapInternal(xIndex, zIndex, height, collider);
    }

    bool canBuildOnTheMapInternal(int x, int z, float height, Vector3 collider)
    {
        Vector3 numberOfCell = collider.GetCellSize();
        for (int i = 0; i < numberOfCell.x; i++)
        {
            for (int j = 0; j < numberOfCell.z; j++)
            {
                int xIndex = x + i;
                int zIndex = z + j;
                if (xIndex < 0 || xIndex >= MapSizeX || zIndex < 0 || zIndex >= MapSizeZ)
                {
                    return false;
                }
                int id = MapUtility.CoordinateToId(xIndex, zIndex, MapSizeX);
                if (map[id].Unit != UnitType.NONE && map[id].GetHeightest() > height)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void Save(string fileName)
    {
        using (StreamWriter sw = new StreamWriter(fileName))
        {
            writeMapSize(sw);
            int len = map.Length;
            for (int i = 0; i < len; i++)
            {
                CellData cell = map[i];
                if (checkIfNeedSave(cell))
                {
                    writeCellData(sw, cell);
                }
            }
        }
    }

    /// <summary>
    /// xMin、zMin、xMax、zMax are the bounding volumes of the map
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="xMin"></param>
    /// <param name="xMax"></param>
    /// <param name="zMin"></param>
    /// <param name="zMax"></param>
    public void SavePattern(string fileName, int xMin, int xMax, int zMin, int zMax)
    {
        using (StreamWriter sw = new StreamWriter(fileName))
        {
            int patternSizeX = xMax - xMin + 1;
            int patternSizeZ = zMax - zMin + 1;
            writeMapSize(sw, patternSizeX, patternSizeZ, MapCenterPosition);
            for (int i = xMin; i <= xMax; i++)
            {
                for (int j = zMin; j <= zMax; j++)
                {
                    //Cur map id to new map id
                    int xIndex = i - xMin;
                    CellData cell = GetCell(i, j);
                    if (checkIfNeedSave(cell))
                    {
                        int zIndex = j - zMin;
                        writeCellData(sw, GetCell(i, j), MapUtility.CoordinateToId(xIndex, zIndex, patternSizeX));
                    }
                }
            }
        }
    }

    /// <summary>
    /// mindId and maxId are the bounding volumes of the map
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="minId"></param>
    /// <param name="maxId"></param>
    public void SavePattern(string fileName, int minId, int maxId)
    {
        int xMin = -1, xMax = -1;
        int zMin = -1, zMax = -1;
        MapUtility.IdToCoordinate(minId, MapSizeX, ref xMin, ref zMin);
        MapUtility.IdToCoordinate(maxId, MapSizeX, ref xMax, ref zMax);
        SavePattern(fileName, xMin, xMax, zMin, zMax);    
    }

    void writeMapSize(StreamWriter sw)
    {
        writeMapSize(sw, MapSizeX, MapSizeZ, MapCenterPosition);
    }

    void writeMapSize(StreamWriter sw, int xLen, int zLen, Vector3 centerPosition)
    {
        string s = xLen + "$" + zLen + "$" + centerPosition.y + "$" + centerPosition.x + "$" + centerPosition.z;
        sw.WriteLine(s);
    }

    void writePatternBounding(StreamWriter sw, int xMin, int xMax, int zMin, int zMax)
    {
        string s = xMin + "$" + xMax + "$" + zMin + "$" + zMax;
        sw.WriteLine(s);
    }

    void writeCellData(StreamWriter sw, CellData cell, int id = -1)
    {
        string s = id >= 0 ? id.ToString() : MapUtility.CoordinateToId(cell.Xpos, cell.Zpos, MapSizeX).ToString();

        int len = cell.Height.Count;
        for (int i = 0; i < len; i++)
        {
            s += "$" + cell.Height[i].Height + "$" + cell.Height[i].Size;
        }

        s += "$" + cell.Unit;
        sw.WriteLine(s);
    }

    private bool checkIfNeedSave(CellData cell)
    {
        return cell.GetHeightest() > 0 || cell.IsHaveThisUnitType(UnitType.OBSTACLE) || cell.IsHaveThisUnitType(UnitType.OBJECTS);
    }

    public void Load(string fileName)
    {
        TextAsset textAsset = Resources.Load(fileName) as TextAsset;
        if (textAsset != null)
        {
            StringReader sr = new StringReader(textAsset.text);
            string firstLine = sr.ReadLine();
            if (firstLine != null)
            {
                loadCellSize(firstLine);
                string s;
                while ((s = sr.ReadLine()) != null)
                {
                    loadCellData(s);
                } 
            }
        }
    }

    void loadCellSize(string s)
    {
        string[] data = s.Split('$');
        Vector3 center = Vector3.zero;
        center.y = data.Length <= 2 ? 0f : float.Parse(data[2]);
        if (data.Length > 3)
        {
            center.x = float.Parse(data[3]);
            center.z = float.Parse(data[4]);
        }
        initMap(int.Parse(data[0]), int.Parse(data[1]), center);
    }

    void loadCellData(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return;
        }

        string[] data = s.Split('$');
        if (data.Length < 2)
        {
            return;
        }

        CellData cell = GetCellById(int.Parse(data[0]));

        int heightDataCount = (data.Length - 1) / 2;
        for (int i = 0; i < heightDataCount; i++)
        {
            float h = float.Parse(data[i * 2 + 1]);
            float sz = float.Parse(data[i * 2 + 2]);
            cell.AddHeight(h, sz);
        }

        if (((data.Length - 1) % 2) == 1)
        {
            bool canMove = true;
            if (bool.TryParse(data[data.Length - 1], out canMove))
            {
                if (canMove)
                {
                    cell.AddUnit(UnitType.OBJECTS);
                }
                else
                {
                    cell.AddUnit(UnitType.OBSTACLE);
                }
            }
            else
            {
                cell.Unit = (UnitType)System.Enum.Parse(typeof(UnitType), data[data.Length - 1]);
            }
        }
        else
        {
            cell.AddUnit(UnitType.OBJECTS);
        }
    }

    public CellData GetCell(int x, int z)
    {
        return GetCellById(MapUtility.CoordinateToId(x, z, MapSizeX));
    }

    public CellData GetCellById(int id)
    {
        if (id < 0 || id >= map.Length)
        {
            return null;
        }

        return map[id];
    }

    public CellData this[int x, int z]
    {
        get
        {
            return GetCell(x, z);
        }
        set
        {
            WriteCellData(x, z, new Vector3(1, 0, 1), value.Unit);
            int len = value.Height.Count;
            CellData cur = GetCell(x, z);
            cur.RemoveAllHeight();
            for (int i = 0; i < len; i++)
            {
                cur.AddHeight(value.Height[i].Height, value.Height[i].Size);
            }
        }
    }

    public IEnumerator GetMapEnumerator()
    {
        return map.GetEnumerator();
    } 

    void initMap(int x, int z, Vector3 centerPosition)
    {
        MapSizeX = x;
        MapSizeZ = z;
        map = new CellData[x * z];
        initMap(centerPosition);
    }

    void initMap(Vector3 centerPosition)
    {
        MapCenterPosition = centerPosition;
        int len = map.Length;
        for (int i = 0; i < len; i++)
        {
            int xIndex = -1;
            int zIndex = -1;
            MapUtility.IdToCoordinate(i, MapSizeX, ref xIndex, ref zIndex);
            map[i] = new CellData(xIndex, zIndex, this);
        }
    }
}

public struct MapIndex
{
    public int x;
    public int z;

    
    public MapIndex(int x, int z)
    {
        this.x = x;
        this.z = z;       
    }

    public static bool operator ==(MapIndex rhs, MapIndex lhs)
    {
        return rhs.x == lhs.x && rhs.z == lhs.z;
    }

    public static bool operator !=(MapIndex rhs, MapIndex lhs)
    {
        return !(rhs == lhs);
    }

    public static MapIndex InvalidValue
    {
        get
        {
            return new MapIndex(-1, -1);
        }
    }
}
