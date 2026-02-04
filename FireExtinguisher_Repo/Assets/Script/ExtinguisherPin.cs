using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ExtinguisherPin : MonoBehaviour
{
    public XRGrabInteractable grabInteractable;
    public Transform lockedPosition;
    public float unlockDistance = 0.1f;
    public bool isUnlocked = false;

    private void Awake()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void Update()
    {
        if (!isUnlocked)
        {
            float dist = Vector3.Distance(transform.position, lockedPosition.position);
            if (dist > unlockDistance)
            {
                UnlockPin();
            }
        }
    }

    void UnlockPin()
    {
        isUnlocked = true;
        Debug.Log("Pin removed! Extinguisher is now active.");
        // Optionally: notify extinguisher script that it can now spray
        GetComponent<Rigidbody>().isKinematic = false;
    }
}
