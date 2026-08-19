using UnityEngine;

namespace DualSouls.Core
{
    public class FirstPersonCameraController : MonoBehaviour
    {
        [Header("References")]
        public Transform playerBody;
        public Transform cameraHolder;

        [Header("Mouse Look")]
        public float mouseSensitivity = 2.5f;
        public float minLookAngle = -80f;
        public float maxLookAngle = 80f;

        [Header("Cursor")]
        public bool lockCursorOnStart = true;

        private float xRotation;

        private void Start()
        {
            if (cameraHolder == null)
                cameraHolder = transform;

            if (lockCursorOnStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void Update()
        {
            HandleMouseLook();
            HandleCursorUnlock();
        }

        private void HandleMouseLook()
        {
            if (playerBody == null)
                return;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Olhar para cima/baixo
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, minLookAngle, maxLookAngle);

            cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // Girar corpo do player para esquerda/direita
            playerBody.Rotate(Vector3.up * mouseX);
        }

        private void HandleCursorUnlock()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}