using UnityEngine;

/// <summary>
/// Controlador de vuelo con dinámicas de planeo (inercia, picado, ascenso).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class PaperPlaneController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform cameraTransform;
    public Transform planeVisual;
    public Transform swordVisual;
    public ThirdPersonSwordCamera cameraRig;

    [Header("Referencia al Sistema de Apuntado")]
    [Tooltip("Arrastra aquí el objeto del Canvas que tiene el script AimAssistVisual")]
    public AimAssistVisual aimAssist;

    [Header("Sistema de Lanzamiento Inicial")]
    public Transform orbitCenter;
    public float orbitRadius = 5f;
    public float orbitSpeed = 90f;
    public bool centrifugeRotation = true;
    public bool isOrbitingAtStart = true;
    private float currentOrbitAngle = 0f;

    [Header("Cámara durante Órbita")]
    public float cameraOrbitAngle = 180f;
    public float cameraOrbitDistance = 8f;
    public float cameraOrbitHeight = 3f;
    public bool lockCameraDuringOrbit = true;

    [Header("Dinámicas de Planeo (Inercia)")]
    public float initialLaunchSpeed = 80f;
    public float minSpeed = 15f;
    public float absoluteMaxSpeed = 150f;
    public float diveAcceleration = 45f;
    public float climbDeceleration = 35f;
    public float baseDrag = 8f;

    [Header("Control directo con teclado")]
    public float pitchInputSharpness = 12f;
    public float maxPitchAngle = 60f;

    [Header("Inclinación y seguimiento de cámara")]
    public float maxBankAngle = 90f;
    public float cameraYawToBankSensitivity = 10f;
    public float cameraYawFollowSharpness = 10f;

    [Header("Seguimiento de yaw")]
    public float minYawFollowMultiplier = 0.5f;
    public float maxYawFollowMultiplier = 3f;
    public float bankToYawCurve = 0.4f;

    [Header("Pitch por cámara")]
    public float cameraPitchToNoseSensitivity = 1.5f;
    public float cameraPitchFollowSharpness = 10f;
    public float maxCameraPitchAngle = 80f;

    [Header("Efecto de vuelo")]
    public float wobbleAmplitude = 1.2f;
    public float wobbleFrequency = 1.2f;
    public bool enableGravityGlide = true;
    public float glideGravity = 2.0f; // Ajustado para ser una fuerza pura

    [Header("Colisiones y Enemigos")]
    [Range(0f, 1f)]
    public float collisionSpeedRetention = 0.3f;
    public string enemyTag = "Enemie";

    [Header("Lanzamiento Automático (Aim Assist)")]
    public float enemyLaunchSpeed = 120f;
    public float maxEnemyLaunchDuration = 3f;
    public bool rotateTowardsLaunchTarget = true;
    public float launchRotationSharpness = 25f;
    public bool launchOnLeftClick = true;
    public bool allowRelauchAfterImpact = true;

    [Header("Giro visual de espada")]
    public float swordNormalSpinSpeed = 360f;
    public float swordLaunchMaxSpinSpeed = 1800f;
    public float swordSpinAcceleration = 1200f;
    public float swordSpinDeceleration = 900f;
    public float swordSpinDirection = 1f;
    public bool spinAroundWorldY = true;
    public bool preserveSwordInitialRotation = true;

    private Rigidbody rb;
    private float currentSpeed;
    private float currentKeyboardPitch;
    private float currentCameraPitch;
    private float currentBank;
    private float currentYaw;
    private float lastCameraYaw;
    private float lastCameraPitch;
    private float wobbleTimer;

    private bool isLaunchedAtEnemy;
    private Transform launchTarget;
    private Vector3 launchTargetPoint;
    private float launchTimer;
    private bool gravityBeforeLaunch;

    private float currentSwordSpinSpeed;
    private float swordSpinAngle;
    private Quaternion swordPivotInitialLocalRotation;
    private Quaternion swordInitialLocalRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (cameraRig == null) cameraRig = FindObjectOfType<ThirdPersonSwordCamera>();
        if (planeVisual == null) planeVisual = transform;
        if (swordVisual == null) swordVisual = planeVisual;

        if (isOrbitingAtStart && orbitCenter != null)
        {
            rb.isKinematic = true;

            if (lockCameraDuringOrbit && cameraRig != null)
            {
                cameraRig.enabled = false;
            }
        }

        swordPivotInitialLocalRotation = (planeVisual != null && planeVisual != transform) ? planeVisual.localRotation : Quaternion.identity;
        swordInitialLocalRotation = (swordVisual != null) ? swordVisual.localRotation : Quaternion.identity;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentSpeed = minSpeed;
        currentYaw = transform.eulerAngles.y;
        currentSwordSpinSpeed = Mathf.Sign(swordSpinDirection) * swordNormalSpinSpeed;

        if (cameraRig != null && !isOrbitingAtStart)
        {
            lastCameraYaw = cameraRig.CurrentYaw;
            lastCameraPitch = cameraRig.CurrentPitch;
            currentYaw = cameraRig.CurrentYaw;
        }
    }

    private void Update()
    {
        ManageCursorState();

        if (isOrbitingAtStart && orbitCenter != null)
        {
            HandleOrbitLaunch();
            UpdateVisualSwordSpin();
            return;
        }

        DetectLaunchInput();

        if (isLaunchedAtEnemy)
        {
            UpdateLaunchTimer();
            ActualizarVisualDuranteLanzamiento();
            UpdateVisualSwordSpin();
            return;
        }

        HandlePitchKeyboardInput();
        HandleCameraDrivenBank();
        HandleCameraDrivenPitch();
        ApplyWobble();

        UpdateInertiaAndSpeed();

        ActualizarVisualDuranteVueloNormal();
        UpdateVisualSwordSpin();
    }

    private void FixedUpdate()
    {
        if (isOrbitingAtStart) return;

        if (isLaunchedAtEnemy)
        {
            ApplyEnemyLaunchMovement();
            return;
        }
        ApplyNormalFlightMovement();
    }

    private void ManageCursorState()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void HandleOrbitLaunch()
    {
        currentOrbitAngle += orbitSpeed * Time.deltaTime;
        float rad = currentOrbitAngle * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * orbitRadius;
        transform.position = orbitCenter.position + offset;

        if (centrifugeRotation)
        {
            transform.forward = offset.normalized;
        }
        else
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, offset.normalized);
            if (orbitSpeed < 0) tangent = -tangent;
            transform.forward = tangent;
        }

        if (lockCameraDuringOrbit && cameraTransform != null)
        {
            float camRad = cameraOrbitAngle * Mathf.Deg2Rad;
            Vector3 camOffset = new Vector3(
                Mathf.Sin(camRad) * cameraOrbitDistance,
                cameraOrbitHeight,
                Mathf.Cos(camRad) * cameraOrbitDistance
            );

            cameraTransform.position = orbitCenter.position + camOffset;
            cameraTransform.LookAt(orbitCenter.position);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ExecuteStartLaunch();
        }
    }

    private void ExecuteStartLaunch()
    {
        isOrbitingAtStart = false;
        rb.isKinematic = false;

        if (lockCameraDuringOrbit && cameraRig != null)
        {
            cameraRig.enabled = true;
        }

        currentYaw = transform.eulerAngles.y;
        currentBank = 0f;
        currentKeyboardPitch = 0f;
        currentCameraPitch = 0f;

        currentSpeed = initialLaunchSpeed;

        if (cameraRig != null)
        {
            lastCameraYaw = currentYaw;
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.StartLevel();
        }
    }

    private void UpdateInertiaAndSpeed()
    {
        float verticalDirection = Vector3.Dot(transform.forward, Vector3.down);

        if (verticalDirection > 0)
        {
            currentSpeed += verticalDirection * diveAcceleration * Time.deltaTime;
        }
        else
        {
            currentSpeed += verticalDirection * climbDeceleration * Time.deltaTime;
        }

        currentSpeed -= baseDrag * Time.deltaTime;
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, absoluteMaxSpeed);
    }

    // =========================================================
    // CÁLCULO DE VUELO CORREGIDO (Sin retrasos de físicas)
    // =========================================================
    private void ApplyNormalFlightMovement()
    {
        float pitch = Mathf.Clamp(currentKeyboardPitch + currentCameraPitch, -maxPitchAngle, maxPitchAngle);
        float wobble = Mathf.Sin(wobbleTimer * Mathf.PI * 2f) * wobbleAmplitude;

        // 1. Calculamos la rotación exacta matemática del frame actual
        Quaternion targetRotation = Quaternion.Euler(pitch, currentYaw, currentBank + wobble);

        // 2. Extraemos el vector hacia adelante de esa rotación (sin depender del objeto)
        Vector3 exactForward = targetRotation * Vector3.forward;

        // 3. Multiplicamos por la velocidad inercial
        Vector3 finalVelocity = exactForward * currentSpeed;

        if (enableGravityGlide)
        {
            // Planear tira ligeramente hacia abajo de forma constante
            finalVelocity += Vector3.down * glideGravity;
        }

        SetLinearVelocity(finalVelocity);
        rb.MoveRotation(targetRotation); // Giramos físicamente el avión

        ActualizarVisualDuranteVueloNormal();
    }

    private void ActualizarVisualDuranteVueloNormal()
    {
        if (planeVisual == null || planeVisual == transform) return;

        float wobble = Mathf.Sin(wobbleTimer * Mathf.PI * 2f) * wobbleAmplitude;
        float pitch = Mathf.Clamp(currentKeyboardPitch + currentCameraPitch, -maxPitchAngle, maxPitchAngle);

        Quaternion inclinacion = Quaternion.Euler(pitch, 0f, currentBank + wobble);
        planeVisual.localRotation = preserveSwordInitialRotation
            ? swordPivotInitialLocalRotation * inclinacion
            : inclinacion;
    }

    private void ActualizarVisualDuranteLanzamiento()
    {
        if (planeVisual == null || planeVisual == transform) return;
        planeVisual.localRotation = preserveSwordInitialRotation ? swordPivotInitialLocalRotation : Quaternion.identity;
    }

    private void DetectLaunchInput()
    {
        if (!launchOnLeftClick || !Input.GetMouseButtonDown(0)) return;
        TryLaunchAtEnemy();
    }

    private void TryLaunchAtEnemy()
    {
        if (isLaunchedAtEnemy) return;

        if (aimAssist == null || aimAssist.objetivoActual == null) return;

        Transform hitTransform = aimAssist.objetivoActual;
        launchTarget = hitTransform.root;

        Collider col = hitTransform.GetComponentInChildren<Collider>();
        launchTargetPoint = col != null ? col.bounds.center : hitTransform.position;

        BeginEnemyLaunch();
    }

    private void BeginEnemyLaunch()
    {
        isLaunchedAtEnemy = true;
        launchTimer = 0f;
        gravityBeforeLaunch = rb.useGravity;
        //if (disableGravityDuringLaunch) rb.useGravity = false;

        SetLinearVelocity(Vector3.zero);
        rb.angularVelocity = Vector3.zero;

        Vector3 direction = launchTargetPoint - rb.worldCenterOfMass;
        if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;

        direction.Normalize();
        SetLinearVelocity(direction * enemyLaunchSpeed);
        rb.MoveRotation(Quaternion.LookRotation(direction, Vector3.up));
    }

    private void ApplyEnemyLaunchMovement()
    {
        if (launchTarget != null)
        {
            Collider targetCollider = launchTarget.GetComponentInChildren<Collider>();
            launchTargetPoint = targetCollider != null ? targetCollider.bounds.center : launchTarget.position;
        }

        Vector3 direction = launchTargetPoint - rb.worldCenterOfMass;
        if (direction.sqrMagnitude < 0.0001f) direction = transform.forward;

        direction.Normalize();
        SetLinearVelocity(direction * enemyLaunchSpeed);

        if (rotateTowardsLaunchTarget)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            float t = 1f - Mathf.Exp(-launchRotationSharpness * Time.fixedDeltaTime);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, t));
        }

        ActualizarVisualDuranteLanzamiento();
    }

    private void UpdateLaunchTimer()
    {
        launchTimer += Time.deltaTime;
        if (launchTimer >= maxEnemyLaunchDuration) EndEnemyLaunch(false);
    }

    private void EndEnemyLaunch(bool impactedEnemy)
    {
        isLaunchedAtEnemy = false;
        launchTarget = null;
        launchTimer = 0f;
        rb.useGravity = gravityBeforeLaunch;
        rb.angularVelocity = Vector3.zero;

        currentSpeed = enemyLaunchSpeed;
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, absoluteMaxSpeed);

        if (impactedEnemy)
        {
            SetLinearVelocity(transform.forward * currentSpeed);
        }

        if (allowRelauchAfterImpact)
        {
            lastCameraYaw = cameraRig != null ? cameraRig.CurrentYaw : currentYaw;
            lastCameraPitch = cameraRig != null ? cameraRig.CurrentPitch : 0f;
            currentYaw = transform.eulerAngles.y;
            currentBank = 0f;
            currentCameraPitch = 0f;
        }
    }

    private void HandlePitchKeyboardInput()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.W)) input -= 1f;
        if (Input.GetKey(KeyCode.S)) input += 1f;

        float target = input * maxPitchAngle;
        currentKeyboardPitch = Mathf.Lerp(currentKeyboardPitch, target, 1f - Mathf.Exp(-pitchInputSharpness * Time.deltaTime));
    }

    private void HandleCameraDrivenBank()
    {
        if (cameraRig == null)
        {
            currentBank = Mathf.Lerp(currentBank, 0f, 1f - Mathf.Exp(-cameraYawFollowSharpness * Time.deltaTime));
            return;
        }

        float yaw = cameraRig.CurrentYaw;
        float delta = Mathf.DeltaAngle(lastCameraYaw, yaw);
        lastCameraYaw = yaw;

        float speed = Time.deltaTime > 0f ? delta / Time.deltaTime : 0f;
        float targetBank = Mathf.Clamp(-speed * cameraYawToBankSensitivity, -maxBankAngle, maxBankAngle);

        currentBank = Mathf.Lerp(currentBank, targetBank, 1f - Mathf.Exp(-cameraYawFollowSharpness * Time.deltaTime));

        float bankFactor = Mathf.Abs(currentBank) / Mathf.Max(maxBankAngle, 0.0001f);
        float multiplier = Mathf.Lerp(minYawFollowMultiplier, maxYawFollowMultiplier, Mathf.Pow(bankFactor, bankToYawCurve));

        currentYaw = Mathf.LerpAngle(currentYaw, yaw, 1f - Mathf.Exp(-cameraYawFollowSharpness * multiplier * Time.deltaTime));
    }

    private void HandleCameraDrivenPitch()
    {
        if (cameraRig == null)
        {
            currentCameraPitch = Mathf.Lerp(currentCameraPitch, 0f, 1f - Mathf.Exp(-cameraPitchFollowSharpness * Time.deltaTime));
            return;
        }

        float pitch = cameraRig.CurrentPitch;
        lastCameraPitch = pitch;

        float target = Mathf.Clamp(pitch * cameraPitchToNoseSensitivity, -maxCameraPitchAngle, maxCameraPitchAngle);

        currentCameraPitch = Mathf.Lerp(currentCameraPitch, target, 1f - Mathf.Exp(-cameraPitchFollowSharpness * Time.deltaTime));
    }

    private void ApplyWobble()
    {
        wobbleTimer += Time.deltaTime * wobbleFrequency;
    }

    private void UpdateVisualSwordSpin()
    {
        if (swordVisual == null) return;

        if (!isOrbitingAtStart)
        {
            float direction = Mathf.Sign(swordSpinDirection);
            float targetSpeed = isLaunchedAtEnemy ? direction * swordLaunchMaxSpinSpeed : direction * swordNormalSpinSpeed;
            float acceleration = isLaunchedAtEnemy ? swordSpinAcceleration : swordSpinDeceleration;

            currentSwordSpinSpeed = Mathf.MoveTowards(currentSwordSpinSpeed, targetSpeed, acceleration * Time.deltaTime);

            float deltaAngle = currentSwordSpinSpeed * Time.deltaTime;
            swordSpinAngle = Mathf.Repeat(swordSpinAngle + deltaAngle, 360f);
        }

        if (spinAroundWorldY)
        {
            float planeYaw = transform.eulerAngles.y;
            swordVisual.rotation = Quaternion.Euler(0f, planeYaw + swordSpinAngle, 0f) * swordInitialLocalRotation;
        }
        else
        {
            swordVisual.localRotation = swordInitialLocalRotation * Quaternion.Euler(0f, swordSpinAngle, 0f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isOrbitingAtStart) return;

        bool isEnemy = collision.collider.CompareTag(enemyTag) || collision.collider.transform.root.CompareTag(enemyTag);

        if (isLaunchedAtEnemy && isEnemy)
        {
            EndEnemyLaunch(true);
            return;
        }

        if (!isEnemy)
        {
            currentSpeed *= collisionSpeedRetention;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isOrbitingAtStart) return;

        bool isEnemy = other.CompareTag(enemyTag) || other.transform.root.CompareTag(enemyTag);

        if (isEnemy && isLaunchedAtEnemy)
        {
            EndEnemyLaunch(true);
        }
    }

    private void SetLinearVelocity(Vector3 velocity)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = velocity;
#else
        rb.velocity = velocity;
#endif
    }

    private Vector3 GetLinearVelocity()
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }
}