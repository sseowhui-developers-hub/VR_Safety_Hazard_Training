using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FireExtinguisherHoseLock : MonoBehaviour
{
    [Header("References")]
    public FireExtinguisherPinInputSystem pinSystem; // drag your FireExtinguisherPinInputSystem here
    public XRGrabInteractable hoseGrab; // assign your hose/nozzle XRGrabInteractable here

    void Start()
    {
        if (hoseGrab != null)
        {
            hoseGrab.enabled = false; // locked at start
        }
    }

    void Update()
    {
        if (pinSystem == null || hoseGrab == null) return;

        // Unlock when pin is pulled
        if (pinSystem.isPinPulled && !hoseGrab.enabled)
        {
            hoseGrab.enabled = true;
            Debug.Log("Hose unlocked - now grabbable");
        }
    }
}
