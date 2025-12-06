using UnityEngine;

namespace WildernessSurvival.Core.Systems
{
    /// <summary>
    /// Camera controller isometrica stile Len's Island semplificata.
    /// Supporta zoom, pan con tastiera e touch.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class IsometricCameraController : MonoBehaviour
    {
        // ============================================
        // SINGLETON (opzionale)
        // ============================================

        public static IsometricCameraController Instance { get; private set; }

        // ============================================
        // CONFIGURAZIONE ISOMETRICA
        // ============================================

        [Header("=== SETUP ISOMETRICO ===")]
        [Tooltip("Angolo X della camera (tipico: 30-45)")]
        [Range(20f, 60f)]
        [SerializeField] private float cameraAngleX = 45f;

        [Tooltip("Angolo Y della camera (rotazione orizzontale)")]
        [SerializeField] private float cameraAngleY = 45f;

        [Tooltip("Usa proiezione ortografica?")]
        [SerializeField] private bool useOrthographic = true;

        [Header("=== ZOOM ===")]
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 20f;
        [SerializeField] private float currentZoom = 10f;

        [Tooltip("Smoothing dello zoom")]
        [SerializeField] private float zoomSmoothing = 10f;

        [Header("=== PAN (MOVIMENTO) ===")]
        [SerializeField] private float panSpeed = 20f;
        [SerializeField] private float panSmoothing = 8f;

        [Tooltip("Limiti del mondo (x=minX, y=maxX, z=minZ, w=maxZ)")]
        [SerializeField] private Vector4 worldBounds = new Vector4(-50, 50, -50, 50);

        [Tooltip("Abilita pan con bordi schermo")]
        #pragma warning disable CS0414 // Reserved for future edge panning feature
        [SerializeField] private bool enableEdgePan = true;

        [Tooltip("Distanza dal bordo per edge pan (pixels)")]
        [SerializeField] private float edgePanThreshold = 20f;
        #pragma warning restore CS0414

        [Header("=== TARGET FOLLOW ===")]
        [Tooltip("Target da seguire (opzionale)")]
        [SerializeField] private Transform followTarget;

        [Tooltip("Offset dal target")]
        [SerializeField] private Vector3 followOffset = Vector3.zero;

        [Tooltip("Velocità di inseguimento")]
        [SerializeField] private float followSpeed = 5f;

        [Header("=== DEBUG ===")]
        [SerializeField] private bool debugMode = false;

        // ============================================
        // RUNTIME
        // ============================================

        private Camera cam;
        private Vector3 targetPosition;
        private float targetZoom;
        private Vector3 lastMousePosition;
        private Vector3 panVelocity;

        // Touch
        private Vector2 touchStartPos;
        private float initialPinchDistance;
        private float initialZoom;

        // ============================================
        // PROPERTIES
        // ============================================

        public float CurrentZoom => currentZoom;
        public Vector3 FocusPoint => targetPosition;

        // ============================================
        // LIFECYCLE
        // ============================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            cam = GetComponent<Camera>();
        }

        private void Start()
        {
            SetupCamera();
            targetPosition = transform.position;
            targetZoom = currentZoom;
        }

        private void LateUpdate()
        {
            HandleInput();
            UpdateCamera();
        }

        // ============================================
        // SETUP
        // ============================================

        private void SetupCamera()
        {
            // Imposta proiezione
            cam.orthographic = useOrthographic;

            if (useOrthographic)
            {
                cam.orthographicSize = currentZoom;
            }

            // Imposta rotazione isometrica
            transform.rotation = Quaternion.Euler(cameraAngleX, cameraAngleY, 0f);

            if (debugMode)
            {
                Debug.Log($"[Camera] Setup: Angle({cameraAngleX}, {cameraAngleY}), Ortho: {useOrthographic}");
            }
        }

        // ============================================
        // INPUT HANDLING
        // ============================================

        private void HandleInput()
        {
            // Determina se siamo su mobile o PC
            bool isMobile = Input.touchCount > 0;

            if (isMobile)
            {
                HandleTouchInput();
            }
            else
            {
                HandleMouseInput();   // Solo zoom con rotella
                HandleKeyboardInput(); // WASD / frecce + Q/E per zoom
            }
        }

        private void HandleMouseInput()
        {
            // === ZOOM (Scroll wheel) ===
            float scrollDelta = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                targetZoom -= scrollDelta * zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            }

            // Pan con mouse e edge pan sono stati rimossi intenzionalmente
        }

        private void HandleKeyboardInput()
        {
            // WASD / Frecce per pan
            Vector3 input = Vector3.zero;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                input.z += 1;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                input.z -= 1;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                input.x -= 1;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                input.x += 1;

            if (input != Vector3.zero)
            {
                Vector3 move = GetWorldMovement(input.normalized * panSpeed * Time.deltaTime);
                targetPosition += move;
            }

            // Q/E per zoom
            if (Input.GetKey(KeyCode.Q))
                targetZoom += zoomSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E))
                targetZoom -= zoomSpeed * Time.deltaTime;

            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        private void HandleTouchInput()
        {
            if (Input.touchCount == 1)
            {
                // Single touch = Pan
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Moved)
                {
                    Vector3 delta = new Vector3(touch.deltaPosition.x, 0, touch.deltaPosition.y);
                    Vector3 move = GetWorldMovement(delta * panSpeed * Time.deltaTime * 0.05f);
                    targetPosition -= move;
                }
            }
            else if (Input.touchCount == 2)
            {
                // Pinch = Zoom
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                if (touch1.phase == TouchPhase.Began)
                {
                    initialPinchDistance = Vector2.Distance(touch0.position, touch1.position);
                    initialZoom = targetZoom;
                }
                else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
                {
                    float currentDistance = Vector2.Distance(touch0.position, touch1.position);

                    if (initialPinchDistance > 0)
                    {
                        float ratio = initialPinchDistance / currentDistance;
                        targetZoom = Mathf.Clamp(initialZoom * ratio, minZoom, maxZoom);
                    }
                }
            }
        }

        /// <summary>
        /// Converte movimento input in movimento mondo considerando rotazione camera
        /// </summary>
        private Vector3 GetWorldMovement(Vector3 input)
        {
            // Ottieni direzione forward/right della camera proiettate sul piano XZ
            Vector3 forward = transform.forward;
            forward.y = 0;
            forward.Normalize();

            Vector3 right = transform.right;
            right.y = 0;
            right.Normalize();

            return (right * input.x + forward * input.z);
        }

        // ============================================
        // UPDATE CAMERA
        // ============================================

        private void UpdateCamera()
        {
            // Follow target se assegnato
            if (followTarget != null)
            {
                Vector3 targetPos = followTarget.position + followOffset;
                targetPosition = Vector3.Lerp(targetPosition, targetPos, followSpeed * Time.deltaTime);
            }

            // Clamp posizione ai bounds
            targetPosition.x = Mathf.Clamp(targetPosition.x, worldBounds.x, worldBounds.y);
            targetPosition.z = Mathf.Clamp(targetPosition.z, worldBounds.z, worldBounds.w);

            // Smooth movement
            Vector3 desiredPosition = targetPosition - transform.forward * (targetZoom * 2f);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref panVelocity, 1f / panSmoothing);

            // Smooth zoom
            currentZoom = Mathf.Lerp(currentZoom, targetZoom, zoomSmoothing * Time.deltaTime);

            if (useOrthographic)
            {
                cam.orthographicSize = currentZoom;
            }
            else
            {
                // Per prospettiva, aggiusta FOV o distanza
                cam.fieldOfView = Mathf.Lerp(30f, 60f, (currentZoom - minZoom) / (maxZoom - minZoom));
            }
        }

        // ============================================
        // API PUBBLICA
        // ============================================

        /// <summary>
        /// Muove la camera su una posizione specifica
        /// </summary>
        public void FocusOn(Vector3 worldPosition, bool instant = false)
        {
            if (instant)
            {
                targetPosition = worldPosition;
                transform.position = worldPosition - transform.forward * (targetZoom * 2f);
            }
            else
            {
                targetPosition = worldPosition;
            }
        }

        /// <summary>
        /// Segue un target
        /// </summary>
        public void SetFollowTarget(Transform target, Vector3 offset = default)
        {
            followTarget = target;
            followOffset = offset;
        }

        /// <summary>
        /// Smette di seguire
        /// </summary>
        public void ClearFollowTarget()
        {
            followTarget = null;
        }

        /// <summary>
        /// Imposta zoom
        /// </summary>
        public void SetZoom(float zoom, bool instant = false)
        {
            targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);

            if (instant)
            {
                currentZoom = targetZoom;
                if (useOrthographic)
                {
                    cam.orthographicSize = currentZoom;
                }
            }
        }

        /// <summary>
        /// Converte posizione schermo in posizione mondo sul piano Y=0
        /// </summary>
        public Vector3 ScreenToWorldPosition(Vector3 screenPos)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

            if (groundPlane.Raycast(ray, out float distance))
            {
                return ray.GetPoint(distance);
            }

            return Vector3.zero;
        }

        // ============================================
        // DEBUG
        // ============================================

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Disegna bounds
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3(
                (worldBounds.x + worldBounds.y) / 2f,
                0,
                (worldBounds.z + worldBounds.w) / 2f
            );
            Vector3 size = new Vector3(
                worldBounds.y - worldBounds.x,
                1f,
                worldBounds.w - worldBounds.z
            );
            Gizmos.DrawWireCube(center, size);

            // Disegna focus point
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(targetPosition, 1f);
        }
#endif
    }
}
