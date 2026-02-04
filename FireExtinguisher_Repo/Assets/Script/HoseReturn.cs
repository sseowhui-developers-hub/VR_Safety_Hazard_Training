using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HoseReturn : MonoBehaviour
{
    [Header("References")]
    public XRGrabInteractable grabInteractable;
    public Transform defaultRestPoint; // empty GameObject on extinguisher where nozzle should return

    [Header("Return Settings")]
    public float returnSpeed = 5f; // how fast it moves back
    public float returnRotationSpeed = 5f; // how fast it rotates back

    private bool isReturning = false;

    void Awake()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();

        // Subscribe to grab/release events
        grabInteractable.selectEntered.AddListener(_ => OnGrabbed());
        grabInteractable.selectExited.AddListener(_ => OnReleased());
    }

    void OnGrabbed()
    {
        isReturning = false; // stop returning while grabbed
    }

    void OnReleased()
    {
        isReturning = true; // start returning when released
    }

    void Update()
    {
        if (isReturning && defaultRestPoint != null)
        {
            // Smoothly move back
            transform.position = Vector3.Lerp(transform.position, defaultRestPoint.position, Time.deltaTime * returnSpeed);

            // Smoothly rotate back
            transform.rotation = Quaternion.Slerp(transform.rotation, defaultRestPoint.rotation, Time.deltaTime * returnRotationSpeed);

            // Stop if close enough
            if (Vector3.Distance(transform.position, defaultRestPoint.position) < 0.01f)
            {
                transform.position = defaultRestPoint.position;
                transform.rotation = defaultRestPoint.rotation;
                isReturning = false;
            }
        }
    }
}
