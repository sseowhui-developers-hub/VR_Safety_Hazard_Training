using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class HoseController : MonoBehaviour
{
    [Header("References")]
    public FireExtinguisherPinInputSystem fireExtinguisher;
    public Transform hoseSocket;

    [Header("Settings")]
    public float returnSpeed = 2f;
    public float returnDelay = 1f;

    private XRGrabInteractable grabInteractable;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private Transform originalParent;
    private bool isGrabbed = false;
    private bool isSocketed = true;
    private Coroutine returnCoroutine;

    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();

        // Store original parent (should be the fire extinguisher)
        originalParent = transform.parent;

        // If hoseSocket is assigned, position the hose at the socket initially
        if (hoseSocket != null)
        {
            // Make sure the hose is a child of the fire extinguisher
            if (originalParent == null)
            {
                transform.SetParent(hoseSocket.parent);
            }

            // Position hose at socket
            SocketHose();
        }
        else
        {
            // Store current local position as default socket position
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
        }

        // Initially disable grabbing until pin is pulled
        grabInteractable.enabled = false;

        // Setup events
        grabInteractable.selectEntered.AddListener(OnGrabbed);
        grabInteractable.selectExited.AddListener(OnReleased);
    }

    void Update()
    {
        // Enable hose grabbing when pin is pulled
        if (fireExtinguisher != null && fireExtinguisher.isPinPulled && !grabInteractable.enabled)
        {
            grabInteractable.enabled = true;
            Debug.Log("Hose can now be grabbed - pin has been pulled!");
        }
    }

    void SocketHose()
    {
        if (hoseSocket != null)
        {
            // Position hose at socket
            transform.position = hoseSocket.position;
            transform.rotation = hoseSocket.rotation;

            // Store these as local coordinates relative to parent
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;
        }

        isSocketed = true;
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        isSocketed = false;

        // Stop any return coroutine
        if (returnCoroutine != null)
        {
            StopCoroutine(returnCoroutine);
            returnCoroutine = null;
        }

        // Remove from parent so it can be moved independently
        transform.SetParent(null);

        Debug.Log("Hose grabbed and detached from fire extinguisher");
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;

        // Start return to socket coroutine
        returnCoroutine = StartCoroutine(ReturnToSocket());
    }

    IEnumerator ReturnToSocket()
    {
        yield return new WaitForSeconds(returnDelay);

        if (isGrabbed || isSocketed) yield break; // Don't return if grabbed again or already socketed

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        // Calculate target position (socket position in world space)
        Vector3 targetPos;
        Quaternion targetRot;

        if (hoseSocket != null)
        {
            targetPos = hoseSocket.position;
            targetRot = hoseSocket.rotation;
        }
        else if (originalParent != null)
        {
            // Use original local position relative to parent
            targetPos = originalParent.TransformPoint(originalLocalPosition);
            targetRot = originalParent.rotation * originalLocalRotation;
        }
        else
        {
            yield break;
        }

        float duration = Vector3.Distance(startPos, targetPos) / returnSpeed;
        float elapsed = 0f;

        while (elapsed < duration && !isGrabbed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            transform.rotation = Quaternion.Lerp(startRot, targetRot, t);

            yield return null;
        }

        if (!isGrabbed)
        {
            // Final positioning and re-parenting
            if (originalParent != null)
            {
                transform.SetParent(originalParent);
            }

            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;

            isSocketed = true;
            Debug.Log("Hose returned to socket and reattached to fire extinguisher");
        }
    }

    void OnDestroy()
    {
        // Clean up events
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }
}