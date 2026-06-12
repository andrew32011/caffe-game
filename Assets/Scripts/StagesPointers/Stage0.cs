/// <summary>
/// Stage0: Гость идёт к стойке (движение ProcessVisitor).
/// Сцена: MainScene
/// Зависимости: ProcessVisitor
/// SDK: Нет
/// </summary>
using UnityEngine;

public class Stage0 : MonoBehaviour
{
    private ProcessVisitor _processVisitor;

    private void OnEnable()
    {
        _processVisitor = FindObjectOfType<ProcessVisitor>();
        if (_processVisitor != null)
            _processVisitor.StartMoving();
        else
            Debug.LogWarning("Stage0: ProcessVisitor не найден.");
    }
}
