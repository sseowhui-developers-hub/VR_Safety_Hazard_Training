using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class Fire : MonoBehaviour
{
    [Header("Fire Intensity")]
    [SerializeField, Range(0f, 1f)] private float currentIntensity = 1.0f;
    [SerializeField] private float maxIntensity = 1.0f;
    private float[] startIntensities = new float[0];
    [SerializeField] private ParticleSystem[] fireParticleSystems = new ParticleSystem[0];

    [Header("Fire Extinguishing")]
    [SerializeField] private bool isExtinguished = false;
    [SerializeField] private float extinguishThreshold = 0.01f;

    [Header("Audio Effects")]
    [SerializeField] private AudioClip extinguishSound;
    [SerializeField] private AudioSource audioSource;

    [Header("Visual Effects")]
    [SerializeField] private Light fireLight;
    private Color originalLightColor;
    private float originalLightIntensity;

    public System.Action<Fire> OnFireExtinguished;
    public System.Action<Fire, float> OnIntensityChanged;

    private void OnEnable()
    {
        StartCoroutine(RegisterActivationSafe());
    }

    private IEnumerator RegisterActivationSafe()
    {
        yield return null;
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterActivatedFire(this);
    }

    private void Start()
    {
        FireManager.Instance?.RegisterFire(this);

        if (GameManager.Instance != null)
            GameManager.Instance.RegisterActivatedFire(this);

        startIntensities = new float[fireParticleSystems.Length];
        for (int i = 0; i < fireParticleSystems.Length; i++)
        {
            if (fireParticleSystems[i] != null)
                startIntensities[i] = fireParticleSystems[i].emission.rateOverTime.constant;
        }

        if (fireLight != null)
        {
            originalLightColor = fireLight.color;
            originalLightIntensity = fireLight.intensity;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        maxIntensity = currentIntensity;
    }

    private void Update()
    {
        ChangeIntensity();
        if (currentIntensity <= extinguishThreshold && !isExtinguished)
            ExtinguishCompletely();
    }

    private void ChangeIntensity()
    {
        for (int i = 0; i < fireParticleSystems.Length; i++)
        {
            if (fireParticleSystems[i] != null)
            {
                var emission = fireParticleSystems[i].emission;
                emission.rateOverTime = currentIntensity * startIntensities[i];
            }
        }

        if (fireLight != null)
        {
            fireLight.intensity = originalLightIntensity * currentIntensity;
            fireLight.color = Color.Lerp(Color.red, originalLightColor, currentIntensity);
        }
    }

    // Public API
    public float GetCurrentIntensity() => currentIntensity;

    public void SetIntensity(float newIntensity)
    {
        float old = currentIntensity;
        currentIntensity = Mathf.Clamp01(newIntensity);
        if (Mathf.Abs(old - currentIntensity) > 0.01f)
            OnIntensityChanged?.Invoke(this, currentIntensity);
    }

    public void ReduceIntensity(float amount) => SetIntensity(currentIntensity - amount);
    public bool IsExtinguished() => isExtinguished;
    public void Extinguish() => SetIntensity(0f);

    private void ExtinguishCompletely()
    {
        if (isExtinguished) return;
        isExtinguished = true;

        // 🔊 Play extinguish sound and delay destruction
        if (audioSource != null && extinguishSound != null)
        {
            audioSource.PlayOneShot(extinguishSound);
            Destroy(gameObject, extinguishSound.length);
        }
        else
        {
            Destroy(gameObject);
        }

        for (int i = 0; i < fireParticleSystems.Length; i++)
        {
            if (fireParticleSystems[i] != null)
            {
                var emission = fireParticleSystems[i].emission;
                emission.rateOverTime = 0;
                fireParticleSystems[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        if (fireLight != null)
            fireLight.enabled = false;

        OnFireExtinguished?.Invoke(this);

        FireManager.Instance?.OnFireExtinguished(this);

        if (GameManager.Instance != null)
            GameManager.Instance.NotifyFireExtinguished(this);

        Debug.Log($"Fire '{gameObject.name}' extinguished.");
    }

    public void RelightFire(float intensity = 1f)
    {
        if (FireManager.Instance != null && !FireManager.Instance.CanStartNewFire()) return;
        isExtinguished = false;
        currentIntensity = Mathf.Clamp01(intensity);
        for (int i = 0; i < fireParticleSystems.Length; i++)
            if (fireParticleSystems[i] != null) fireParticleSystems[i].Play();
        if (fireLight != null) fireLight.enabled = true;
    }

    public float GetFirePercentage() => (currentIntensity / maxIntensity) * 100f;
    public bool IsCriticallyLow() => currentIntensity <= (maxIntensity * 0.25f);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.Lerp(Color.black, Color.red, currentIntensity);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.3f * extinguishThreshold);
    }
}
