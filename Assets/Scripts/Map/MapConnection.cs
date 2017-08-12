using UnityEngine;
using System.Collections;

[System.Serializable]
public class ConnectionRotation
{
    public GameObject Go;
    public int Angle;
}

public class MapConnection : MonoBehaviour
{
    public ConnectionRotation Default;
    public ConnectionRotation Turn; //可以接兩個X and Z
    public ConnectionRotation Forward; //可以接兩 XX or ZZ
    public ConnectionRotation Tform; //T form，可以接三個
    public ConnectionRotation Cross; //可以接四個
    public ConnectionRotation End; //可以接一個
}
