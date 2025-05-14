using UnityEngine;

public class Boid : MonoBehaviour
{
    public Rigidbody rigidBody;

    private void Awake()
    {
        Renderer boidRenderer = GetComponent<Renderer>();
        if (boidRenderer != null && boidRenderer.material != null)
        {
            boidRenderer.material.SetColor("_BaseColor", Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f));
        }
        else
        {
            Debug.LogError("Boid is missing a Renderer or Material to set color. Make sure your Boid Prefab has a Mesh Renderer with a Material.", this.gameObject);
        }

        rigidBody = GetComponent<Rigidbody>();
        if (rigidBody != null)
        {
            rigidBody.linearVelocity = Random.insideUnitSphere * Random.Range(0f, 1f);
        }
        else
        {
            Debug.LogError("Boid is missing a Rigidbody component. Make sure your Boid Prefab has a Rigidbody.", this.gameObject);
        }
    }

    void Update()
    {
        if (rigidBody != null && rigidBody.linearVelocity != Vector3.zero) // Check velocity is not zero before aligning
        {
            AlignToVelocity();
        }
    }

    public void AlignToVelocity()
    {
        float maxRadiansDelta = Mathf.Deg2Rad * 180f * Time.deltaTime;
        float maxMagnitudeDelta = 100f;
        transform.forward = Vector3.RotateTowards(transform.forward, rigidBody.linearVelocity.normalized, maxRadiansDelta, maxMagnitudeDelta);
    }
}