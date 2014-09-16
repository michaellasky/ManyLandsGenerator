using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BenTools.Mathematics;
using BenTools.Data;

public class IslandTileCorner
{
    public HashSet<IslandTileCorner> adjacent;   // Corners connected by edges
    public HashSet<VoronoiEdge>      protrudes;  // Edges touching this corner
    public HashSet<IslandTile>       touches;    // Tiles connected by this corner
    public Vector                    position;
    public float                     elevation;
    public int                       inlandDistance = 1;
    public float                     moistureFlowRate = 0.95f;

    public IslandTileCorner DownSlopeCorner
    {
        get 
        { 
            if (downSlopeCorner == null)
            {
                downSlopeCorner = GetDownSlopeCorner(this);
            }

            return downSlopeCorner;
        }
    }

    public float Elevation
    {
        get
        {
            if (IsWater) { return 0; }
            return elevation;    
        }
    }

    public float Moisture 
    {
        get 
        {
            if (!calcedMoisture) { moisture = CalcMoisture(); }
            return moisture;
        }
    }

    public Vector3 ElevatedPosition
    {
        get 
        { 
            return Island.ElevateVec(IslandTile.VToV3(position), elevation); 
        }
    }

    public bool IsWater 
    {
        get 
        {
            if (!calcedWater) { CalcWater(); }
            return isWater || elevation <= 0;    
        }
    }

    public bool IsShore
    {
        get
        {
            if (!calcedShore) { CalcShore(); }
            return isShore;
        }
    }
    
    public static Dictionary<Vector, IslandTileCorner> 
        Index = new Dictionary<Vector, IslandTileCorner>(); 
    
    private IslandTileCorner downSlopeCorner;

    private bool  isWater        = false;
    private bool  calcedWater    = false;
    private bool  calcedMoisture = false;
    private bool  isShore        = false;
    private bool  calcedShore    = false;
    private float moisture;
    
    public IslandTileCorner (Vector p)
    {
        adjacent        = new HashSet<IslandTileCorner>();
        protrudes       = new HashSet<VoronoiEdge>();
        touches         = new HashSet<IslandTile>();
        position        = p;
        elevation       = Mathf.Infinity;

        Index[p] = this;
    }

    public static IslandTileCorner GetDownSlopeCorner(IslandTileCorner corner)
    {
        float lowestY = corner.ElevatedPosition.y;
        IslandTileCorner downCorner = corner; 

        foreach (IslandTileCorner c in corner.adjacent)
        {
            float cornerY = c.ElevatedPosition.y;
            if (cornerY < lowestY) 
            { 
                downCorner = c;
                lowestY    = cornerY; 
            }
        }

        return downCorner;
    }

    private void CalcWater ()
    {
        isWater = false;
        foreach (IslandTile t in touches) 
        { 
            if (t.IsWater) { isWater = true; } 
        }
        calcedWater = true;
    }

    private void CalcShore ()
    {
        bool hasWater = false;
        bool hasLand  = false;
        foreach (IslandTile t in touches)
        {
            if (t.IsWater) { hasWater = true; }
            else           { hasLand  = true; }
        }

        isShore     = hasWater && hasLand;
        calcedShore = true;
    }

    private float CalcMoisture ()
    {
        moisture = 0;
        foreach (IslandTile t in touches)
        {
            moisture += (t.BaseMoisture / t.corners.Count);
        }

        foreach (IslandTileCorner c in adjacent)
        {
            if (c.DownSlopeCorner == this)
            {
                moisture += c.moisture * moistureFlowRate;
            }
        }

        calcedMoisture = true;
        return moisture;
    }
}