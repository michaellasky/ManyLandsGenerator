using UnityEngine;
using System;
using System.Collections;
using CoherentNoise;
using CoherentNoise.Generation;
using CoherentNoise.Generation.Displacement;
using CoherentNoise.Generation.Fractal;
using CoherentNoise.Generation.Modification;
using CoherentNoise.Generation.Patterns;
using CoherentNoise.Texturing;
using Random = UnityEngine.Random;

public class World : MonoBehaviour 
{

    public  static   int         worldSeed;

    private static   World       instance;
    private          Generator   worldNoise;
    
    void Awake ()
    {
        instance        = this;
        worldSeed       = (int) (Random.value * System.DateTime.Now.Ticks);
        Random.seed     = worldSeed;
        Time.timeScale  = 1f;
		GenerateWorld ();
    }

    public void GenerateWorld ()
    {
        worldNoise = new RidgeNoise(worldSeed) + new BillowNoise(worldSeed); 
    }

    public Generator GetNoiseAt (Vector3 pos)
    {
        return new Translate(worldNoise, pos);
    }
}
