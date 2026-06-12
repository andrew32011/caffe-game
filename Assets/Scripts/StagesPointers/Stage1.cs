/// <summary>
/// Stage1: Гость у стойки — показывает диалог приветствия и заказа.
/// Активируется Stages.cs, сразу завершает этап после диалога.
/// </summary>
using UnityEngine;

public class Stage1 : MonoBehaviour
{
    private void OnEnable()
    {
        // Stage1 управляется через DayController — он запускает диалог напрямую.
        // После завершения диалога DayController вызовет Stages.CompleteStage()
        // Этот скрипт — маркер, что мы в фазе "диалог с гостем"
        Debug.Log("Stage1: Фаза приветствия гостя.");
    }
}
