using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class StageData
{
    public string stageName = "Stage";
    public bool isEnabled = true;

    // ������
    public Transform cameraTarget; // ����� ���� null
    public float cameraMoveDuration = 0.5f;

    // ��������
    public bool disableCharacter = false;

    // ������ �������
    public MonoBehaviour stageScript; // ����� ���� null
    public bool autoCompleteIfNoScript = true;
}

public class Stages : MonoBehaviour
{
    [Header("Stages")]
    public List<StageData> stages = new List<StageData>();

    [Header("Flow")]
    [Tooltip("Автоматически перейти на этап 0 при старте сцены. Выключить, если этапами управляет DayController.")]
    public bool autoStartStageZero = true;

    [Header("References")]
    public Transform playerCharacter;
    public Camera mainCamera;

    [Header("Подъём камеры на этапе ожидания гостя")]
    [Tooltip("Индекс этапа, где мы смотрим на подходящего гостя (обычно 0).")]
    public int guestWaitStageIndex = 0;
    [Tooltip("На сколько поднять камеру (по Y) на этом этапе.")]
    public float guestWaitCameraLift = 0.5f;

    [Header("Debug")]
    public bool manualControl = false; // ����� ��� ������� ������������ � ���������
    public int debugStageIndex = 0;

    [Header("Events")]
    public UnityEvent onReadyForCustomer; // ������ 0
    public UnityEvent onReadyForEffects;  // ��������� ������

    private int currentStage = -1;
    private bool isTransitioning = false;

    // Внешние скрипты (DayController) могут следить за сменой этапов
    public System.Action<int> OnStageEntered;
    public bool IsTransitioning => isTransitioning;

    void Start()
    {
        if (autoStartStageZero)
            StartCoroutine(GoToStage(0));
    }

#if UNITY_EDITOR
    // Только редакторский дебаг: ручное переключение стадий из инспектора.
    // Весь метод скрыт под UNITY_EDITOR — в билде Update не существует, значит
    // Unity не вызывает его каждый кадр (убираем пустой per-frame вызов).
    void Update()
    {
        if (manualControl && !isTransitioning && stages.Count > 0)
        {
            if (debugStageIndex != currentStage)
            {
                debugStageIndex = Mathf.Clamp(debugStageIndex, 0, stages.Count - 1);
                StartCoroutine(GoToStage(debugStageIndex));
            }
        }
    }
#endif

    // ����� �� �������� �������: ������ ��������
    public void CompleteStage()
    {
        if (isTransitioning) return;

        int nextStage = currentStage + 1;
        if (nextStage >= stages.Count) nextStage = 0; // �����������

        StartCoroutine(GoToStage(nextStage));
    }

    // Прямой переход на произвольный этап (для DayController / CoffeeCraftingSystem)
    public void JumpToStage(int index)
    {
        if (isTransitioning) return;
        StartCoroutine(GoToStage(index));
    }

    // ������� � ����������� �������
    private IEnumerator GoToStage(int targetStage)
    {
        if (isTransitioning || targetStage < 0 || targetStage >= stages.Count) yield break;

        isTransitioning = true;

        // ���� ��� ���������� ������ � �������� ��������� �������
        if (currentStage >= 0 && currentStage < stages.Count)
        {
            StageData prevStage = stages[currentStage];
            if (prevStage.disableCharacter && playerCharacter != null)
            {
                playerCharacter.gameObject.SetActive(true);
            }

            // ������������ ������ ����������� �������
            if (prevStage.stageScript != null)
            {
                prevStage.stageScript.enabled = false;
            }
        }

        currentStage = targetStage;
        StageData stage = stages[currentStage];

        // ��������� ��������� ���� �����
        if (stage.disableCharacter && playerCharacter != null)
        {
            playerCharacter.gameObject.SetActive(false);
        }

        // ���������� ������ ���� ���� ����
        if (stage.cameraTarget != null && mainCamera != null)
        {
            Vector3 startPos = mainCamera.transform.position;
            Vector3 endPos = stage.cameraTarget.position;
            // Подъём камеры на этапе ожидания гостя — смотрим на подходящего гостя чуть свысока.
            if (currentStage == guestWaitStageIndex) endPos.y += guestWaitCameraLift;
            Quaternion startRot = mainCamera.transform.rotation;
            Quaternion endRot = stage.cameraTarget.rotation;
            float timer = 0f;

            while (timer < stage.cameraMoveDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / stage.cameraMoveDuration);
                mainCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
                mainCamera.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }

            // ��������������� ��������� ������ ��������
            mainCamera.transform.position = endPos;
            mainCamera.transform.rotation = endRot;
        }

        // ���������� ������ �������
        if (stage.stageScript != null && stage.isEnabled)
        {
            stage.stageScript.enabled = true;
        }
        else if (stage.autoCompleteIfNoScript && stage.isEnabled)
        {
            // �������������� ����� �������� ��������
            yield return new WaitForSeconds(0.2f);
            CompleteStage();
        }

        // ������� ��� ������� � ���������� �������
        if (currentStage == 0)
        {
            onReadyForCustomer.Invoke();
        }
        else if (currentStage == stages.Count - 1)
        {
            onReadyForEffects.Invoke();
        }

        OnStageEntered?.Invoke(currentStage);
        isTransitioning = false;
    }

    // �������� ������� ������
    public int GetCurrentStageIndex()
    {
        return currentStage;
    }
}