using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private float _mouseSensity = 3f;
    [SerializeField] private Transform _controller;

    private float _xRotation = 0f;

    private void Start()
    {
        // Lock cursor to the center of the screen
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (_controller == null) return;

        // Chỉ xoay camera khi con trỏ chuột bị khóa (đang chơi game bình thường)
        if (Cursor.lockState != CursorLockMode.Locked) return;

        // Get mouse inputs
        float mouseX = Input.GetAxis("Mouse X") * _mouseSensity;
        float mouseY = Input.GetAxis("Mouse Y") * _mouseSensity;

        // Rotate camera vertically (clamped between -90 and 90 degrees)
        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        // Rotate player controller body horizontally
        _controller.Rotate(Vector3.up * mouseX);
    }
}
