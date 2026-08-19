using UnityEngine;

namespace DualSouls.Core
{
    public class ThirdPersonCameraFollow : MonoBehaviour
    {
        public Transform target;

        [Header("Position")]
        public Vector3 offset = new Vector3(0f, 3f, -6f);
        public float followSmoothness = 10f;

        [Header("Mouse Look")]
        public bool useMouseLook = true;
        public float mouseSensitivity = 2.5f;
        public float minPitch = -20f;
        public float maxPitch = 60f;

        private float yaw;
        private float pitch = 20f;

        private void Start()
        {
            if (target != null)
                yaw = target.eulerAngles.y;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            if (useMouseLook)
            {
                yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            }

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desiredPosition = target.position + rotation * offset;

            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSmoothness * Time.deltaTime);
            transform.LookAt(target.position + Vector3.up * 1.4f);
        }
    }
}
