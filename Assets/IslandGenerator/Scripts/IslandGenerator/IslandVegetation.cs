using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class IslandVegetation : MonoBehaviour 
{
	public Material vegetationMaterial;
	public float 	density = 0.65f;
	public string 	vegetationName;
	public float    scale = 0.1f;
	public float    maxParticles = 3000;
	public float 	minSlope = 0.2f;

	public AnimationCurve moistureTolerance;
	public AnimationCurve altitudeTolerance;
	
	private ParticleRenderer 	particleRenderer;
	private ParticleEmitter 	pEmitter;
	private MeshFilter 			meshFilter;

	private Island 		   		island;
	private Transform   		t;
	private float 				lifetime = 100000;

	void Awake () 
	{
		t      = gameObject.GetComponent<Transform>();
		island = (Island) gameObject.GetComponent<Island>();
	}

	public void SpawnVegetation ()
	{
		CreateParticleSystem();
		EmitParticles();
	}

	void CreateParticleSystem () 
	{
		GameObject 	pSysGo 	   = new GameObject(vegetationName);
		string 	   	emitterType = "MeshParticleEmitter";

		pSysGo.GetComponent<Transform>().parent = t;

		meshFilter = pSysGo.AddComponent<MeshFilter>();
		meshFilter.mesh = island.IslandMesh;

		pEmitter = (ParticleEmitter) pSysGo.AddComponent(emitterType);
		particleRenderer = pSysGo.AddComponent<ParticleRenderer>();

		particleRenderer.renderer.sharedMaterial = vegetationMaterial;
		particleRenderer.maxParticleSize = 0.01f;
		particleRenderer.particleRenderMode = ParticleRenderMode.VerticalBillboard;

		pEmitter.maxEmission = maxParticles;

		pEmitter.emit = false;
	}
	
	void EmitParticles ()
	{
		foreach (IslandTile tile in island.GetLargestIsland()) 
		{	
			// Too Steep?	
			if (Mathf.Abs(tile.Normal.y) < minSlope) { continue; }
			
			float   baseMoisture     = tile.BaseMoisture;
			float 	alt 			 = tile.Elevation / island.Scale;
			float 	altitudeModifier = altitudeTolerance.Evaluate(alt);
			float 	moistureModifier = moistureTolerance.Evaluate(baseMoisture);
			float   comfortModifier  = 	density * 
										Random.value * 
										moistureModifier * 
										altitudeModifier;

			int   	numLoops 		= (int) (100 * comfortModifier);
			
			for (int i = 0; i < numLoops; i++)
			{
				Vector3 position = tile.RandomFacePosition();
			
				pEmitter.Emit (	position, 
										Vector3.zero, 
										scale * Random.value, 
										lifetime, 
										Color.white);
			}
		}
	}
}
