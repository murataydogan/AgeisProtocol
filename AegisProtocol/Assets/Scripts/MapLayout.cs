using UnityEngine;

using System;
using System.Collections.Generic;

[Serializable]
public class Vector2IntData
{
    public int x;
    public int y;

    public Vector2Int ToVector2Int() => new Vector2Int(x, y);
}

[Serializable]
public class HotspotData
{
    public List<Vector2IntData> H1_U_ici;
    public List<Vector2IntData> H2_Kivrim;
    public List<Vector2IntData> H3_Merkez;
    public List<Vector2IntData> H4_Cekirdek_Onu;
}

[Serializable]
public class MapLayout
{
    public Vector2IntData gridSize;
    public Vector2IntData engineerStart;
    public Vector2IntData corePosition;
    public Vector2IntData portalPosition;
    public Vector2IntData redGatePosition;
    public Vector2IntData mergePoint;

    public List<Vector2IntData> iceRocks;
    public HotspotData hotspots;

    public List<Vector2IntData> pathA_Waypoints;
    public List<Vector2IntData> pathB_Waypoints;
    public List<Vector2IntData> commonPath_Waypoints;
}