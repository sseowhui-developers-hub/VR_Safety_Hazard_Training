using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HoseHingeJointController : MonoBehaviour
{
    [Header("References")]
    public FireExtinguisherPinInputSystem fireExtinguisher;
    public Transform fireExtinguisherBody; // The main fire extinguisher body
    public Transform hingeAnchorPoint; // Where the hose connects (socket position)

    [Header("Hinge Joint Settings")]
    [Range(0f, 180f)]
    public float maxAngle = 90f; // Maximum swing angle
    public bool useLimits = true;
    public bool useSpring = true;
    [Range(0f, 1000f)]
    public float springForce = 100f;
    [Range(0f, 100f)]
    public float damper = 10f;

    [Header("Stability Settings")]
    public bool stabilizeConnectedBody = true; // Keep fire extinguisher stable
    public float connectedBodyMassMultiplier = 5f; // Make fire extinguisher heavier when hose is grabbed

    [Header("Debug")]
    public bool showDebugInfo = true;

    private HingeJoint hingeJoint;
    private XRGrabInteractable grabInteractable;
    private Rigidbody hoseRigidbody;
    private Rigidbody fireExtinguisherRigidbody;
    private float originalConnectedBodyMass;
    private float originalConnectedBodyDrag;
    private float originalConnectedBodyAngularDrag;
    private bool isGrabbed = false;

    void Start()
    {
        // Get components
        hoseRigidbody = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
        fireExtinguisherRigidbody = fireExtinguisherBody.GetComponent<Rigidbody>();

        if (!ValidateSetup()) return;

        // Store original connected body properties
        originalConnectedBodyMass = fireExtinguisherRigidbody.mass;
        originalConnectedBodyDrag = fireExtinguisherRigidbody.drag;
        originalConnectedBodyAngularDrag = fireExtinguisherRigidbody.angularDrag;

        // Create hinge joint
        CreateHingeJoint();

        // Setup grab events
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);

            // Initially disable until pin is pulled
            grabInteractable.enabled = false;
        }

        if (showDebugInfo)
        {
            Debug.Log($"Hose Hinge Joint setup complete. Connected to: {fireExtinguisherRigidbody.name}");
        }
    }

    void Update()
    {
        // Enable hose grabbing when pin is pulled
        if (fireExtinguisher != null && fireExtinguisher.isPinPulled && !grabInteractable.enabled)
        {
            grabInteractable.enabled = true;
            if (showDebugInfo) Debug.Log("Hose can now be grabbed - pin pulled!");
        }
    }

    bool ValidateSetup()
    {
        if (hoseRigidbody == null)
        {
            Debug.LogError("Hose needs a Rigidbody component!");
            return false;
        }

        if (fireExtinguisherBody == null)
        {
            Debug.LogError("Fire Extinguisher Body reference missing!");
            return false;
        }

        if (fireExtinguisherRigidbody == null)
        {
            Debug.LogError("Fire Extinguisher Body needs a Rigidbody component!");
            return false;
        }

        if (hingeAnchorPoint == null)
        {
            Debug.LogWarning("No hinge anchor point set, using hose position");
            hingeAnchorPoint = transform;
        }

        return true;
    }

    void CreateHingeJoint()
    {
        // Remove existing hinge joint if any
        if (hingeJoint != null)
        {
            DestroyImmediate(hingeJoint);
        }

        // Create hinge joint on the hose
        hingeJoint = gameObject.AddComponent<HingeJoint>();

        // Connect to fire extinguisher
        hingeJoint.connectedBody = fireExtinguisherRigidbody;

        // Set anchor points
        Vector3 localAnchor = transform.InverseTransformPoint(hingeAnchorPoint.position);
        hingeJoint.anchor = localAnchor;

        Vector3 connectedAnchor = fireExtinguisherBody.InverseTransformPoint(hingeAnchorPoint.position);
        hingeJoint.connectedAnchor = connectedAnchor;

        // Set hinge axis (usually the axis the hose rotates around)
        // For a fire extinguisher, this is typically the forward axis of the socket
        hingeJoint.axis = Vector3.right; // Adjust based on your model orientation

        // Configure limits
        if (useLimits)
        {
            hingeJoint.useLimits = true;
            JointLimits limits = new JointLimits();
            limits.min = -maxAngle;
            limits.max = maxAngle;
            limits.bounciness = 0.1f;
            hingeJoint.limits = limits;
        }

        // Configure spring (keeps hose in default position)
        if (useSpring)
        {
            hingeJoint.useSpring = true;
            JointSpring spring = new JointSpring();
            spring.spring = springForce;
            spring.damper = damper;
            spring.targetPosition = 0f; // Default position
            hingeJoint.spring = spring;
        }

        // Set break forces (make it strong so it doesn't break easily)
        hingeJoint.breakForce = Mathf.Infinity;
        hingeJoint.breakTorque = Mathf.Infinity;

        if (showDebugInfo)
        {
            Debug.Log($"Hinge Joint created with limits: +/-{maxAngle} degrees, Spring: {springForce}, Damper: {damper}");
        }
    }

    void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;

        if (stabilizeConnectedBody)
        {
            StabilizeConnectedBody();
        }

        // Reduce spring force when grabbed for easier manipulation
        if (hingeJoint != null && hingeJoint.useSpring)
        {
            JointSpring spring = hingeJoint.spring;
            spring.spring = springForce * 0.3f; // Reduce to 30% when grabbed
            hingeJoint.spring = spring;
        }

        if (showDebugInfo)
        {
            Debug.Log("Hose grabbed - Connected body stabilized, spring force reduced");
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;

        if (stabilizeConnectedBody)
        {
            RestoreConnectedBody();
        }

        // Restore full spring force
        if (hingeJoint != null && hingeJoint.useSpring)
        {
            JointSpring spring = hingeJoint.spring;
            spring.spring = springForce; // Restore full spring force
            hingeJoint.spring = spring;
        }

        if (showDebugInfo)
        {
            Debug.Log("Hose released - Connected body restored, spring force restored");
        }
    }

    void StabilizeConnectedBody()
    {
        // Increase mass to make fire extinguisher more stable
        fireExtinguisherRigidbody.mass = originalConnectedBodyMass * connectedBodyMassMultiplier;

        // Increase drag to reduce unwanted movement
        fireExtinguisherRigidbody.drag = originalConnectedBodyDrag + 5f;
        fireExtinguisherRigidbody.angularDrag = originalConnectedBodyAngularDrag + 10f;

        // Optionally freeze certain axes
        // fireExtinguisherRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    void RestoreConnectedBody()
    {
        // Restore original properties
        fireExtinguisherRigidbody.mass = originalConnectedBodyMass;
        fireExtinguisherRigidbody.drag = originalConnectedBodyDrag;
        fireExtinguisherRigidbody.angularDrag = originalConnectedBodyAngularDrag;

        // Remove constraints
        // fireExtinguisherRigidbody.constraints = RigidbodyConstraints.None;
    }

    // Public methods for external control
    public void SetHingeAxis(Vector3 axis)
    {
        if (hingeJoint != null)
        {
            hingeJoint.axis = axis;
            if (showDebugInfo) Debug.Log($"Hinge axis set to: {axis}");
        }
    }

    public void SetLimits(float minAngle, float maxAngle)
    {
        if (hingeJoint != null)
        {
            JointLimits limits = hingeJoint.limits;
            limits.min = minAngle;
            limits.max = maxAngle;
            hingeJoint.limits = limits;

            if (showDebugInfo) Debug.Log($"Hinge limits set to: {minAngle} to {maxAngle} degrees");
        }
    }

    public float GetCurrentAngle()
    {
        return hingeJoint != null ? hingeJoint.angle : 0f;
    }

    public bool IsAtLimit()
    {
        if (hingeJoint == null || !hingeJoint.useLimits) return false;

        float currentAngle = hingeJoint.angle;
        JointLimits limits = hingeJoint.limits;

        return currentAngle <= limits.min + 1f || currentAngle >= limits.max - 1f;
    }

    void OnDrawGizmos()
    {
        if (hingeAnchorPoint == null) return;

        // Draw hinge connection
        Gizmos.color = isGrabbed ? Color.green : Color.blue;
        Gizmos.DrawLine(transform.position, hingeAnchorPoint.position);

        // Draw anchor point
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(hingeAnchorPoint.position, 0.05f);

        // Draw hinge axis
        if (hingeJoint != null)
        {
            Vector3 worldAxis = transform.TransformDirection(hingeJoint.axis);
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(hingeAnchorPoint.position, worldAxis * 0.2f);
        }

        // Draw angle limits
        if (useLimits && hingeJoint != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            // This is a simplified visualization - you might need to adjust based on your setup
            Vector3 minDir = Quaternion.AngleAxis(-maxAngle, right) * forward;
            Vector3 maxDir = Quaternion.AngleAxis(maxAngle, right) * forward;

            Gizmos.DrawRay(hingeAnchorPoint.position, minDir * 0.3f);
            Gizmos.DrawRay(hingeAnchorPoint.position, maxDir * 0.3f);
        }
    }

    void OnJointBreak(float breakForce)
    {
        if (showDebugInfo)
        {
            Debug.LogWarning($"Hinge joint broken with force: {breakForce}");
        }

        // Optionally recreate the joint
        // CreateHingeJoint();
    }

    void OnDestroy()
    {
        // Clean up events
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        // Restore connected body properties if they were modified
        if (fireExtinguisherRigidbody != null && isGrabbed)
        {
            RestoreConnectedBody();
        }
    }
}