using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IKFootSolver : MonoBehaviour
{
    public bool isMovingForward;
    [SerializeField] LayerMask terrainLayer = default;
    [SerializeField] Transform body = default;
    [SerializeField] IKFootSolver otherFoot = default;
    [SerializeField] float speed = 4;
    [SerializeField] float stepDistance = .2f;
    [SerializeField] float stepLength = .2f;
    [SerializeField] float sideStepLength = .1f;
    [SerializeField] float stepHeight = .3f;
    [SerializeField] Vector3 footOffset = default;
    public Vector3 footRotOffset;
    public float footYPosOffset = 0.1f;
    public float rayStartYOffset = 0;
    public float rayLength = 1.5f;
    
    [Header("Stability Settings")]
    [SerializeField] float stepThreshold = 0.25f; // Minimum distance before considering a new step
    [SerializeField] float bodyVelocityThreshold = 0.1f; // Minimum body movement to trigger steps
    
    float footSpacing;
    Vector3 oldPosition, currentPosition, newPosition;
    Vector3 oldNormal, currentNormal, newNormal;
    Vector3 lastBodyPosition;
    float lerp;
    
    private void Start()
    {
        footSpacing = transform.localPosition.x;
        currentPosition = newPosition = oldPosition = transform.position;
        currentNormal = newNormal = oldNormal = transform.up;
        lastBodyPosition = body.position;
        lerp = 1;
    }
    
    void Update()
    {
        transform.position = currentPosition + Vector3.up * footYPosOffset;
        transform.localRotation = Quaternion.Euler(footRotOffset);
        
        // Calculate ray start position more accurately
        Vector3 rayStart = body.position + (body.right * footSpacing) + Vector3.up * rayStartYOffset;
        Ray ray = new Ray(rayStart, Vector3.down);
        Debug.DrawRay(rayStart, Vector3.down * rayLength, Color.blue);
        
        if (Physics.Raycast(ray, out RaycastHit info, rayLength, terrainLayer))
        {
            // Check if body has moved significantly
            float bodyMovement = Vector3.Distance(body.position, lastBodyPosition);
            bool bodyIsMoving = bodyMovement > bodyVelocityThreshold * Time.deltaTime;
            
            // Calculate target position with proper offset
            Vector3 targetPoint = info.point + Vector3.ProjectOnPlane(footOffset, info.normal);
            
            // Use a more stable distance check
            float distanceToTarget = Vector3.Distance(currentPosition, targetPoint);
            
            // Only step if:
            // 1. Distance is significant enough
            // 2. Other foot is not moving
            // 3. Current foot is not already moving
            // 4. Body is actually moving (optional - remove if you want stepping in place)
            if (distanceToTarget > stepThreshold && 
                !otherFoot.IsMoving() && 
                lerp >= 1 && 
                bodyIsMoving)
            {
                StartStep(info, targetPoint);
            }
        }
        
        UpdateStepAnimation();
        lastBodyPosition = body.position;
    }
    
    private void StartStep(RaycastHit info, Vector3 targetPoint)
    {
        lerp = 0;
        Vector3 direction = Vector3.ProjectOnPlane(targetPoint - currentPosition, Vector3.up).normalized;
        float angle = Vector3.Angle(body.forward, direction);
        isMovingForward = angle < 50 || angle > 130;
        
        if (isMovingForward)
        {
            newPosition = targetPoint + direction * stepLength;
        }
        else
        {
            newPosition = targetPoint + direction * sideStepLength;
        }
        
        newNormal = info.normal;
    }
    
    private void UpdateStepAnimation()
    {
        if (lerp < 1)
        {
            Vector3 tempPosition = Vector3.Lerp(oldPosition, newPosition, lerp);
            tempPosition.y += Mathf.Sin(lerp * Mathf.PI) * stepHeight;
            currentPosition = tempPosition;
            currentNormal = Vector3.Lerp(oldNormal, newNormal, lerp);
            lerp += Time.deltaTime * speed;
        }
        else
        {
            oldPosition = newPosition;
            oldNormal = newNormal;
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(newPosition, 0.1f);
        
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentPosition, 0.05f);
            
            // Draw step threshold
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(currentPosition, stepThreshold);
        }
    }
    
    public bool IsMoving()
    {
        return lerp < 1;
    }
}