using UnityEngine;
using System.Collections;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Referências")]
    public Transform player;
    public Transform verticalPivot;
    public Camera cam;

    [Header("Follow")]
    public float smoothSpeed = 10f;
    public Vector3 playerOffset = new Vector3(0, 1.5f, 0);

    [Header("Rotação")]
    public float mouseSensitivity = 200f;
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;

    private float horizontalRotation;
    private float verticalRotation;

    [Header("FOV")]
    public float normalFOV = 60f;
    public float sprintFOV = 75f;
    public float fovSmooth = 5f;

    [Header("Tilt")]
    public float maxTilt = 5f;
    public float tiltSmooth = 5f;
    private float currentTilt;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        FollowPlayer();
        RotateCamera();
        HandleFOV();
        HandleTilt();

        // Teste de camera shake.
        // Aperte F para testar.
        if (Input.GetKeyDown(KeyCode.F))
        {
            StartCoroutine(CameraShake(0.2f, 0.2f));
        }
    }

    void FollowPlayer()
    {
        Vector3 targetPosition = player.position + playerOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }

    void RotateCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        horizontalRotation += mouseX;
        verticalRotation -= mouseY;

        verticalRotation = Mathf.Clamp(
            verticalRotation,
            minVerticalAngle,
            maxVerticalAngle
        );

        // Gira horizontalmente ao redor do player.
        transform.rotation = Quaternion.Euler(
            0f,
            horizontalRotation,
            0f
        );

        // Gira verticalmente ao redor do player.
        verticalPivot.localRotation = Quaternion.Euler(
            verticalRotation,
            0f,
            currentTilt
        );
    }

    void HandleFOV()
    {
        float targetFOV = Input.GetKey(KeyCode.LeftShift)
            ? sprintFOV
            : normalFOV;

        cam.fieldOfView = Mathf.Lerp(
            cam.fieldOfView,
            targetFOV,
            fovSmooth * Time.deltaTime
        );
    }

    void HandleTilt()
    {
        float horizontalInput = Input.GetAxis("Horizontal");

        float targetTilt = -horizontalInput * maxTilt;

        currentTilt = Mathf.Lerp(
            currentTilt,
            targetTilt,
            tiltSmooth * Time.deltaTime
        );
    }

    public IEnumerator CameraShake(float duration, float magnitude)
    {
        Vector3 originalPosition = cam.transform.localPosition;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cam.transform.localPosition = originalPosition + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cam.transform.localPosition = originalPosition;
    }
}
