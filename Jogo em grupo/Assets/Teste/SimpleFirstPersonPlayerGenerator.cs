using UnityEngine;

public class SimpleFirstPersonPlayerGenerator : MonoBehaviour
{
    [Header("Geração")]
    public bool gerarAoIniciar = true;
    public bool substituirPlayerExistente = true;

    [Header("Spawn")]
    public string nomePlayer = "Player_POV";
    public Vector3 posicaoInicial = new Vector3(-24f, 1f, -10f);

    [Header("Estrutura do Player")]
    public float alturaPlayer = 1.8f;
    public float raioPlayer = 0.35f;
    public float alturaOlhos = 1.62f;

    [ContextMenu("Gerar Player FPS")]
    public void GerarPlayerFPS()
    {
        if (substituirPlayerExistente)
        {
            RemoverExistente(nomePlayer);
            RemoverExistente("FPS_Camera");
        }

        GameObject player = GameObject.Find(nomePlayer);

        if (player == null)
        {
            player = new GameObject(nomePlayer);
            player.tag = "Player";
            player.transform.position = posicaoInicial;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null)
            controller = player.AddComponent<CharacterController>();

        controller.height = alturaPlayer;
        controller.radius = raioPlayer;
        controller.center = new Vector3(0f, alturaPlayer / 2f, 0f);
        controller.stepOffset = 0.3f;
        controller.slopeLimit = 45f;

        // Pivot da cabeça
        Transform headPivot = player.transform.Find("HeadPivot");
        if (headPivot == null)
        {
            GameObject headObj = new GameObject("HeadPivot");
            headPivot = headObj.transform;
            headPivot.SetParent(player.transform);
        }

        headPivot.localPosition = new Vector3(0f, alturaOlhos, 0f);
        headPivot.localRotation = Quaternion.identity;

        // Câmera
        Camera cam = null;
        GameObject existingCamObj = GameObject.Find("FPS_Camera");

        if (existingCamObj != null)
        {
            cam = existingCamObj.GetComponent<Camera>();
        }

        if (cam == null)
        {
            GameObject camObj = new GameObject("FPS_Camera");
            cam = camObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
        }

        cam.transform.SetParent(headPivot);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;

        AudioListener listener = cam.GetComponent<AudioListener>();
        if (listener == null)
            cam.gameObject.AddComponent<AudioListener>();

        DesativarAudioListenersExtras(cam.gameObject);

        SimpleHumanFPSController fps = player.GetComponent<SimpleHumanFPSController>();
        if (fps == null)
            fps = player.AddComponent<SimpleHumanFPSController>();

        fps.headPivot = headPivot;
        fps.playerCamera = cam;

        Debug.Log("Player FPS gerado com sucesso.");
    }

    void Start()
    {
        if (gerarAoIniciar)
            GerarPlayerFPS();
    }

    void RemoverExistente(string nome)
    {
        GameObject obj = GameObject.Find(nome);

        if (obj != null)
        {
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }
    }

    void DesativarAudioListenersExtras(GameObject keep)
    {
        AudioListener[] listeners = FindObjectsOfType<AudioListener>();

        for (int i = 0; i < listeners.Length; i++)
        {
            if (listeners[i] != null)
            {
                listeners[i].enabled = listeners[i].gameObject == keep;
            }
        }
    }
}

public class SimpleHumanFPSController : MonoBehaviour
{
    [Header("Referências")]
    public Transform headPivot;
    public Camera playerCamera;

    [Header("Movimento")]
    public float velocidadeAndando = 4.5f;
    public float velocidadeCorrendo = 6.8f;
    public float aceleracao = 14f;
    public float desaceleracao = 18f;
    public float forcaPulo = 1.2f;
    public float gravidade = -20f;

    [Header("Mouse")]
    public float sensibilidadeMouse = 2f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    [Header("Câmera - FOV")]
    public float fovPadrao = 70f;
    public float fovCorrendo = 76f;
    public float velocidadeFov = 8f;

    [Header("Câmera - Head Bob")]
    public float bobAndandoFrequencia = 8f;
    public float bobAndandoAmplitudeVertical = 0.045f;
    public float bobAndandoAmplitudeHorizontal = 0.025f;

    public float bobCorrendoFrequencia = 11f;
    public float bobCorrendoAmplitudeVertical = 0.065f;
    public float bobCorrendoAmplitudeHorizontal = 0.035f;

    [Header("Câmera - Respiração/Idle")]
    public float idleBreathingSpeed = 1.35f;
    public float idleBreathingVertical = 0.012f;
    public float idleBreathingHorizontal = 0.006f;

    [Header("Câmera - Inclinação")]
    public float inclinacaoLateral = 3.5f;
    public float velocidadeInclinacao = 8f;

    [Header("Câmera - Impacto ao pousar")]
    public float impactoPousoMaximo = 0.09f;
    public float velocidadeRecuperacaoPouso = 8f;
    public float pitchImpactoPouso = 3f;

    [Header("Cursor")]
    public bool travarCursorAoIniciar = true;

    private CharacterController controller;

    private float yaw;
    private float pitch;
    private Vector3 velocidadeHorizontal;
    private float velocidadeVertical;

    private bool estavaNoChao;
    private float velocidadeVerticalAntesDoChao;

    private float bobTimer;
    private float inclinacaoAtual;
    private float landingOffset;
    private float landingPitchOffset;

    private Vector3 cameraBaseLocalPos;
    private float currentFov;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        if (headPivot == null && playerCamera != null)
            headPivot = playerCamera.transform.parent;

        if (playerCamera != null)
        {
            cameraBaseLocalPos = playerCamera.transform.localPosition;
            currentFov = fovPadrao;
            playerCamera.fieldOfView = fovPadrao;
        }

        yaw = transform.eulerAngles.y;

        if (travarCursorAoIniciar)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        if (controller == null || playerCamera == null || headPivot == null)
            return;

        HandleLook();
        HandleMovement();
        HandleCameraEffects();
        HandleCursor();
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensibilidadeMouse;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadeMouse;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        headPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleMovement()
    {
        bool noChao = controller.isGrounded;

        if (noChao && velocidadeVertical < 0f)
            velocidadeVertical = -2f;

        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        Vector3 direcao = (transform.right * inputX + transform.forward * inputZ).normalized;

        bool correndo = Input.GetKey(KeyCode.LeftShift) && inputZ > 0.1f;
        float velocidadeAlvo = correndo ? velocidadeCorrendo : velocidadeAndando;

        Vector3 alvoHorizontal = direcao * velocidadeAlvo;

        float taxa = direcao.magnitude > 0.01f ? aceleracao : desaceleracao;
        velocidadeHorizontal = Vector3.Lerp(velocidadeHorizontal, alvoHorizontal, taxa * Time.deltaTime);

        velocidadeVerticalAntesDoChao = velocidadeVertical;

        if (Input.GetKeyDown(KeyCode.Space) && noChao)
        {
            velocidadeVertical = Mathf.Sqrt(forcaPulo * -2f * gravidade);
        }

        velocidadeVertical += gravidade * Time.deltaTime;

        Vector3 movimentoFinal = velocidadeHorizontal + Vector3.up * velocidadeVertical;
        controller.Move(movimentoFinal * Time.deltaTime);

        bool pousouAgora = !estavaNoChao && controller.isGrounded;

        if (pousouAgora)
        {
            float impacto = Mathf.Abs(velocidadeVerticalAntesDoChao) * 0.02f;
            impacto = Mathf.Clamp(impacto, 0f, impactoPousoMaximo);

            landingOffset = impacto;
            landingPitchOffset = Mathf.Clamp(impacto * 40f, 0f, pitchImpactoPouso);
        }

        estavaNoChao = controller.isGrounded;
    }

    void HandleCameraEffects()
    {
        Vector3 velocidadePlana = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
        float velocidadeAtual = velocidadePlana.magnitude;

        bool noChao = controller.isGrounded;
        bool andando = noChao && velocidadeAtual > 0.15f;
        bool correndo = andando && Input.GetKey(KeyCode.LeftShift) && Input.GetAxisRaw("Vertical") > 0.1f;

        float verticalBob = 0f;
        float horizontalBob = 0f;

        if (andando)
        {
            float freq = correndo ? bobCorrendoFrequencia : bobAndandoFrequencia;
            float ampY = correndo ? bobCorrendoAmplitudeVertical : bobAndandoAmplitudeVertical;
            float ampX = correndo ? bobCorrendoAmplitudeHorizontal : bobAndandoAmplitudeHorizontal;

            bobTimer += Time.deltaTime * freq * Mathf.Clamp01(velocidadeAtual / velocidadeCorrendo);

            verticalBob = Mathf.Abs(Mathf.Sin(bobTimer)) * ampY;
            horizontalBob = Mathf.Sin(bobTimer * 0.5f) * ampX;
        }
        else
        {
            bobTimer += Time.deltaTime * idleBreathingSpeed;

            verticalBob = Mathf.Sin(bobTimer) * idleBreathingVertical;
            horizontalBob = Mathf.Sin(bobTimer * 0.5f) * idleBreathingHorizontal;
        }

        landingOffset = Mathf.Lerp(landingOffset, 0f, velocidadeRecuperacaoPouso * Time.deltaTime);
        landingPitchOffset = Mathf.Lerp(landingPitchOffset, 0f, velocidadeRecuperacaoPouso * Time.deltaTime);

        Vector3 cameraTargetPos = cameraBaseLocalPos;
        cameraTargetPos += new Vector3(horizontalBob, verticalBob - landingOffset, 0f);

        playerCamera.transform.localPosition = Vector3.Lerp(
            playerCamera.transform.localPosition,
            cameraTargetPos,
            12f * Time.deltaTime
        );

        float inputX = Input.GetAxisRaw("Horizontal");
        float inclinacaoAlvo = -inputX * inclinacaoLateral;
        inclinacaoAtual = Mathf.Lerp(inclinacaoAtual, inclinacaoAlvo, velocidadeInclinacao * Time.deltaTime);

        Quaternion cameraTargetRot = Quaternion.Euler(landingPitchOffset, 0f, inclinacaoAtual);
        playerCamera.transform.localRotation = Quaternion.Lerp(
            playerCamera.transform.localRotation,
            cameraTargetRot,
            10f * Time.deltaTime
        );

        float fovAlvo = correndo ? fovCorrendo : fovPadrao;
        currentFov = Mathf.Lerp(currentFov, fovAlvo, velocidadeFov * Time.deltaTime);
        playerCamera.fieldOfView = currentFov;
    }

    void HandleCursor()
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