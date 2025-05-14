using UnityEngine;
using System.Collections.Generic; 

public class BoidSimulation : MonoBehaviour
{
    [Header("Boid Settings")]
    public GameObject boidPrefab;       // The blueprint for our boids
    public int boidsToSpawn = 10;       // How many boids to create

    [Header("Spawning Area (Relative to Fish Tank)")]
    public Transform fishTankTransform; // Assign your main visual fish tank GameObject here
    public Vector3 spawnAreaSize = new Vector3(9f, 4f, 9f); // X, Y, Z dimensions of the spawn volume centered on the tank

    // Optional: A list to keep track of spawned boids
    private List<GameObject> spawnedBoids = new List<GameObject>();

    void Start()
    {
        // --- Essential Sanity Checks ---
        if (boidPrefab == null)
        {
            Debug.LogError("ERROR: 'Boid Prefab' has not been assigned in the BoidSimulationControl script! Please assign it in the Inspector.", this.gameObject);
            this.enabled = false; // was life saver in debugin
            return;
        }

        if (fishTankTransform == null)
        {
            Debug.LogError("ERROR: 'Fish Tank Transform' has not been assigned in the BoidSimulationControl script! Please assign your visual fish tank GameObject in the Inspector.", this.gameObject);
            this.enabled = false; 
            return;
        }

        Debug.Log("BoidSimulationControl: Starting to spawn " + boidsToSpawn + " boids.");

        // --- Spawn the Boids ---
        for (int i = 0; i < boidsToSpawn; i++)
        {
            // Calculate a random offset from the center of the spawnAreaSize
            // The spawnAreaSize defines the TOTAL width, height, depth.
            //  went from -halfSize to +halfSize for each component.
            float offsetX = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
            float offsetY = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
            float offsetZ = Random.Range(-spawnAreaSize.z / 2f, spawnAreaSize.z / 2f);

            Vector3 randomOffset = new Vector3(offsetX, offsetY, offsetZ);

            // The final spawn position is the fish tank's world position + our random offset
            Vector3 spawnPosition = fishTankTransform.position + randomOffset;

            // Created a new boid from the prefab at the calculated spawn position, with a random initial rotation
            GameObject newBoid = Instantiate(boidPrefab, spawnPosition, Random.rotation);

            newBoid.name = "Boid_" + i;         
            spawnedBoids.Add(newBoid);         
        }

        Debug.Log(boidsToSpawn + " boids have been spawned.");
    }

   
    void OnDrawGizmosSelected()
    {
        // Only draw if the fishTankTransform has been assigned
        if (fishTankTransform != null)
        {
            // Set the color for our Gizmo
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Green, slightly transparent

            // Gizmo center will be the fishTank Transform position
            // Gizmo size will be the spawnAreaSize
            Gizmos.DrawCube(fishTankTransform.position, spawnAreaSize);

            
            Gizmos.color = Color.green; // Solid green for the wireframe
            Gizmos.DrawWireCube(fishTankTransform.position, spawnAreaSize);
        }
        else
        {
            
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Red, slightly transparent
            Gizmos.DrawCube(this.transform.position, spawnAreaSize);
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(this.transform.position, spawnAreaSize);
            
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            { // Only show this message if not playing
                Debug.LogWarning("Fish Tank Transform not set on BoidSimulationControl. Gizmo is showing spawn area relative to this object (" + this.gameObject.name + "). Assign your Fish Tank to position boids correctly.", this.gameObject);
            }
#endif
        }
    }
}