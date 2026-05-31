using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class StageData
{
    public string stageName = "Stage";
    public bool isEnabled = true;

    // Камера
    public Transform cameraTarget; // Может быть null
    public float cameraMoveDuration = 0.5f;

    // Персонаж
    public bool disableCharacter = false;

    // Логика стейджа
    public MonoBehaviour stageScript; // Может быть null
    public bool autoCompleteIfNoScript = true;
}

public class Stages : MonoBehaviour
{
    [Header("Stages")]
    public List<StageData> stages = new List<StageData>();

    [Header("References")]
    public Transform playerCharacter;
    public Camera mainCamera;

    [Header("Debug")]
    public bool manualControl = false; // Галка для ручного переключения в редакторе
    public int debugStageIndex = 0;

    [Header("Events")]
    public UnityEvent onReadyForCustomer; // Стейдж 0
    public UnityEvent onReadyForEffects;  // Последний стейдж

    private int currentStage = -1;
    private bool isTransitioning = false;

    void Start()
    {
        // Начинаем со стейджа 0 (ожидание посетителя)
        StartCoroutine(GoToStage(0));
    }

    void Update()
    {
        // Ручное переключение стейджей только в редакторе
#if UNITY_EDITOR
        if (manualControl && !isTransitioning && stages.Count > 0)
        {
            if (debugStageIndex != currentStage)
            {
                debugStageIndex = Mathf.Clamp(debugStageIndex, 0, stages.Count - 1);
                StartCoroutine(GoToStage(debugStageIndex));
            }
        }
#endif
    }

    // Вызов из внешнего скрипта: стейдж завершён
    public void CompleteStage()
    {
        if (isTransitioning) return;

        int nextStage = currentStage + 1;
        if (nextStage >= stages.Count) nextStage = 0; // Зацикливаем

        StartCoroutine(GoToStage(nextStage));
    }

    // Переход к конкретному стейджу
    private IEnumerator GoToStage(int targetStage)
    {
        if (isTransitioning || targetStage < 0 || targetStage >= stages.Count) yield break;

        isTransitioning = true;

        // Если был предыдущий стейдж — включаем персонажа обратно
        if (currentStage >= 0 && currentStage < stages.Count)
        {
            StageData prevStage = stages[currentStage];
            if (prevStage.disableCharacter && playerCharacter != null)
            {
                playerCharacter.gameObject.SetActive(true);
            }

            // Деактивируем скрипт предыдущего стейджа
            if (prevStage.stageScript != null)
            {
                prevStage.stageScript.enabled = false;
            }
        }

        currentStage = targetStage;
        StageData stage = stages[currentStage];

        // Дизейблим персонажа если нужно
        if (stage.disableCharacter && playerCharacter != null)
        {
            playerCharacter.gameObject.SetActive(false);
        }

        // Перемещаем камеру если есть цель
        if (stage.cameraTarget != null && mainCamera != null)
        {
            Vector3 startPos = mainCamera.transform.position;
            Vector3 endPos = stage.cameraTarget.position;
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

            // Гарантированная установка точных значений
            mainCamera.transform.position = endPos;
            mainCamera.transform.rotation = endRot;
        }

        // Активируем скрипт стейджа
        if (stage.stageScript != null && stage.isEnabled)
        {
            stage.stageScript.enabled = true;
        }
        else if (stage.autoCompleteIfNoScript && stage.isEnabled)
        {
            // Автозавершение через короткую задержку
            yield return new WaitForSeconds(0.2f);
            CompleteStage();
        }

        // События для первого и последнего стейджа
        if (currentStage == 0)
        {
            onReadyForCustomer.Invoke();
        }
        else if (currentStage == stages.Count - 1)
        {
            onReadyForEffects.Invoke();
        }

        isTransitioning = false;
    }

    // Получить текущий стейдж
    public int GetCurrentStageIndex()
    {
        return currentStage;
    }
}