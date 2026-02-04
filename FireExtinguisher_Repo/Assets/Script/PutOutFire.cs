using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PutOutFire : MonoBehaviour
{
    [Header("Particle System Settings")]
    [SerializeField] private ParticleSystem sprayParticleSystem;

    [Header("Fire Extinguishing")]
    [SerializeField] private float extinguishRate = 0.5f; // How fast fire is extinguished per particle hit
    [SerializeField] private LayerMask fireLayer = -1; // Layer for fire objects

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private bool isSprayActive = false;
    private Dictionary<Fire, float> firesBeingExtinguished = new Dictionary<Fire, float>();

    void Start()
    {
        // Get particle system if not assigned
        if (sprayParticleSystem == null)
        {
            sprayParticleSystem = GetComponent<ParticleSystem>();
        }

        // Setup particle collision
        SetupParticleCollision();

        if (fireLayer.value == -1)
        {
            if (showDebugInfo)
                Debug.LogWarning("PutOutFire: fireLayer left as default (-1). Assign the Fire layer in the Inspector for accurate detection.");
        }
    }

    private void SetupParticleCollision()
    {
        if (sprayParticleSystem == null) return;

        // Enable collision module
        var collision = sprayParticleSystem.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;

        // Set collision quality
        collision.quality = ParticleSystemCollisionQuality.High;
        collision.collidesWith = fireLayer; // This should match your fire layer

        // Enable collision callbacks
        collision.sendCollisionMessages = true;

        // Optional: Make particles behave realistically
        collision.bounce = 0f; // No bounce
        collision.dampen = 1f; // Particles lose all energy on hit
        collision.lifetimeLoss = 0.2f; // Particles lose 20% lifetime on collision

        if (showDebugInfo)
        {
            Debug.Log($"Particle collision configured. Collides with layers: {LayerMaskToString(collision.collidesWith)}");
        }
    }

    public void StartSpray()
    {
        isSprayActive = true;
        firesBeingExtinguished.Clear();

        if (showDebugInfo)
        {
            Debug.Log("Spray started - particle collision detection active");
        }
    }

    public void StopSpray()
    {
        isSprayActive = false;
        firesBeingExtinguished.Clear();

        if (showDebugInfo)
        {
            Debug.Log("Spray stopped - clearing fire targets");
        }
    }

    // Called by Unity when particles collide with objects
    void OnParticleCollision(GameObject other)
    {
        if (!isSprayActive)
        {
            if (showDebugInfo)
            {
                Debug.Log("Particle collision detected but spray is not active");
            }
            return;
        }

        if (showDebugInfo)
        {
            Debug.Log($"Particle collision with: {other.name} on layer {other.layer} ({LayerMask.LayerToName(other.layer)})");
        }

        // Check if collided object (or parent) has a Fire component
        Fire fireComponent = other.GetComponent<Fire>() ?? other.GetComponentInParent<Fire>();

        if (fireComponent != null)
        {
            if (IsInLayerMask(other.layer, fireLayer))
            {
                ExtinguishFire(fireComponent, other);
            }
            else
            {
                if (showDebugInfo)
                    Debug.Log($"Fire component found on {other.name} but not on fire layer. Object layer: {other.layer}, Fire layer mask: {fireLayer}");
            }
        }
        else
        {
            if (showDebugInfo)
                Debug.Log($"No Fire component found on {other.name}");
        }
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    private void ExtinguishFire(Fire fire, GameObject hitObject)
    {
        if (fire == null) return;
        if (fire.IsExtinguished()) return;

        int particleHits = GetParticleHitCount(hitObject);

        float extinguishAmount = extinguishRate * Mathf.Min(particleHits, 3) * Time.deltaTime;

        float currentIntensity = fire.GetCurrentIntensity();
        float newIntensity = Mathf.Max(0f, currentIntensity - extinguishAmount);
        fire.SetIntensity(newIntensity);

        if (!firesBeingExtinguished.ContainsKey(fire))
        {
            firesBeingExtinguished[fire] = 0f;
        }
        firesBeingExtinguished[fire] += extinguishAmount;

        if (showDebugInfo)
        {
            Debug.Log($"Extinguishing {fire.name}: {particleHits} particles hit, extinguish amount: {extinguishAmount:F4}, intensity: {newIntensity:F2}");
        }

        // Cleanup null entries
        List<Fire> toRemove = new List<Fire>();
        foreach (var kv in firesBeingExtinguished)
        {
            if (kv.Key == null) toRemove.Add(kv.Key);
        }
        foreach (var r in toRemove) firesBeingExtinguished.Remove(r);
    }

    private int GetParticleHitCount(GameObject hitObject)
    {
        if (sprayParticleSystem == null) return 1;

        List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
        int eventCount = ParticlePhysicsExtensions.GetCollisionEvents(sprayParticleSystem, hitObject, collisionEvents);

        return Mathf.Max(1, eventCount);
    }

    private string LayerMaskToString(LayerMask layerMask)
    {
        string result = "";
        for (int i = 0; i < 32; i++)
        {
            if ((layerMask.value & (1 << i)) != 0)
            {
                if (result.Length > 0) result += ", ";
                result += LayerMask.LayerToName(i);
            }
        }
        return string.IsNullOrEmpty(result) ? "None" : result;
    }

    public List<Fire> GetActiveTargets()
    {
        List<Fire> result = new List<Fire>();
        List<Fire> toRemove = new List<Fire>();
        foreach (var kv in firesBeingExtinguished)
        {
            if (kv.Key == null) toRemove.Add(kv.Key);
            else result.Add(kv.Key);
        }
        foreach (var r in toRemove) firesBeingExtinguished.Remove(r);
        return result;
    }

    public bool IsExtinguishingFires()
    {
        List<Fire> toRemove = new List<Fire>();
        foreach (var kv in firesBeingExtinguished)
        {
            if (kv.Key == null) toRemove.Add(kv.Key);
        }
        foreach (var r in toRemove) firesBeingExtinguished.Remove(r);

        return firesBeingExtinguished.Count > 0;
    }

    [ContextMenu("Test Collision Setup")]
    void TestCollisionSetup()
    {
        if (sprayParticleSystem != null)
        {
            var collision = sprayParticleSystem.collision;
            Debug.Log($"Collision enabled: {collision.enabled}");
            Debug.Log($"Send collision messages: {collision.sendCollisionMessages}");
            Debug.Log($"Collides with: {LayerMaskToString(collision.collidesWith)}");
        }
        else
        {
            Debug.LogError("No particle system assigned!");
        }
    }
}
