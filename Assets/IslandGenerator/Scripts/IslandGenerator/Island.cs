using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using CoherentNoise;
using CoherentNoise.Generation;
using CoherentNoise.Generation.Displacement;
using CoherentNoise.Generation.Fractal;
using CoherentNoise.Generation.Modification;
using CoherentNoise.Generation.Patterns;
using BenTools.Mathematics;
using Random = UnityEngine.Random;

public class Island : MonoBehaviour 
{
    public  bool                                Generated   
    { 
        get { return islandGenerated; } 
    } 
    
    public  Dictionary<Vector3, IslandTile>     Tiles       
    { 
        get { return islandTiles; } 
    }
    
    public  float                               WaterHeight 
    { 
        get { return waterHeight; } 
    }

    public  Mesh                                IslandMesh  
    { 
        get { return islandMesh; } 
    }

    public  float                               Scale       
    { 
        get { return scale; } 
    }

    public  int                                 voronoiCells    = 512;
    public  int                                 seed            = 1;

    public  static List<Vector3[]>              vGridLineGizmos;
   
    public  Generator                           islandNoise;

    public  Vector3                             islandPosition;

    [SerializeField]
    private float                               scale = 1f;

    [SerializeField]
    private bool                                islandGenerated = false;

    [SerializeField]
    private float                               elevationStep = 0.0005f;

    [SerializeField]
    private int                                 relaxationIterations = 3;

    [SerializeField]
    private float                               waterHeight = 0.06f;

    [SerializeField]
    private World                               world;

    private Dictionary<Vector3, IslandTile>     islandTiles;
    private List<Vector>                        cellVectors; 
    
    private Mesh                                islandMesh;
    private VoronoiGraph                        vGraph;
    private Dictionary<int, List<IslandTile>>   subIslands;
    private MeshFilter                          meshFilter;

    void Awake ()
    {
        Instantiate();
        GetComponents();        
        renderer.material.SetFloat ("_Scale", scale);
        FindWorld();

    }

    void FindWorld ()
    {
        world = GameObject.Find("World").GetComponent<World>();
        World.worldSeed = seed;
        world.GenerateWorld();
    }

    void Instantiate ()
    {
        subIslands   = new Dictionary<int, List<IslandTile>>();
    }

    void GetComponents ()
    {
        meshFilter      = gameObject.GetComponent<MeshFilter>();
    }

    void Reset ()
    {
        islandGenerated = false;
        islandTiles     = null;
        vGraph          = null;
        subIslands      = null;
        cellVectors     = null;
    }

    void OnDrawGizmos ()
    {
        if (vGridLineGizmos != null)
        {
            foreach(Vector3[] line in vGridLineGizmos)
            {
                Gizmos.DrawLine(line[0], line[1]);
            }
        }
    }
    public void Regenerate (bool random)
    {
        if (random) { seed = (int) System.DateTime.Now.Ticks; }
        Regenerate ();
    }

    public void Regenerate ()
    {
        Reset();
        Awake();
        Generate();
        SetupGizmos();
    }

    void SetupGizmos ()
    {
        vGridLineGizmos = new List<Vector3[]>();
        
        foreach (VoronoiEdge e in vGraph.Edges)
        {
            Vector3[] edgeLine = new Vector3[2] 
            {
                IslandTile.VToV3(e.VVertexA),
                IslandTile.VToV3(e.VVertexB)
            };
            
            vGridLineGizmos.Add(edgeLine);
        }
    }
    
    public void Generate()
    {
        GenerateGraph();
        GenerateTiles();        
        MarkSubIslands();
        CullExtraneousIslands();
        SetCornerElevations();
        GenerateIslandMesh();
        SetVegetation();

        islandGenerated = true; 
    }

    public void SetVegetation ()
    {
        IslandVegetation[] vegetation = gameObject.GetComponents<IslandVegetation>();

        foreach (IslandVegetation vegInstance in vegetation) 
        {
            vegInstance.SpawnVegetation();
        }
    }

    public void GenerateGraph ()
    {
        Random.seed = seed;

        islandNoise = world.GetNoiseAt(islandPosition);
        cellVectors = RandomVectorList(voronoiCells);
        vGraph      = GenerateRelaxedGraph(cellVectors, relaxationIterations-1);
    }

    public void GenerateIslandMesh ()
    {
        GenerateMesh(GetLargestIsland());
        meshFilter.mesh = islandMesh;
    }

    public static VoronoiGraph Voronoi (List<Vector> vectors)
    {
        return Fortune.ComputeVoronoiGraph(vectors);
    }

    public VoronoiGraph 
    GenerateRelaxedGraph (List<Vector> cv, int stepsLeft)
    {
        // Implementation of Lloyd's Relaxation algorithm
        // http://en.wikipedia.org/wiki/Lloyd's_algorithm

        VoronoiGraph vg  = Voronoi(cv);
        List<Vector> ncv = new List<Vector>();

        if (stepsLeft == 0) 
        { 
            cellVectors = cv;
            return RefineGraph(vg); 
        }

        foreach(Vector v in cv)
        {
            Vector          centroid = new Vector(0, 0);
            HashSet<Vector> corners  = new HashSet<Vector>();

            foreach(VoronoiEdge e in vg.Edges)
            {
                if (e.LeftData != v && e.RightData != v) { continue; }
                
                corners.Add(e.VVertexA);    
                corners.Add(e.VVertexB);    
            }

            foreach (Vector c in corners) { centroid += c; }

            ncv.Add(centroid / corners.Count);
        }

        return GenerateRelaxedGraph(ncv, stepsLeft - 1);
    }

    public VoronoiGraph RefineGraph(VoronoiGraph g)
    {
        // Removes out of bound edges from a graph
        VoronoiGraph VGErg = new VoronoiGraph();
        
        foreach(VoronoiEdge e in g.Edges)
        {
            if (EdgeInBounds(e)) { VGErg.Edges.Add(e); }
        }

        foreach(VoronoiEdge VE in VGErg.Edges)
        {
            VGErg.Vertizes.Add(VE.VVertexA);
            VGErg.Vertizes.Add(VE.VVertexB);
        }

        return VGErg;
    }

    public bool EdgeInBounds(VoronoiEdge e)
    {
        return VectorInBounds(e.VVertexA) && VectorInBounds(e.VVertexB);
    }

    public bool VectorInBounds(Vector v)
    {
        return  (v[0] > -(scale) && v[0] < scale) && 
                (v[1] > -(scale) && v[1] < scale);
    }

    public List<Vector> RandomVectorList(int size)
    {
        List<Vector> vectors = new List<Vector>();
        Vector2      rVec    = Vector3.zero;
        
        for (int i = 0; i < size; i++)
        {
            rVec = Random.insideUnitCircle * scale;
            vectors.Add(new Vector((double) rVec.x, (double) rVec.y));
        }

        return vectors;
    }

    public static Vector3 ElevateVec(Vector3 v, float amt)
    {
        return v + (Vector3.up * amt);
    }

    public void GenerateMesh(Dictionary<Vector3, IslandTile> tiles)
    {
        List<IslandTile> ts = new List<IslandTile>();
        foreach(KeyValuePair<Vector3, IslandTile> t in tiles) 
        { 
            ts.Add(t.Value); 
        }

        GenerateMesh(ts);
    }

    public void GenerateMesh(List<IslandTile> tiles)
    {
        Mesh             mesh    = new Mesh();
        List<int>        tris    = new List<int>();
        List<Vector3>    verts   = new List<Vector3>(); 
        HashSet<Vector2> uv      = new HashSet<Vector2>();
        HashSet<Vector2> uv2     = new HashSet<Vector2>();

        foreach (IslandTile tile in tiles)
        {
            if (tile.IsWater) { continue; }
            
            if (!verts.Contains(tile.ElevatedCenter))
            {
                verts.Add(tile.ElevatedCenter);
                uv2.Add(new Vector2(tile.Moisture, Random.value));
                uv.Add(new Vector2( (float) tile.center[0], 
                                    (float) tile.center[1]));    
            }

            foreach(IslandTileCorner c in tile.corners)
            {
                if (!verts.Contains(c.ElevatedPosition))
                {
                    verts.Add(c.ElevatedPosition);
                    uv2.Add(new Vector2(c.Moisture, Random.value));
                    uv.Add(new Vector2( (float) c.position[0], 
                                        (float) c.position[1]));    
                }
            }

            foreach (IslandTileEdge e in tile.edges)
            {                
                Vector3[] tr = windTriangle(tile.ElevatedCenter, 
                                            e.cornerA.ElevatedPosition, 
                                            e.cornerB.ElevatedPosition);
                
                tris.AddRange(new int[3] {
                    verts.IndexOf(tr[0]),
                    verts.IndexOf(tr[1]),
                    verts.IndexOf(tr[2])                
                });
            } 
        }

        mesh.vertices  = verts.ToArray();
        mesh.uv        = uv.ToArray();
        mesh.uv2       = uv2.ToArray();
        mesh.triangles = tris.ToArray();
        
        mesh.RecalculateNormals(); 
        mesh.RecalculateBounds(); 
        mesh.Optimize();

        islandMesh = mesh;
    }

    private static Vector3[] windTriangle (Vector3 a, Vector3 b, Vector3 c)
    {   
        if (IsClockwise(a, b, c)) { return new Vector3[3] { a, b, c }; }
        else                      { return new Vector3[3] { a, c, b }; }
    } 

    private static bool IsClockwise (Vector  a, Vector  b, Vector  c)
    {
        return IsClockwise( IslandTile.VToV3(a), 
                            IslandTile.VToV3(b), 
                            IslandTile.VToV3(c));
    }

    private static bool IsClockwise (Vector3 a, Vector3 b, Vector3 c)
    {
        return Vector3.Cross((b - a), (c - a)).y > 0;
    }

    private HashSet<IslandTileCorner> GetCorners ()
    {
        var islandCorners = new HashSet<IslandTileCorner>();

        foreach (KeyValuePair<Vector3, IslandTile> t in islandTiles)
        {
            foreach (IslandTileCorner c in t.Value.corners) 
            { 
                islandCorners.Add(c); 
            }
        }

        return islandCorners;
    }

    private List<IslandTileCorner> GetLandCorners ()
    {
        List<IslandTileCorner> lcs = new List<IslandTileCorner>();

        foreach(KeyValuePair<Vector3, IslandTile> t in islandTiles)
        {
            if (!t.Value.IsWater)
            {
                foreach (IslandTileCorner c in t.Value.corners) { lcs.Add(c); }
            }
        }

        return lcs;
    }

    private static bool VectorIsInfinite (Vector3 v)
    {
        return  v.x == Mathf.Infinity || 
                v.y == Mathf.Infinity || 
                v.z == Mathf.Infinity;
    }

    private static bool VectorIsInfinite(Vector v)
    {
        return VectorIsInfinite(IslandTile.VToV3(v));
    }

    private void GenerateTiles ()
    {
        islandTiles = new Dictionary<Vector3, IslandTile>();

        foreach (Vector v in cellVectors)
        {
            if (VectorIsInfinite(v)) { continue; }
        
            IslandTile t = new IslandTile(v, vGraph, this);
            islandTiles.Add(IslandTile.VToV3(v), t);    
        }
    }

    private List<IslandTile> GetLandTiles ()
    {
        List<IslandTile> tiles = new List<IslandTile>();

        foreach (KeyValuePair<Vector3, IslandTile> tkv in islandTiles)
        {
            if (!tkv.Value.IsWater) { tiles.Add(tkv.Value); }
        }

        return tiles;
    }

    private void MarkSubIslands ()
    {
        List<IslandTile> ts = GetLandTiles();
        
        for (int i = 1; i < ts.Count; i++) { MarkIslandSegment(ts[i], i); }
    }

    private void MarkIslandSegment (IslandTile tile, int id)
    {
        if (tile.IsWater || id == tile.islandId) { return; }

        if (tile.islandId == 99 || tile.islandId < id)
        {
            if (tile.islandId < id) 
            { 
                subIslands.Remove(id);
                id = tile.islandId; 
            }

            tile.islandId = id;

            if (!subIslands.ContainsKey(id)) 
            { 
                subIslands[id] = new List<IslandTile>(); 
            }

            subIslands[id].Add(tile);
                
            foreach (Vector v in tile.neighbors)
            {
                IslandTile nt = islandTiles[IslandTile.VToV3(v)];
                MarkIslandSegment(nt, id);
            }
        }
    }

    public List<IslandTile> GetLargestIsland ()
    {
        int max         = 0;
        int largestIdx  = 0;
        
        foreach (KeyValuePair<int, List<IslandTile>> tl in subIslands)
        {
            if (tl.Value.Count > max)
            {
                max         = tl.Value.Count;
                largestIdx  = tl.Key;
            }
        }

        return subIslands[largestIdx];
    }

    private void CullExtraneousIslands ()
    {
        List<IslandTile> largestIslandTiles = GetLargestIsland();

        foreach (KeyValuePair<Vector3, IslandTile> tkv in islandTiles)
        {
            IslandTile tile = tkv.Value;
            if (!largestIslandTiles.Contains(tile)) { tile.forceWater = true; }
        }
    }

    private void SetCornerElevations ()
    {
        Queue<IslandTileCorner> cornerQueue      = new Queue<IslandTileCorner>();
        var                     islandCorners    = GetCorners();

        foreach (IslandTileCorner c in islandCorners)
        {
            if (c.IsWater) 
            { 
                c.elevation = 0;
                cornerQueue.Enqueue(c);
            }
        }

        while (cornerQueue.Count > 0)
        {
            IslandTileCorner c = cornerQueue.Dequeue();

            foreach (IslandTileCorner ac in c.adjacent)
            {
                if (ac.elevation != Mathf.Infinity || ac.IsWater) { continue; }
                
                float newElevation = c.elevation;
                
                float nLevel = islandNoise.GetValue((float) ac.position[0],
                                                    (float) ac.position[1],
                                                    0);

                newElevation += elevationStep * nLevel * scale; 
                
                ac.inlandDistance = c.inlandDistance + 1;
                ac.elevation = newElevation;
                
                cornerQueue.Enqueue(ac);
            }
        }
    }
}
