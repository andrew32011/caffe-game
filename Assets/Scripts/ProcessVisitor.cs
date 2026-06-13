using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProcessVisitor : MonoBehaviour
{
    [Header("Target to Move")]
    public Transform targetObject; // Объект, который будет двигаться (гость)

    [Header("Route Settings")]
    public List<Transform> routePoints = new List<Transform>(); // Точки маршрута
    public float moveSpeed = 2f; // Скорость движения
    public bool loopRoute = false; // Зацикливать маршрут или остановиться в конце

    [Header("Status")]
    public int currentPointIndex = 0;

    private bool isMoving = false;
    private Transform currentTarget = null;

    // Для внешних скриптов: идёт ли гость сейчас (анимация, ожидание прихода)
    public bool IsMoving => isMoving;

    private int moveDirection = 1; // 1 = вперёд, -1 = назад
    private bool pingPongLoop = true; // true = туда-обратно, false = просто зацикливать

    void Start()
    {
        if (targetObject == null)
        {
            Debug.LogError("ProcessVisitor: targetObject is not assigned!");
            return;
        }

        //if (routePoints.Count > 0)
        //{
        //    StartMoving();
        //}
    }

    void Update()
    {
        if (!isMoving || currentTarget == null || targetObject == null) return;

        // Двигаем целевой объект к точке (плавно, без «притягивания»)
        targetObject.position = Vector3.MoveTowards(
            targetObject.position, currentTarget.position, moveSpeed * Time.deltaTime);

        // Плавный поворот в сторону движения (а не мгновенный разворот)
        Vector3 direction = currentTarget.position - targetObject.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            Quaternion look = Quaternion.LookRotation(direction.normalized, Vector3.up);
            targetObject.rotation = Quaternion.Slerp(
                targetObject.rotation, look, 6f * Time.deltaTime);
        }

        // Достигли точки — переходим к следующей (без жёсткого «прилипания»)
        if (Vector3.Distance(targetObject.position, currentTarget.position) < 0.05f)
        {
            SetNextTarget();
        }
    }

    public void StartMoving()
    {
        if (targetObject == null || routePoints.Count == 0) return;

        currentPointIndex = 0;
        moveDirection = 1; // Сброс направления (после StartMovingBackwards остаётся -1)
        SetNextTarget();
        isMoving = true;
    }

    public void StopMoving()
    {
        isMoving = false;
    }

    // Двигаться к первой точке (уход из кафе)
    public void StartMovingBackwards()
    {
        if (targetObject == null || routePoints.Count == 0) return;

        // Начинаем с текущей позиции гостя → идём к первой точке
        currentPointIndex = 0; // Индекс первой точки
        moveDirection = -1;    // Движение назад

        // Находим ближайшую точку маршрута к текущей позиции гостя
        float minDist = float.MaxValue;
        int nearestIndex = routePoints.Count - 1;

        for (int i = 0; i < routePoints.Count; i++)
        {
            float dist = Vector3.Distance(targetObject.position, routePoints[i].position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestIndex = i;
            }
        }

        // Начинаем движение от ближайшей точки к началу
        currentPointIndex = nearestIndex;
        moveDirection = -1;

        SetNextTarget();
        isMoving = true;
    }

    // Остановиться и сохранить текущую точку для продолжения
    public void PauseRoute()
    {
        isMoving = false;
    }

    // Возобновить движение в текущем направлении
    public void ResumeRoute()
    {
        if (targetObject == null || routePoints.Count == 0) return;

        SetNextTarget();
        isMoving = true;
    }

    private void SetNextTarget()
    {
        // Если достигли конца маршрута вперёд
        if (currentPointIndex >= routePoints.Count && moveDirection == 1)
        {
            if (loopRoute)
            {
                if (pingPongLoop)
                {
                    // Пинг-понг: разворачиваемся и идём назад
                    moveDirection = -1;
                    currentPointIndex = routePoints.Count - 2; // Предпоследняя точка
                }
                else
                {
                    // Обычный луп: возвращаемся к началу
                    currentPointIndex = 0;
                }
            }
            else
            {
                isMoving = false;
                OnRouteCompleted?.Invoke();
                return;
            }
        }
        // Если достигли начала маршрута при движении назад
        else if (currentPointIndex < 0 && moveDirection == -1)
        {
            if (loopRoute)
            {
                if (pingPongLoop)
                {
                    // Пинг-понг: разворачиваемся и идём вперёд
                    moveDirection = 1;
                    currentPointIndex = 1; // Вторая точка
                }
                else
                {
                    // Обычный луп: возвращаемся к концу
                    currentPointIndex = routePoints.Count - 1;
                }
            }
            else
            {
                isMoving = false;
                OnRouteCompleted?.Invoke();
                return;
            }
        }

        // Устанавливаем текущую цель
        currentTarget = routePoints[currentPointIndex];
        currentPointIndex += moveDirection;
    }

    public System.Action OnRouteCompleted;

}