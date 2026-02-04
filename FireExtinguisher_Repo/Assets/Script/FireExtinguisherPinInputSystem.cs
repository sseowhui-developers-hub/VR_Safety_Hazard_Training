using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem;

public class FireExtinguisherPinInputSystem : MonoBehaviour
{
    [Header("Pin Settings")]
    public GameObject pin;
    public Transform pinPulledPosition;
    public float pullDuration = 0.5f;
    public bool isPinPulled = false;

    [Header("Input - Pin Pull (Non-Grabbing Hand)")]
    public InputActionReference leftTriggerAction;
    public InputActionReference rightTriggerAction;

    [Header("Input - Spray Control (Grabbing Hand)")]
    public InputActionReference leftPrimaryPressAction;
    public InputActionReference rightPrimaryPressAction;

    [Header("Spray Settings")]
    public GameObject sprayParticleSystem;
    public PutOutFire putOutFire; // Changed from SprayTrigger to PutOutFire
    public AudioClip spraySound;
    public AudioClip spraySoundLoop;
    private AudioSource sprayAudioSource;
    private ParticleSystem sprayPS;
    private bool isSprayActive = false;

    [Header("Audio")]
    public AudioClip pinPullSound;
    private AudioSource audioSource;

    private XRGrabInteractable grabInteractable;
    private Vector3 originalPinPosition;
    private Quaternion originalPinRotation;
    private bool isGrabbed = false;
    private XRBaseInteractor currentGrabbingHand; // Track which hand is grabbing

    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        audioSource = GetComponent<AudioSource>();

        // Set up spray particle system
        if (sprayParticleSystem != null)
        {
            sprayParticleSystem.SetActive(false);
            sprayPS = sprayParticleSystem.GetComponent<ParticleSystem>();

            // Add separate audio source for spray sounds
            sprayAudioSource = sprayParticleSystem.GetComponent<AudioSource>();
            if (sprayAudioSource == null)
            {
                sprayAudioSource = sprayParticleSystem.AddComponent<AudioSource>();
            }
            sprayAudioSource.loop = true;
            sprayAudioSource.playOnAwake = false;

            // Auto-assign PutOutFire if not manually assigned
            if (putOutFire == null)
            {
                putOutFire = sprayParticleSystem.GetComponent<PutOutFire>();
                if (putOutFire != null)
                {
                    Debug.Log("PutOutFire component found automatically");
                }
                else
                {
                    Debug.LogWarning("No PutOutFire component found on spray particle system!");
                }
            }
        }

        if (pin != null)
        {
            originalPinPosition = pin.transform.position;
            originalPinRotation = pin.transform.rotation;
        }

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }

        // Subscribe to trigger input for pin pulling (both hands - non-grabbing hand will pull)
        if (leftTriggerAction != null)
        {
            leftTriggerAction.action.performed += OnTriggerPressed;
        }

        if (rightTriggerAction != null)
        {
            rightTriggerAction.action.performed += OnTriggerPressed;
        }

        // Subscribe to UI Press input for spray control (only the grabbing hand will work)
        if (leftPrimaryPressAction != null)
        {
            leftPrimaryPressAction.action.started += OnUIPressStarted;
            leftPrimaryPressAction.action.canceled += OnUIPressCanceled;
        }

        if (rightPrimaryPressAction != null)
        {
            rightPrimaryPressAction.action.started += OnUIPressStarted;
            rightPrimaryPressAction.action.canceled += OnUIPressCanceled;
        }
    }

    void OnTriggerPressed(InputAction.CallbackContext context)
    {
        // Pin can only be pulled by the NON-grabbing hand when fire extinguisher is grabbed
        if (isGrabbed && !isPinPulled)
        {
            // Check if the trigger press is from the non-grabbing hand
            bool isFromNonGrabbingHand = IsInputFromNonGrabbingHand(context, true);

            if (isFromNonGrabbingHand)
            {
                PullPin();
            }
        }
    }

    void OnUIPressStarted(InputAction.CallbackContext context)
    {
        // Spray can only be controlled by the GRABBING hand
        if (isGrabbed && isPinPulled && !isSprayActive)
        {
            // Check if the UI press is from the grabbing hand
            bool isFromGrabbingHand = IsInputFromGrabbingHand(context);

            if (isFromGrabbingHand)
            {
                StartSpray();
            }
        }
    }

    void OnUIPressCanceled(InputAction.CallbackContext context)
    {
        // Only stop spray if it was the grabbing hand that released
        if (isSprayActive)
        {
            bool isFromGrabbingHand = IsInputFromGrabbingHand(context);

            if (isFromGrabbingHand)
            {
                StopSpray();
            }
        }
    }

    // Helper method to check if input is from the grabbing hand
    bool IsInputFromGrabbingHand(InputAction.CallbackContext context)
    {
        if (currentGrabbingHand == null) return false;

        // Check if the input action matches the grabbing hand
        string grabbingHandName = currentGrabbingHand.name.ToLower();

        if (grabbingHandName.Contains("left"))
        {
            return context.action == leftPrimaryPressAction?.action;
        }
        else if (grabbingHandName.Contains("right"))
        {
            return context.action == rightPrimaryPressAction?.action;
        }

        return false;
    }

    // Helper method to check if input is from the non-grabbing hand
    bool IsInputFromNonGrabbingHand(InputAction.CallbackContext context, bool isTrigger)
    {
        if (currentGrabbingHand == null) return false;

        // Check if the input action is from the opposite hand
        string grabbingHandName = currentGrabbingHand.name.ToLower();

        if (grabbingHandName.Contains("left"))
        {
            // Left hand is grabbing, so right hand should pull pin
            return isTrigger ? context.action == rightTriggerAction?.action : context.action == rightPrimaryPressAction?.action;
        }
        else if (grabbingHandName.Contains("right"))
        {
            // Right hand is grabbing, so left hand should pull pin
            return isTrigger ? context.action == leftTriggerAction?.action : context.action == leftPrimaryPressAction?.action;
        }

        return false;
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        currentGrabbingHand = args.interactorObject as XRBaseInteractor;

        string handName = currentGrabbingHand != null ? currentGrabbingHand.name : "unknown";

        if (!isPinPulled)
        {
            Debug.Log($"Fire extinguisher grabbed by {handName} - use OTHER hand trigger to pull pin");
        }
        else
        {
            Debug.Log($"Fire extinguisher grabbed by {handName} - hold {handName} UI Press to spray");
        }
    }

    void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;
        currentGrabbingHand = null;

        if (isSprayActive)
        {
            StopSpray();
        }
    }

    public void PullPin()
    {
        if (isPinPulled || pin == null) return;

        isPinPulled = true;

        if (audioSource != null && pinPullSound != null)
        {
            audioSource.PlayOneShot(pinPullSound);
        }

        StartCoroutine(AnimatePinPull());
        Debug.Log("Fire extinguisher pin pulled! Now use grabbing hand UI Press to spray.");
    }

    void StartSpray()
    {
        if (!isGrabbed || !isPinPulled || isSprayActive) return;

        isSprayActive = true;

        if (sprayParticleSystem != null)
        {
            sprayParticleSystem.SetActive(true);
            if (sprayPS != null)
            {
                sprayPS.Play();
            }
        }

        // Start PutOutFire functionality
        if (putOutFire != null)
        {
            putOutFire.StartSpray();
            Debug.Log("PutOutFire.StartSpray() called successfully");
        }
        else
        {
            Debug.LogError("PutOutFire component is null! Cannot start fire detection.");
        }

        // Play spray audio
        if (sprayAudioSource != null)
        {
            if (spraySound != null)
            {
                sprayAudioSource.PlayOneShot(spraySound);
            }
            if (spraySoundLoop != null)
            {
                sprayAudioSource.clip = spraySoundLoop;
                sprayAudioSource.Play();
            }
        }

        Debug.Log("Fire extinguisher spraying!");
    }

    void StopSpray()
    {
        if (!isSprayActive) return;

        isSprayActive = false;

        if (sprayPS != null)
        {
            sprayPS.Stop();
        }

        // Stop PutOutFire functionality
        if (putOutFire != null)
        {
            putOutFire.StopSpray();
            Debug.Log("PutOutFire.StopSpray() called successfully");
        }

        // Stop spray audio
        if (sprayAudioSource != null)
        {
            sprayAudioSource.Stop();
        }

        // Deactivate spray GameObject after particles finish
        if (sprayParticleSystem != null)
        {
            StartCoroutine(DeactivateSprayAfterParticles());
        }

        Debug.Log("Fire extinguisher spray stopped.");
    }

    System.Collections.IEnumerator DeactivateSprayAfterParticles()
    {
        // Wait for particles to finish before deactivating
        if (sprayPS != null)
        {
            yield return new WaitForSeconds(sprayPS.main.startLifetime.constantMax);
        }
        else
        {
            yield return new WaitForSeconds(2f); // Default wait time
        }

        if (sprayParticleSystem != null && !isSprayActive)
        {
            sprayParticleSystem.SetActive(false);
        }
    }

    System.Collections.IEnumerator AnimatePinPull()
    {
        float elapsed = 0f;
        Vector3 startPos = pin.transform.position;
        Quaternion startRot = pin.transform.rotation;

        Vector3 targetPos = pinPulledPosition != null ? pinPulledPosition.position : startPos + Vector3.up * 0.1f;
        Quaternion targetRot = pinPulledPosition != null ? pinPulledPosition.rotation : startRot;

        while (elapsed < pullDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pullDuration;

            pin.transform.position = Vector3.Lerp(startPos, targetPos, t);
            pin.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);

            yield return null;
        }

        // Make pin fall with physics
        Rigidbody pinRb = pin.GetComponent<Rigidbody>();
        if (pinRb == null)
        {
            pinRb = pin.AddComponent<Rigidbody>();
        }
        pinRb.isKinematic = false;
    }

    void OnEnable()
    {
        // Enable pin pull inputs
        if (leftTriggerAction != null && leftTriggerAction.action != null)
            leftTriggerAction.action.Enable();

        if (rightTriggerAction != null && rightTriggerAction.action != null)
            rightTriggerAction.action.Enable();

        // Enable spray control inputs
        if (leftPrimaryPressAction != null && leftPrimaryPressAction.action != null)
            leftPrimaryPressAction.action.Enable();

        if (rightPrimaryPressAction != null && rightPrimaryPressAction.action != null)
            rightPrimaryPressAction.action.Enable();
    }

    void OnDisable()
    {
        // Disable pin pull inputs
        if (leftTriggerAction != null && leftTriggerAction.action != null)
            leftTriggerAction.action.Disable();

        if (rightTriggerAction != null && rightTriggerAction.action != null)
            rightTriggerAction.action.Disable();

        // Disable spray control inputs
        if (leftPrimaryPressAction != null && leftPrimaryPressAction.action != null)
            leftPrimaryPressAction.action.Disable();

        if (rightPrimaryPressAction != null && rightPrimaryPressAction.action != null)
            rightPrimaryPressAction.action.Disable();

        // Stop spray if active
        if (isSprayActive)
        {
            StopSpray();
        }
    }

    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }

        // Unsubscribe from pin pull inputs
        if (leftTriggerAction != null)
        {
            leftTriggerAction.action.performed -= OnTriggerPressed;
        }

        if (rightTriggerAction != null)
        {
            rightTriggerAction.action.performed -= OnTriggerPressed;
        }

        // Unsubscribe from spray control inputs
        if (leftPrimaryPressAction != null)
        {
            leftPrimaryPressAction.action.started -= OnUIPressStarted;
            leftPrimaryPressAction.action.canceled -= OnUIPressCanceled;
        }

        if (rightPrimaryPressAction != null)
        {
            rightPrimaryPressAction.action.started -= OnUIPressStarted;
            rightPrimaryPressAction.action.canceled -= OnUIPressCanceled;
        }
    }
}