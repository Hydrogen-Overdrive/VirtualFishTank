using UnityEngine;

public class BoidSimulationControl : MonoBehaviour
{
    public enum ControlMode
    {
        Seek,
        Pursue,
        Food,
        Obstacle
    }
    public ControlMode controlMode = ControlMode.Seek;

    public GameObject boidPrefab = null;
    public int numBoidsToSpawn = 10; // Variable name from your script

    private void Start()
    {
        if (boidPrefab == null)
        {
            Debug.LogError("ERROR: Boid Prefab is NOT assigned in BoidSimulationControl on GameObject '" + this.gameObject.name + "'. Please assign it in the Inspector.", this.gameObject);
            return;
        }

        Debug.Log("BoidSimulationControl: Starting to spawn boids using tutorial-specific global coordinates. Target count related to numBoidsToSpawn: " + numBoidsToSpawn);

        // Loop condition and spawning coordinates from your image_c15852.png
        // This loop will run (numBoidsToSpawn + 1) times.
        for (int i = 0; i <= numBoidsToSpawn; ++i)
        {
            // Spawning coordinates from your image_c15852.png
            Vector3 spawnPosition = new Vector3(Random.Range(-1.4f, 1.4f),
                                                Random.Range(0f, 1.4f),
                                                Random.Range(-0.9f, 0.9f));

            Quaternion spawnRotation = UnityEngine.Random.rotation;
            GameObject newBoid = Instantiate(boidPrefab, spawnPosition, spawnRotation);
            newBoid.name = "Boid_CtrlSpawn_" + i;
        }
        Debug.Log((numBoidsToSpawn + 1) + " boids should have been attempted to spawn by BoidSimulationControl.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) { controlMode = ControlMode.Seek; Debug.Log("ControlMode set to Seek"); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { controlMode = ControlMode.Pursue; Debug.Log("ControlMode set to Pursue"); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { controlMode = ControlMode.Food; Debug.Log("ControlMode set to Food"); }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { controlMode = ControlMode.Obstacle; Debug.Log("ControlMode set to Obstacle"); }
    }
}