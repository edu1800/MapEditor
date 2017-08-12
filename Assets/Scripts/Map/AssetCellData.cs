using UnityEngine;
using System.Collections;

public class AssetCellData : MonoBehaviour
{
    public Vector3 Size;
    public Vector3 Center;
    public MapObject MapObj;
    public MapObject.ObjectData ObjData;

    public void Rotate(float angle)
    {
        float x = Size.x;
        float z = Size.z;

        float newX = x * Mathf.Cos(Mathf.Deg2Rad * angle) - z * Mathf.Sin(Mathf.Deg2Rad * angle);
        float newZ = z * Mathf.Cos(Mathf.Deg2Rad * angle) + x * Mathf.Sin(Mathf.Deg2Rad * angle);

        Size = new Vector3(Mathf.Abs(newX), Size.y, Mathf.Abs(newZ));
    }
}
