using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BenTools.Mathematics;
using BenTools.Data;

public class IslandTile 
{
    public Vector center;
    public HashSet<IslandTileEdge>   edges      = new HashSet<IslandTileEdge>();
    public HashSet<IslandTileCorner> corners    = new HashSet<IslandTileCorner>();
    public HashSet<Vector>           neighbors  = new HashSet<Vector>();
    public bool                      forceWater = false;
    public int                       islandId   = 99;
    public Mesh                      tileMesh;
    public float                     tileElevation = Mathf.Infinity;
    
    public float Elevation   { get { return CalcElevation(); } }
    public bool  Lonely      { get { return NoEdges && NoNeighbors; } } 
    public bool  NoEdges     { get { return edges.Count == 0; } }
    public bool  NoNeighbors { get { return neighbors.Count == 0; } }
    public bool  IsEdgeTile  { get { return Lonely || !EdgesAreClosed(); }}

    public bool  ForceWater
    {
        get { return forceWater; }
        set 
        {
            forceWater = value;
            
            if (forceWater)
            {
                foreach(IslandTileCorner c in corners) { c.elevation = 0; }    
            } 
        }
    }

    public Vector3 Normal 
    {
        get
        {
            if (normal == Vector3.zero) { normal = CalcNormal(); }

            return normal;
        }
    }

    public float InlandDistance 
    {
        get 
        {
            if (corners.Count == 0) { return 1; }

            int sum = 0;
            foreach (IslandTileCorner c in corners)
            {
                sum += c.inlandDistance;
            }

            return (float) sum / (float) corners.Count;
        }
    }

    public float BaseMoisture 
    { 
        get 
        { 
            return baseMoisture / InlandDistance; 
        } 
    }

    public float Moisture 
    { 
        get 
        { 
            if (moisture == 0)
            {
                moisture = CalcMoisture();    
            }
            return moisture;
        }
    }

    public Vector3 ElevatedCenter
    {
        get { return Island.ElevateVec(VToV3(center), Elevation); }
    }

    public bool IsWater
    {
        get 
        {   
            if (!calcedWater) { CalcWater(); }
            return isWater;
        }
    }

    private Island      island;
    private Vector3     normal          = Vector3.zero;
    private bool        isWater;
    private bool        calcedWater     = false;
    private float       baseMoisture    = 1f; 
    private float       moisture;

    public IslandTile (Vector c, VoronoiGraph g, Island isl)
    {
        island = isl;
        center = c;

        AddEdges(g);
    }

    public float CalcElevation ()
    {
        float sum = 0;
        foreach (IslandTileCorner c in corners) { sum += c.elevation; }

        return sum / corners.Count;
    }

    public Vector3 RandomFacePosition ()
    {
        IslandTileEdge[]   es = new IslandTileEdge[edges.Count];

        edges.CopyTo(es);
        
        IslandTileEdge rEdge = es[Random.Range(0, es.Length)];
        
        Vector3 v1 = rEdge.cornerA.ElevatedPosition - ElevatedCenter;
        Vector3 v2 = rEdge.cornerB.ElevatedPosition - ElevatedCenter;

        float r1 = Random.value;
        float r2 = Random.value;

        if (r1 + r2 > 1f)
        {
            r1 = 1 - r1;
            r2 = 1 - r2;
        }

        return ElevatedCenter + r1 * v1 + r2 * v2;
    }

    private void CalcWater ()
    {
        bool neighborIsEdge = false;

        foreach (Vector n in neighbors)
        {
            if (island.Tiles[VToV3(n)].IsEdgeTile) { neighborIsEdge = true; }
        }
        
        float nv = island.islandNoise.GetValue((float) center[0] / island.Scale, 
                                               (float) center[1] / island.Scale, 
                                               (float) 0);
        
        bool  noiseValIsLow = nv < island.WaterHeight;    

        calcedWater = true;
        isWater = forceWater || neighborIsEdge || IsEdgeTile || noiseValIsLow;
    }

    private Vector3 CalcNormal ()
    {
        Vector3 norm = Vector3.zero;
        IslandTileCorner[] cArray = new IslandTileCorner[corners.Count];
        corners.CopyTo(cArray);

        for (int i = 0; i < cArray.Length; i++)
        {
            Vector3 current = cArray[i].ElevatedPosition;
            Vector3 next    = cArray[(i + 1) % cArray.Length].ElevatedPosition;

            norm.x += (current.y - next.y) * (current.z + next.z);
            norm.y += (current.z - next.z) * (current.x + next.x);
            norm.z += (current.x - next.x) * (current.y + next.y);
        }
        
        return norm.normalized;
    }

    private float CalcMoisture ()
    {
        float sum = 0;
        foreach (IslandTileCorner c in corners)
        {
            sum += c.Moisture;
        }

        return sum / (float) corners.Count;
    }

    private bool EdgeBelongsToTile (VoronoiEdge e)
    { 
        return (e.LeftData == center) || (e.RightData == center);
    }

    private void AddEdges (VoronoiGraph g)
    {
        foreach (VoronoiEdge e in g.Edges)
        {
            if (EdgeBelongsToTile(e)) { AddEdge(e); }
        }
    }

    private void AddEdge (VoronoiEdge e)
    {
        bool isInf = (e.VVertexA == Fortune.VVInfinite) || 
                     (e.VVertexB == Fortune.VVInfinite);

        if (isInf) { return; }

        AddCorners(e);
        edges.Add(new IslandTileEdge(e));

        if (e.LeftData == center) { neighbors.Add(e.RightData); }
        else                      { neighbors.Add(e.LeftData);  }
    }

    private void AddCorners (VoronoiEdge e)
    {
        IslandTileCorner cA = AddCorner(e.VVertexA);
        IslandTileCorner cB = AddCorner(e.VVertexB);

        cA.protrudes.Add(e);
        cA.adjacent.Add(cB);
        cA.touches.Add(this);

        cB.protrudes.Add(e);
        cB.adjacent.Add(cA);
        cB.touches.Add(this);        
    }

    private IslandTileCorner AddCorner (Vector v)
    {
        IslandTileCorner c;
        Dictionary<Vector, IslandTileCorner> cIdx = IslandTileCorner.Index;

        if (cIdx.ContainsKey(v)) { c = IslandTileCorner.Index[v]; }
        else                     { c = new IslandTileCorner(v);   }

        corners.Add(c);

        return c;
    }

    private bool EdgesAreClosed ()
    {
        foreach(IslandTileEdge e in edges)
        {
            if (NumConnectedEdges(e.edge, edges) != 3) { return false; }
        }
        
        return true;
    }

    private int NumConnectedEdges(VoronoiEdge edge, HashSet<IslandTileEdge> es)
    {
        int numConnectedEdges = 0;

        foreach(IslandTileEdge e in es)
        {
            if (e.edge.VVertexA == edge.VVertexA || 
                e.edge.VVertexA == edge.VVertexB ||
                e.edge.VVertexB == edge.VVertexA || 
                e.edge.VVertexB == edge.VVertexB)
            {
                numConnectedEdges++;
            }
        }
        
        return numConnectedEdges;
    }

    public static Vector3 VToV3(Vector v)
    {
        return new Vector3((float) v[0], 0, (float) v[1]);
    }

    public static Vector V3ToV(Vector3 v)
    {
        return new Vector(v.x, v.y, v.z);
    }
}
