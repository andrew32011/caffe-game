/// <summary>
/// Stage7: Гость уходит — запускает обратный маршрут ProcessVisitor
/// (зеркально Stage0, который запускает приход гостя).
/// Сцена: MainScene
/// Зависимости: ProcessVisitor
/// SDK: Нет
/// </summary>
using UnityEngine;

public class Stage7 : MonoBehaviour
{
    private ProcessVisitor _processVisitor;

    private void OnEnable()
    {
        _processVisitor = FindObjectOfType<ProcessVisitor>();
        if (_processVisitor != null)
            _processVisitor.StartMovingBackwards();
        else
            Debug.LogWarning("Stage7: ProcessVisitor не найден.");
    }
}
