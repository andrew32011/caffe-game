/// <summary>
/// Гость: подмена видимой модели (stickman) и полоска удовлетворённости.
/// ДВИЖЕНИЕМ НЕ УПРАВЛЯЕТ — за маршрут отвечает существующий ProcessVisitor
/// (объект ProcessVisitorManager в сцене, цель — VisitorBasis).
/// Сцена: MainScene
/// Зависимости: ProcessVisitor, SatisfactionBar
/// SDK: Нет
/// </summary>
using System.Collections;
using UnityEngine;

public class CustomerController : MonoBehaviour
{
    // ─── Инспектор ───────────────────────────────────────────────────────────

    [Header("Движение (существующая система)")]
    [SerializeField] private ProcessVisitor _processVisitor; // ProcessVisitorManager из сцены
    [SerializeField] private Transform _visitorRoot;         // VisitorBasis из сцены (двигается ProcessVisitor-ом)

    [Header("Существующий ходячий гость (шаблон визуала/анимации)")]
    [Tooltip("Уже стоящий/ходящий в сцене гость. С него копируется анимация и размер, " +
             "после чего его исходная модель удаляется — гостей рисуем подменой модели.")]
    [SerializeField] private GameObject _existingGuest;
    [Tooltip("Локальный масштаб модели (снимается с _existingGuest, либо задаётся вручную).")]
    [SerializeField] private Vector3 _botScale = Vector3.one;
    [Tooltip("Контроллер анимации (снимается с _existingGuest, либо задаётся вручную).")]
    [SerializeField] private RuntimeAnimatorController _botController;

    private Avatar _botAvatar; // аватар рига (для гуманоидов) — снимается с _existingGuest

    [Header("Полоска удовлетворённости")]
    [SerializeField] private SatisfactionBar _satisfactionBarPrefab;
    [SerializeField] private float _barHeightOffset = 2.2f;

    [Header("Удовлетворённость")]
    [SerializeField] private float _satisfactionDrainPerSec = 4f; // % в секунду пока гость ждёт

    // ─── Состояние ───────────────────────────────────────────────────────────

    private GameObject      _currentModel;     // Заспавненный stickman (ребёнок VisitorBasis)
    private Animator        _animator;
    private SatisfactionBar _satisfactionBar;

    private float _satisfactionValue   = 50f;  // 0..100 (гость приходит наполовину довольным — пункт 7)
    private bool  _satisfactionRunning = false;

    public float SatisfactionValue => _satisfactionValue;

    /// <summary>Текущая модель гостя (для передачи кружки в руку, пункт 3).</summary>
    public Transform CurrentCustomer => _currentModel != null ? _currentModel.transform : _visitorRoot;

    // ─── Захват шаблона из существующего гостя ───────────────────────────────

    private void Awake()
    {
        // Снимаем анимацию + размер с уже имеющегося в сцене ходячего гостя
        // и удаляем его исходную модель — дальше рисуем гостей подменой модели.
        if (_existingGuest != null)
        {
            _botScale = _existingGuest.transform.localScale;
            var anim = _existingGuest.GetComponentInChildren<Animator>();
            if (anim != null)
            {
                if (anim.runtimeAnimatorController != null) _botController = anim.runtimeAnimatorController;
                _botAvatar = anim.avatar;
            }
            Destroy(_existingGuest);
            _existingGuest = null;
        }
    }

    // ─── Модель гостя ────────────────────────────────────────────────────────

    /// <summary>Ставит модель гостя (stickman) на VisitorBasis. Старую убирает.</summary>
    public void SpawnModel(GameObject stickmanPrefab)
    {
        RemoveModel();

        if (stickmanPrefab == null || _visitorRoot == null)
        {
            Debug.LogWarning("CustomerController: не назначены префаб или VisitorBasis.");
            return;
        }

        _currentModel = Instantiate(stickmanPrefab, _visitorRoot);
        _currentModel.transform.localPosition = Vector3.zero;
        _currentModel.transform.localRotation = Quaternion.identity;

        // Размер и анимация — как у существующего в сцене бота (пункт 4)
        if (_botScale != Vector3.zero)
            _currentModel.transform.localScale = _botScale;

        _animator = _currentModel.GetComponentInChildren<Animator>();
        if (_animator != null)
        {
            if (_botController != null) _animator.runtimeAnimatorController = _botController;
            if (_botAvatar != null)     _animator.avatar = _botAvatar;
        }

        // Полоска удовлетворённости над головой
        if (_satisfactionBarPrefab != null)
        {
            _satisfactionBar = Instantiate(
                _satisfactionBarPrefab,
                _visitorRoot.position + Vector3.up * _barHeightOffset,
                Quaternion.identity,
                _visitorRoot);

            _satisfactionBar.SetValue(0.5f); // приходит наполовину довольным (пункт 7)
            _satisfactionBar.gameObject.SetActive(false);
        }

        _satisfactionValue   = 50f;
        _satisfactionRunning = false;
    }

    /// <summary>Убирает модель гостя и полоску.</summary>
    public void RemoveModel()
    {
        _satisfactionRunning = false;

        if (_satisfactionBar != null) Destroy(_satisfactionBar.gameObject);
        if (_currentModel != null) Destroy(_currentModel);

        _satisfactionBar = null;
        _currentModel    = null;
        _animator        = null;
    }

    // ─── Ожидание движения ProcessVisitor ────────────────────────────────────

    /// <summary>Ждёт, пока ProcessVisitor закончит текущий маршрут. Awaitable.</summary>
    public IEnumerator WaitForRouteEnd()
    {
        if (_processVisitor == null) yield break;

        // Даём кадру на запуск движения (Stage0/Stage7 включают его в OnEnable)
        yield return null;

        while (_processVisitor.IsMoving)
            yield return null;
    }

    private void Update()
    {
        // Анимация ходьбы по состоянию ProcessVisitor
        if (_animator == null || _processVisitor == null) return;

        bool walking = _processVisitor.IsMoving;
        if (_animator.HasParameter("Speed"))
            _animator.SetFloat("Speed", walking ? 1f : 0f);
        if (_animator.HasParameter("IsWalking"))
            _animator.SetBool("IsWalking", walking);
    }

    // ─── Удовлетворённость ───────────────────────────────────────────────────

    /// <summary>Запускает медленное уменьшение настроения (пока ждёт заказ).</summary>
    public void StartSatisfactionTimer()
    {
        if (_satisfactionBar != null) _satisfactionBar.gameObject.SetActive(true);
        _satisfactionRunning = true;
        StartCoroutine(DrainSatisfactionRoutine());
    }

    public void StopSatisfactionTimer()
    {
        _satisfactionRunning = false;
    }

    /// <summary>Резкое уменьшение (при ошибке заказа).</summary>
    public void DecreaseSatisfaction(float amount)
    {
        _satisfactionValue = Mathf.Max(0f, _satisfactionValue - amount);
        UpdateBar();
    }

    /// <summary>Заполнить полоску до 100% (полностью довольный гость).</summary>
    public void FillSatisfactionBar() => SetSatisfaction(1f);

    /// <summary>Плавно меняет полосу до заданного значения 0..1 (результат заказа, пункт 7).</summary>
    public void SetSatisfaction(float value01)
    {
        _satisfactionRunning = false;
        StartCoroutine(AnimateToValue(Mathf.Clamp01(value01) * 100f));
    }

    private IEnumerator DrainSatisfactionRoutine()
    {
        while (_satisfactionRunning && _satisfactionValue > 0f)
        {
            _satisfactionValue -= _satisfactionDrainPerSec * Time.deltaTime;
            _satisfactionValue  = Mathf.Max(0f, _satisfactionValue);
            UpdateBar();
            yield return null;
        }
    }

    private IEnumerator AnimateToValue(float target)
    {
        const float speed = 80f; // % в секунду
        while (Mathf.Abs(_satisfactionValue - target) > 0.5f)
        {
            _satisfactionValue = Mathf.MoveTowards(_satisfactionValue, target, speed * Time.deltaTime);
            UpdateBar();
            yield return null;
        }
        _satisfactionValue = target;
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (_satisfactionBar != null)
            _satisfactionBar.SetValue(_satisfactionValue / 100f);
    }
}

// ─── Расширение Animator ──────────────────────────────────────────────────────

public static class AnimatorExtensions
{
    public static bool HasParameter(this Animator animator, string paramName)
    {
        foreach (var param in animator.parameters)
            if (param.name == paramName) return true;
        return false;
    }
}
