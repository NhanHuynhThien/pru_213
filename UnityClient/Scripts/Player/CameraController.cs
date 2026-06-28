using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTransform;
    public Vector3 offset = new Vector3(0f, 4f, -6f);

    [Header("Follow Settings")]
    public float followSpeed = 8f;
    public float rotationSpeed = 5f;

    [Header("Orbit Settings")]
    public float orbitSpeed = 3f;
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;
    private float currentYaw = 0f;
    private float currentPitch = 20f;

    [Header("Collision")]
    public LayerMask collisionMask = ~0;
    public float collisionRadius = 0.3f;
    public float clipOffset = 0.3f;

    [Header("Look At")]
    public Vector3 lookAtOffset = new Vector3(0f, 1.5f, 0f);

    private Vector3 currentVelocity;
    private Vector3 currentPosition;

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        if (playerTransform != null)
        {
            currentPosition = playerTransform.position + offset;
            transform.position = currentPosition;
            transform.LookAt(playerTransform.position + lookAtOffset);
        }
    }

    void LateUpdate()
    {
        if (playerTransform == null) return;

        float mouseX = Input.GetAxisRaw("Mouse X") * orbitSpeed;
        float mouseY = Input.GetAxisRaw("Mouse Y") * orbitSpeed;

        currentYaw += mouseX;
        currentPitch -= mouseY;
        currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
        Vector3 targetOffset = rotation * new Vector3(0f, 0f, -1f) * offset.magnitude;
        targetOffset.y = offset.y;

        Vector3 targetPosition = playerTransform.position + targetOffset;

        RaycastHit hit;
        Vector3 toTarget = (targetPosition - playerTransform.position).normalized;
        float targetDist = Vector3.Distance(playerTransform.position, targetPosition);

        if (Physics.SphereCast(playerTransform.position + Vector3.up * 0.5f, collisionRadius, toTarget, out hit, targetDist, collisionMask))
        {
            Vector3 correctedPos = playerTransform.position + toTarget * (hit.distance - clipOffset);
            correctedPos.y = targetPosition.y;
            targetPosition = correctedPos;
        }

        currentPosition = Vector3.SmoothDamp(currentPosition, targetPosition, ref currentVelocity, 1f / followSpeed);
        transform.position = currentPosition;

        Vector3 lookTarget = playerTransform.position + lookAtOffset;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookTarget - currentPosition), rotationSpeed * Time.deltaTime);
    }
}
