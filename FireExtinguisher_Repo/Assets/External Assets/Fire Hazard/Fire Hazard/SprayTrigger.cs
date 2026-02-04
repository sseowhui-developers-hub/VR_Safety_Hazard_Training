using UnityEngine;

public class SprayTrigger : MonoBehaviour
{
    [Header("Spray Settings")]

    [Tooltip("How much fire intensity to remove per second while spraying (0..1 scale).")]
    public float reducePerSecond = 0.6f;

    [Header("Debug")]
    public bool debugLogs = true;

    [Header("Extinguish Options")]
    [Tooltip("If true, the spray will instantly set a Bonfire's intensity to 0 on first contact (ignores reducePerSecond).")]
    public bool instantExtinguish = false;

    [Header("Bonfire Tags")]
    [Tooltip("List all bonfire tags the spray can extinguish (e.g. Bonfire, Bonfire1, Bonfire2...).")]
    public string[] bonfireTags = new string[] { "Bonfire", "Bonfire1", "Bonfire2", "Bonfire3", "Bonfire4", "Bonfire5" };

    private Collider myCol;
    private bool isSpraying = false;

    // -------------------
    // Lifecycle
    // -------------------
    void Start()
    {
        myCol = GetComponent<Collider>();
        if (myCol == null)
        {
            if (debugLogs) Debug.LogWarning($"[SprayTrigger] No Collider found on '{gameObject.name}'. OnTrigger events won't fire. Add a Collider (Is Trigger = true) to this same GameObject.");
        }
        else
        {
            // ensure it's trigger (spray should be a trigger collider)
            if (!myCol.isTrigger && debugLogs)
                Debug.LogWarning($"[SprayTrigger] Collider on '{gameObject.name}' is not marked Is Trigger. Set Is Trigger = true for spray collision.");

            myCol.enabled = false; // start disabled — only enabled while spraying
            if (debugLogs) Debug.Log($"[SprayTrigger] Collider found on '{gameObject.name}'. IsTrigger={myCol.isTrigger}");
        }

        if (debugLogs) Debug.Log($"[SprayTrigger] Start on '{gameObject.name}'. ReducePerSecond={reducePerSecond} InstantExtinguish={instantExtinguish}");
    }

    void Update()
    {
        // optional heartbeat when spraying (reduce log spam)
        if (isSpraying && debugLogs && Time.frameCount % 120 == 0)
            Debug.Log($"[SprayTrigger] Still spraying on '{gameObject.name}'...");
    }

    // -------------------
    // Public control methods - called by FireExtinguisherPinInputSystem
    // -------------------
    public void StartSpray()
    {
        if (isSpraying) return;
        isSpraying = true;

        if (myCol != null && !myCol.enabled)
            myCol.enabled = true;

        if (debugLogs) Debug.Log($"[SprayTrigger] StartSpray() on '{gameObject.name}' - Collider enabled? {(myCol != null ? myCol.enabled.ToString() : "NO COLLIDER")} - InstantExtinguish={instantExtinguish}");
    }

    public void StopSpray()
    {
        if (!isSpraying) return;
        isSpraying = false;

        if (myCol != null && myCol.enabled)
            myCol.enabled = false;

        if (debugLogs) Debug.Log($"[SprayTrigger] StopSpray() on '{gameObject.name}' - Collider enabled? {(myCol != null ? myCol.enabled.ToString() : "NO COLLIDER")}");
    }

    // Property to check if currently spraying
    public bool IsSpraying => isSpraying;

    // -------------------
    // Trigger handlers
    // -------------------
    private void OnTriggerEnter(Collider other)
    {
        if (debugLogs) Debug.Log($"[SprayTrigger] OnTriggerEnter: '{other.gameObject.name}' (tag={other.tag})");
    }

    private void OnTriggerExit(Collider other)
    {
        if (debugLogs) Debug.Log($"[SprayTrigger] OnTriggerExit: '{other.gameObject.name}' (tag={other.tag})");
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isSpraying)
        {
            if (debugLogs) Debug.Log($"[SprayTrigger] OnTriggerStay detected but not spraying (other='{other.gameObject.name}', tag={other.tag})");
            return;
        }

        if (debugLogs) Debug.Log($"[SprayTrigger] OnTriggerStay while spraying: '{other.gameObject.name}' (tag={other.tag})");

        // Check if the collided object's tag is one of the bonfireTags
        bool tagMatches = false;
        foreach (string t in bonfireTags)
        {
            if (string.IsNullOrEmpty(t)) continue;
            if (other.CompareTag(t))
            {
                tagMatches = true;
                break;
            }
        }

        if (!tagMatches)
        {
            if (debugLogs) Debug.Log($"[SprayTrigger] Collided object is not in bonfireTags -> '{other.gameObject.name}' (tag={other.tag})");
            return;
        }

        // Try to get the Bonfire component on the collided object
        Bonfire b;
        if (!other.TryGetComponent<Bonfire>(out b))
        {
            if (debugLogs) Debug.LogWarning($"[SprayTrigger] Object tagged '{other.tag}' but no Bonfire component found on '{other.gameObject.name}'");
            return;
        }

        // If instantExtinguish: compute the amount required to zero the intensity and call ReduceFire(amount)
        if (instantExtinguish)
        {
            float multiplier = Mathf.Max(1e-6f, b.extinguisherEffectivenessMultiplier);
            float amountNeeded = b.intensity / multiplier;
            float before = b.intensity;
            b.ReduceFire(amountNeeded); // should set intensity to ~0
            float after = b.intensity;

            if (debugLogs) Debug.Log($"[SprayTrigger] Instant extinguish applied to '{other.gameObject.name}': amountNeeded={amountNeeded:0.000}. intensity: {before:0.000} -> {after:0.000}");
        }
        else
        {
            float amt = reducePerSecond * Time.deltaTime;
            float before = b.intensity;
            b.ReduceFire(amt);
            float after = b.intensity;

            if (debugLogs) Debug.Log($"[SprayTrigger] Reduced '{other.gameObject.name}' by {amt:0.000}. intensity: {before:0.000} -> {after:0.000}");
        }
    }
}