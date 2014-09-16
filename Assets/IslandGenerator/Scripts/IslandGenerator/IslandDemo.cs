using UnityEngine;
using System.Collections;

public class IslandDemo : MonoBehaviour {

    public GameObject islandPrefab;

    private GameObject currentIsland;

    void Start () 
    {
        SpawnIsland ();
    }

    void SpawnIsland () 
    {
        if (currentIsland != null) { Destroy (currentIsland); }
    
        currentIsland = (GameObject) GameObject.Instantiate (islandPrefab, Vector3.zero, Quaternion.identity);

        Island isl = currentIsland.GetComponent<Island>();
        isl.islandPosition = new Vector3 (Random.Range (0, 10000), Random.Range (0, 10000), Random.Range (0, 10000));
        isl.Regenerate(true);
    }

    void OnGUI ()
    {
        if (GUI.Button (new Rect (10, 70, 500, 30), "Generate New Island")) 
        {
            SpawnIsland();
        }
    }
}
