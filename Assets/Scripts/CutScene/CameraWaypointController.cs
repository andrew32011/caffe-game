using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Camera))]
public class SmoothCameraWaypointController : MonoBehaviour
{
    [System.Serializable]
    public class WaypointSettings
    {
        public Transform waypoint;
        [Range(0.1f, 50f), Tooltip("�������� ����������� � �����")]
        public float moveSpeed = 5f;
        [Range(0.1f, 50f), Tooltip("�������� �������� � �����")]
        public float rotationSpeed = 5f;
        [Range(0.1f, 5f), Tooltip("��������� �����/������ �� �����")]
        public float smoothness = 1f;

        public WaypointSettings(Transform wp)
        {
            waypoint = wp;
            moveSpeed = 5f;
            rotationSpeed = 5f;
            smoothness = 1f;
        }
    }

    [Header("�������� ���������")]
    public bool autoSwitchWaypoints = false;
    [Tooltip("����� ��������� �� ������ ����� ����� ��������� � ���������")]
    public float holdTime = 2f;

    [Header("����������� �����")]
    public List<WaypointSettings> waypoints = new List<WaypointSettings>();

    [Header("��������� ���������")]
    [Tooltip("����������� ���������� ��� ���������� ���������� �����")]
    public float arrivalThreshold = 0.1f;
    [Tooltip("����������� ���� ��� ���������� ���������� ����������")]
    public float rotationThreshold = 1f;

    [Header("��������� FOV ��� ��������� �����")]
    [Tooltip("������������ ������� ��������� FOV �� ��������� �����")]
    public bool enableFinalFovControl = true;
    [Tooltip("��������� �������� FOV ��� �������� �� ��������� �����")]
    public float initialFov = 60f;
    [Tooltip("������� �������� FOV �� ��������� �����")]
    public float targetFov = 40f;
    [Tooltip("�������� ��������� FOV")]
    [Range(0.1f, 10f)]
    public float fovChangeSpeed = 2f;
    [Tooltip("����� �������� ����� ������� ��������� FOV")]
    public float fovDelay = 1f;

    [Header("������� ���������")]
    public int currentWaypointIndex = 0;
    public bool isEnabled = true;
    public bool isAtFinalWaypoint = false;

    private Camera cam;
    private float currentHoldTime = 0f;
    private bool isMoving = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float currentMoveSpeed;
    private float currentRotationSpeed;
    private float currentSmoothness;
    private Vector3 currentVelocity = Vector3.zero;
    private float lastDistanceToTarget = 0f;
    private float currentFov = 60f;
    private float fovTimer = 0f;
    private bool isChangingFov = false;
    private bool _endTriggered = false; // финал запускаем один раз

    private void Awake()
    {
        cam = GetComponent<Camera>();
        currentFov = cam.fieldOfView;
    }

    private void Start()
    {
        if (waypoints.Count == 0)
        {
            Debug.LogWarning("��� ����������� �����. �������� �� � ����������.");
            return;
        }

        SetTargetWaypoint(currentWaypointIndex);
    }

    private void Update()
    {
        if (!isEnabled || waypoints.Count == 0) return;

        if (isMoving)
        {
            MoveToTarget();
        }
        else if (isAtFinalWaypoint && enableFinalFovControl)
        {
            HandleFinalFov();
        }
    }

    private void LateUpdate()
    {
        if (!isEnabled || waypoints.Count == 0) return;

        if (autoSwitchWaypoints && !isMoving && !isAtFinalWaypoint)
        {
            currentHoldTime += Time.deltaTime;
            if (currentHoldTime >= holdTime)
            {
                NextWaypoint();
                currentHoldTime = 0f;
            }
        }
    }

    private void MoveToTarget()
    {
        if (currentWaypointIndex >= waypoints.Count || waypoints[currentWaypointIndex].waypoint == null)
        {
            isMoving = false;
            return;
        }

        WaypointSettings currentSettings = waypoints[currentWaypointIndex];
        targetPosition = waypoints[currentWaypointIndex].waypoint.position;
        targetRotation = waypoints[currentWaypointIndex].waypoint.rotation;

        // ������ ���������� �� ����
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        float angleToTarget = Quaternion.Angle(transform.rotation, targetRotation);

        // ������������ ��������� �������� � ����������� �� ����������
        float moveProgress = Mathf.InverseLerp(0f, lastDistanceToTarget, distanceToTarget);
        float rotationProgress = Mathf.InverseLerp(0f, 180f, angleToTarget);

        // ������� ��������� � ����������
        float speedMultiplier = Mathf.SmoothStep(0.1f, 1f, moveProgress);
        float rotationMultiplier = Mathf.SmoothStep(0.1f, 1f, rotationProgress);

        // ���������� ���������/����������
        speedMultiplier = Mathf.Lerp(0.1f, 1f, Mathf.Pow(speedMultiplier, currentSettings.smoothness));
        rotationMultiplier = Mathf.Lerp(0.1f, 1f, Mathf.Pow(rotationMultiplier, currentSettings.smoothness));

        // ���������� ������� ���������
        float effectiveMoveSpeed = currentMoveSpeed * speedMultiplier;
        float effectiveRotationSpeed = currentRotationSpeed * rotationMultiplier;

        // ������� ����������� � �������������� SmoothDamp
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            1f / (effectiveMoveSpeed + 0.0001f) // �������� ������� �� ����
        );

        // ������� ������� � �������������� Slerp
        Quaternion smoothedRotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            effectiveRotationSpeed * Time.deltaTime
        );

        // ���������� ������� � ��������
        transform.position = smoothedPosition;
        transform.rotation = smoothedRotation;

        // �������� ���������� ���� � ������ ��������� ��������
        bool reachedPosition = distanceToTarget < arrivalThreshold;
        bool reachedRotation = angleToTarget < rotationThreshold;

        if (reachedPosition && reachedRotation)
        {
            isMoving = false;
            isAtFinalWaypoint = (currentWaypointIndex == waypoints.Count - 1);

            if (isAtFinalWaypoint && enableFinalFovControl)
            {
                // ��������� ��������� FOV � �������� ������ ��������
                currentFov = cam.fieldOfView;
                fovTimer = 0f;
                isChangingFov = false;
                Debug.Log("���������� ��������� �����. �������� ����� ���������� FOV...");
            }

            Debug.Log($"���������� ����� {currentWaypointIndex + 1}/{waypoints.Count}");
        }

        // ��������� ��������� ���������� ��� ���������
        lastDistanceToTarget = distanceToTarget;
    }

    private void HandleFinalFov()
    {
        // �������� ����� ������� ��������� FOV
        if (!isChangingFov)
        {
            fovTimer += Time.deltaTime;
            if (fovTimer >= fovDelay)
            {
                isChangingFov = true;
                currentFov = cam.fieldOfView;
                Debug.Log($"������ �������� ��������� FOV � {currentFov} �� {targetFov}");
            }
            return;
        }

        // ������� ��������� FOV
        currentFov = Mathf.Lerp(currentFov, targetFov, fovChangeSpeed * Time.deltaTime);
        cam.fieldOfView = currentFov;

        // �������� ���������� �������� FOV
        if (!_endTriggered && Mathf.Abs(currentFov - targetFov) < 8f)
        {
            cam.fieldOfView = targetFov;
            currentFov = targetFov;
            _endTriggered = true;
            // Камера доехала: показываем историю Миры. Кнопка «Продолжить» грузит сцену.
            if (IntroStoryUI.Instance != null) IntroStoryUI.Instance.Begin();
            else LoadMainLevel();
        }
    }

    public void LoadMainLevel()
    {
        SceneManager.LoadScene(1);
    }
    public void SetTargetWaypoint(int index)
    {
        if (index < 0 || index >= waypoints.Count || waypoints[index].waypoint == null)
        {
            Debug.LogError($"������������ ������ �����: {index}");
            return;
        }

        // ���������� ��������� ��������� �����
        isAtFinalWaypoint = false;
        isChangingFov = false;

        currentWaypointIndex = index;
        targetPosition = waypoints[index].waypoint.position;
        targetRotation = waypoints[index].waypoint.rotation;
        currentMoveSpeed = waypoints[index].moveSpeed;
        currentRotationSpeed = waypoints[index].rotationSpeed;
        currentSmoothness = waypoints[index].smoothness;

        // ����� �������� ��� ����� ����
        currentVelocity = Vector3.zero;
        lastDistanceToTarget = Vector3.Distance(transform.position, targetPosition);

        isMoving = true;
        currentHoldTime = 0f;

        Debug.Log($"������ �������� � ����� {index + 1}");
    }

    public void NextWaypoint()
    {
        int nextIndex = currentWaypointIndex + 1;
        if (nextIndex < waypoints.Count)
        {
            SetTargetWaypoint(nextIndex);
        }
        else
        {
            // �������� ����� � ������ ���������������
            isMoving = false;
            isAtFinalWaypoint = true;

            if (enableFinalFovControl)
            {
                currentFov = initialFov;
                fovTimer = 0f;
                isChangingFov = false;
                Debug.Log("���������� ��������� ����������� �����. ����� � ��������� FOV.");
            }
            else
            {
                Debug.Log("���������� ��������� ����������� �����. �������� �����������.");
            }
        }
    }

    public void PreviousWaypoint()
    {
        int prevIndex = currentWaypointIndex - 1;
        if (prevIndex >= 0)
        {
            SetTargetWaypoint(prevIndex);
        }
        else
        {
            Debug.Log("��� �� ������ �����.");
        }
    }

    public void ResetToCurrentWaypoint()
    {
        if (currentWaypointIndex >= 0 && currentWaypointIndex < waypoints.Count &&
            waypoints[currentWaypointIndex].waypoint != null)
        {
            transform.position = waypoints[currentWaypointIndex].waypoint.position;
            transform.rotation = waypoints[currentWaypointIndex].waypoint.rotation;
            isMoving = false;
            isAtFinalWaypoint = (currentWaypointIndex == waypoints.Count - 1);
            currentVelocity = Vector3.zero;

            if (isAtFinalWaypoint && enableFinalFovControl)
            {
                currentFov = initialFov;
                cam.fieldOfView = initialFov;
                fovTimer = 0f;
                isChangingFov = false;
            }
        }
    }

    public void ResetFovToInitial()
    {
        if (isAtFinalWaypoint)
        {
            currentFov = initialFov;
            cam.fieldOfView = initialFov;
            fovTimer = 0f;
            isChangingFov = false;
            Debug.Log("FOV ������� � ���������� ��������");
        }
    }

    public void SetTargetFov(float newTargetFov)
    {
        targetFov = Mathf.Clamp(newTargetFov, 10f, 120f);
        if (isAtFinalWaypoint && isChangingFov)
        {
            Debug.Log($"������� FOV �������� �� {targetFov}");
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying && waypoints != null)
        {
            // ������ ����� ��������
            for (int i = 0; i < waypoints.Count; i++)
            {
                if (waypoints[i] != null && waypoints[i].waypoint != null)
                {
                    // �������� �����
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(waypoints[i].waypoint.position, 0.2f);

                    // ����������� �������
                    Vector3 forward = waypoints[i].waypoint.forward * 0.5f;
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(waypoints[i].waypoint.position, waypoints[i].waypoint.position + forward);

                    // ���������� � ���������� ������
                    if (i > 0 && waypoints[i - 1] != null && waypoints[i - 1].waypoint != null)
                    {
                        Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.5f);
                        Gizmos.DrawLine(waypoints[i - 1].waypoint.position, waypoints[i].waypoint.position);
                    }
                }
            }

            // ��������� ��������� �����
            if (waypoints.Count > 0 && waypoints[waypoints.Count - 1] != null && waypoints[waypoints.Count - 1].waypoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(waypoints[waypoints.Count - 1].waypoint.position, 0.4f);
            }

            // ��������� ������� ����� � ���������
            if (currentWaypointIndex >= 0 && currentWaypointIndex < waypoints.Count &&
                waypoints[currentWaypointIndex] != null && waypoints[currentWaypointIndex].waypoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(waypoints[currentWaypointIndex].waypoint.position, 0.3f);
            }
        }
    }
#endif
}