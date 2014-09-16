using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BenTools.Mathematics;
using BenTools.Data;

public class IslandTileEdge 
{
    public HashSet<IslandTile>       neighbors = new HashSet<IslandTile>();
    public VoronoiEdge               edge;

    public IslandTileCorner cornerA;
    public IslandTileCorner cornerB;

    public IslandTileEdge (VoronoiEdge e)
    {
        edge = e;

        // Corner Index are assumed to be populated from IslandTile
        cornerA = IslandTileCorner.Index[e.VVertexA];
        cornerB = IslandTileCorner.Index[e.VVertexB];   
    }
}