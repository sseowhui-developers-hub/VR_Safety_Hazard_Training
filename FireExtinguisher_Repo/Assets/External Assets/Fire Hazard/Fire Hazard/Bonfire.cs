using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;   // Needed for VisualEffect

public class Bonfire : MonoBehaviour
{
    [Header("Fire Settings")]
    [Range(0f, 1f)] public float intensity = 1f;
    public float extinguisherEffectivenessMultiplier = 1f;

    [Header("References")]
    [Tooltip("Optional: ParticleSystems (auto-detected if empty).")]
    public List<ParticleSystem> flameParticles = new List<ParticleSystem>();

    [Tooltip("Optional: VisualEffects (auto-detected if empty).")]
    public List<VisualEffect> flameVFX = new List<VisualEffect>();

    public Light flameLight;
    public AudioSource burnAudio;

    [Header("Debug")]
    public bool debugLogs = true;

    private float baselineLightIntensity = 1f;

    void Start()
    {
        // Auto-find ParticleSystems if not manually assigned
        if (flameParticles.Count == 0)
        {
            flameParticles.AddRange(GetComponentsInChildren<ParticleSystem>(true));
        }

        // Auto-find VisualEffects if not manually assigned
        if (flameVFX.Count == 0)
        {
            flameVFX.AddRange(GetComponentsInChildren<VisualEffect>(true));
        }

        if (flameLight != null)
            baselineLightIntensity = flameLight.intensity;

        if (burnAudio != null && intensity > 0.02f)
            burnAudio.Play();

        if (debugLogs)
        {
            Debug.Log("[Bonfire] Initialized '" + gameObject.name +
                      "' with " + flameParticles.Count + " ParticleSystems, " +
                      flameVFX.Count + " VisualEffects.");
        }
    }

    void Update()
    {
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        // Light
        if (flameLight != null)
        {
            flameLight.intensity = Mathf.Lerp(0f, baselineLightIntensity, Mathf.Clamp01(intensity));
            flameLight.enabled = intensity > 0.01f;
        }

        // Audio
        if (burnAudio != null)
        {
            if (intensity > 0.02f)
            {
                if (!burnAudio.isPlaying) burnAudio.Play();
                burnAudio.volume = Mathf.Clamp01(intensity);
            }
            else
            {
                if (burnAudio.isPlaying) burnAudio.Stop();
            }
        }

        // Handle extinguish
        if (intensity <= 0.001f)
        {
            OnExtinguished();
        }
    }

    public void ReduceFire(float amount)
    {
        if (amount <= 0f) return;

        float before = intensity;
        intensity -= amount * extinguisherEffectivenessMultiplier;
        intensity = Mathf.Clamp01(intensity);

        if (debugLogs)
            Debug.Log("[Bonfire] ReduceFire: " + before.ToString("0.000") + " -> " + intensity.ToString("0.000"));

        if (intensity <= 0.001f)
            OnExtinguished();
    }

    private void OnExtinguished()
    {
        if (debugLogs) Debug.Log("[Bonfire] Extinguished '" + gameObject.name + "'");

        // Disable ParticleSystems
        foreach (var ps in flameParticles)
        {
            if (ps == null) continue;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Clear(true);
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null) renderer.enabled = false;
            if (debugLogs) Debug.Log("[Bonfire] Disabled ParticleSystem '" + ps.gameObject.name + "'");
        }

        // Disable VisualEffects
        foreach (var vfx in flameVFX)
        {
            if (vfx == null) continue;
            vfx.Stop();
            vfx.Reinit();
            vfx.enabled = false;
            if (debugLogs) Debug.Log("[Bonfire] Disabled VisualEffect '" + vfx.gameObject.name + "'");
        }

        // Optionally disable the whole bonfire object
        // gameObject.SetActive(false);
    }
}
