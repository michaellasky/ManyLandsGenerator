using System;
using CoherentNoise;
using CoherentNoise.Generation;
using CoherentNoise.Generation.Displacement;
using CoherentNoise.Generation.Fractal;
using CoherentNoise.Generation.Modification;
using CoherentNoise.Generation.Patterns;
using CoherentNoise.Texturing;
using UnityEngine;
using System.Collections;
using Random = UnityEngine.Random;

public class TerrainDemo : MonoBehaviour
{
    // terrains that we would fly over. Terrain1 is currently showing, Terrain2 is farther away and being generated
    public Terrain Terrain1;
    public Terrain Terrain2;

    public float Speed = 50; // flight speed

    private Generator m_Generator; // noise generator for terrain height
    private Generator m_Weight; // noise generator for terrain type: hills vs desert
    private int m_NoiseCoord = 0; // current terrain coordinate in domain (noise) space
    private bool m_Move; // are we flying yet?
    private bool m_CameraChangingHeight; // is camera height changing?

    void Start()
    {
        if (!Terrain1 || !Terrain2)
        {
            Debug.LogError("Terrains not set!!");
            enabled = false;
        }

        // desert dune-like ridges are created using RidgeNoise. it is scaled down a bit, and Gain applied to make ridges more pronounced
        var desert = new Gain(
            new RidgeNoise(23478568)
                {
                    OctaveCount = 8
                } * 0.6f, 0.4f);
        // hills use a simple pink noise. 
        var hills = new PinkNoise(3465478)
                        {
                            OctaveCount = 5, // smooth hills (no higher frequencies)
                            Persistence = 0.56f // but steep (changes rapidly in the frequencies we do have)
                        };
        // weight decides, whether a given point is hills or desert
        // cached, as we'd need it to decide texture at every point
        m_Weight = new Cache(new PinkNoise(346546)
                                    {
                                        Frequency = 0.5f,
                                        OctaveCount = 3
                                    }.ScaleShift(0.5f, 0.5f));
        // overall heightmap blends hills and deserts
        m_Generator = desert.Blend(hills, m_Weight).ScaleShift(0.5f, 0.5f);
        // as changes to terrains made in playmode get saved in the editor, we need to re-generate terrains
        // at start. The coroutine will set m_Move in the end, so that camera does not fly intil it's ready
#if UNITY_EDITOR
        StartCoroutine(CreateMapsAtStart());
#else
        m_NoiseCoord=1;
        m_Move=true;
#endif
    }

    // Update is called once per frame
    void Update()
    {
        if (m_Move)
        {
            var t = Terrain1;
            // the point where camera should move
            Vector3 nextPoint = Camera.main.transform.position + new Vector3(0, 0, Speed * Time.deltaTime);
            // let's fond out terrain height there
            Vector3 coord = nextPoint - t.transform.position;
            var x = coord.z / (t.terrainData.heightmapWidth * t.terrainData.heightmapScale.x);
            var y = coord.x / (t.terrainData.heightmapHeight * t.terrainData.heightmapScale.z);
            // this is the desired camera height
            var targetheight = t.terrainData.GetInterpolatedHeight(y, x) + 25;

            // only change height if we are sufficiently far away from target, OR the correction has started
            // all this stuff here is to make (vertical) camera movement nice and smooth
            if (m_CameraChangingHeight || Mathf.Abs(targetheight - nextPoint.y) > 10)
            {
                // if we're too low, jump up: no falling throug terrain here!
                if (targetheight > nextPoint.y + 20)
                    nextPoint.y = targetheight;
                else
                {
                    // make vertical speed higher if we're too low. Descending is always constant (low) speed.
                    var speedCoeff = targetheight < nextPoint.y + 5 ? 0.25f : (targetheight - nextPoint.y - 5)*0.2f;
                    // we're not too low, so fly towards target height 
                    var delta = Math.Min(Mathf.Abs(targetheight - nextPoint.y), Speed * speedCoeff * Time.deltaTime);
                    nextPoint.y += delta * Mathf.Sign(targetheight - nextPoint.y);
                }
                // if we reached target height (more or less), go to horizintal flight
                m_CameraChangingHeight = Mathf.Abs(targetheight - nextPoint.y) > 0.5f;
            }
            // ok, actually move camera
            Camera.main.transform.position = nextPoint;

            // camera flew over Terrain1 and is showing Terrain2 -  let's switch terrains
            if (Camera.main.transform.position.z > 2000) // 2000 is the size of terrain. Hardcoding it is bad code.
                SwitchTerrains();
        }
    }

    private void SwitchTerrains()
    {
        // return camera to start (we want to always be near coordinate origin, so that float precision does not become an issue)
        var delta = Camera.main.transform.position.z;
        Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, 0);
        // move terrains with camera
        Terrain1.transform.position = Terrain1.transform.position - new Vector3(0, 0, delta);
        Terrain2.transform.position = Terrain2.transform.position - new Vector3(0, 0, delta);
        // move terrain1 to farther position
        Terrain1.transform.position = Terrain1.transform.position + new Vector3(0, 0, 4000); // terrains are 2000x2000
        // swap terrains so that t1 becomes t2
        var t = Terrain1;
        Terrain1 = Terrain2;
        Terrain2 = t;
        // noise domain shifted
        m_NoiseCoord++;
        // aaaand start new terrain generation
        StartCoroutine(CreateTerrain(t));
    }

    private IEnumerator CreateTerrain(Terrain t)
    {
        var start = DateTime.UtcNow.Ticks;
        var td = t.terrainData;
        // fixing resultions in code, as there's no place in editor to do that
        td.alphamapResolution = td.heightmapResolution;
        td.SetDetailResolution(td.heightmapWidth, 16);
        
        // arrays for various maps used in terrain
        var hm = new float[td.heightmapWidth, td.heightmapHeight]; // height
        var am = new float[td.heightmapWidth, td.heightmapHeight, 2]; // alpha (textures)
        var dm = new int[td.detailResolution, td.detailResolution]; // detail (grass)

        // create heightmap data
        for (int ii = 0; ii < td.heightmapWidth; ++ii)
            for (int jj = 0; jj < td.heightmapHeight; ++jj)
            {
                // check our running time. We want to yield every now and then, so that FPS don't stall
                var timeInMs = (float)(DateTime.UtcNow.Ticks - start) / TimeSpan.TicksPerMillisecond;
                if (timeInMs > 1000f / 25) // shoot for 25 FPS 
                {
                    start = DateTime.UtcNow.Ticks;
                    yield return null;
                }

                // Domain coordinates. Each terrain is 1x1 in domain space
                float x = (float)ii / (td.heightmapWidth - 1) + m_NoiseCoord;
                float y = (float)jj / (td.heightmapHeight - 1);

                // calculate height. This is where the performance hit is; everything else is peanuts compared to evaluating
                // a complex noise function
                var v = m_Generator.GetValue(x, y, 0);
                hm[ii, jj] = v;

                // texture alpha. Weight determines, whether we are generating hills or desert here, or something in between
                var w = Mathf.Clamp01(m_Weight.GetValue(x, y, 0) * 1.3f); // *1.3 to make more grassy areas. Looks nicer this way.
                am[ii, jj, 0] = 1 - w;  // w==0 means deserts
                am[ii, jj, 1] = w;      // w==1 means hills

                // details count
                var details = w > 0.5f ? (w - 0.5) * 40 : 0; // w<0.5 means desert, so no grass there. 
                // Unity does not allow to set detail map resultion at will, so we have to map heightmap coords to detailmap
                // (although in this case terrains are square, and resolutions match exactly)
                int dx = ii * td.detailResolution / td.heightmapWidth;
                int dy = jj * td.detailResolution / td.heightmapHeight;

                // when we have less than 1 blade of grass, add one randomly. This makes grass to no-grass transition smoother
                dm[dx, dy] = details > 1 ? (int)details : Random.value < details ? 1 : 0;
            }

        // apply changes to terrain
        yield return null; // end frame
        td.SetHeights(0, 0, hm);
        yield return null; // end frame
        td.SetAlphamaps(0, 0, am);
        yield return null; // end frame
        td.SetDetailLayer(0, 0, 0, dm);

        // according to documentation, we should register terrains as neighbours, so that LODs match
        // however, in practice they don't match anyway. There's probably some trick here.
        //Terrain2.SetNeighbors(null, null, Terrain1, null);
        //Terrain1.SetNeighbors(Terrain2, null, null, null);
    }

    private IEnumerator CreateMapsAtStart()
    {
        var t = CreateTerrain(Terrain1);
        while (t.MoveNext())
        {
            yield return null;
        }
        m_NoiseCoord++;
        t = CreateTerrain(Terrain2);
        while (t.MoveNext())
        {
            yield return null;
        }
        m_Move = true;
    }
}
