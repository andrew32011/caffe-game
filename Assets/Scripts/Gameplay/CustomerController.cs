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

    [Header("Локомоция (пункт 2): покой при общении, ходьба при движении")]
    [Tooltip("Контроллер покоя (idle) — включается, когда гость стоит у стойки и общается.")]
    [SerializeField] private RuntimeAnimatorController _idleController;
    [Tooltip("Контроллер ходьбы — когда гость идёт. Если пусто, берётся controller существующего гостя.")]
    [SerializeField] private RuntimeAnimatorController _walkController;

    private Avatar _botAvatar; // аватар рига (для гуманоидов) — снимается с _existingGuest
    private bool?  _appliedWalking; // последнее применённое состояние локомоции
    private RuntimeAnimatorController _spawnDefaultController; // исходный контроллер модели (запасная ходьба)

    [Header("Полоска удовлетворённости")]
    [SerializeField] private SatisfactionBar _satisfactionBarPrefab;
    [SerializeField] private float _barHeightOffset = 2.2f;

    [Header("Аура существ (пункт 1): партиклы над головой гостей-существ")]
    [Tooltip("Префабы эффектов: [0]искры, [1]дым, [2]пламя, [3]щит/вода, [4]портал.")]
    [SerializeField] private GameObject[] _creatureAuraPrefabs;
    [SerializeField] private float _auraHeadOffset = 2.5f;
    [SerializeField] private float _auraScale = 0.5f;

    [Header("Эмоция гостя (Батч 1): спрайты-реакции над головой")]
    [Tooltip("[0] грусть/недовольство, [1] нейтрально/ок, [2] восторг.")]
    [SerializeField] private Sprite[] _emoteSprites;
    [SerializeField] private float _emoteHeadOffset = 2.9f;
    [SerializeField] private float _emoteScale = 0.6f;

    private GameObject _auraInstance;

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

    /// <summary>Ставит модель гостя (stickman) на VisitorBasis. Старую убирает.
    /// type — для ауры существ (пункт 1).</summary>
    public void SpawnModel(GameObject stickmanPrefab, CharacterType type = CharacterType.Traveler)
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
            // Запоминаем исходный контроллер модели — это запасная анимация ходьбы.
            _spawnDefaultController = _animator.runtimeAnimatorController;
            if (_botAvatar != null) _animator.avatar = _botAvatar;
            // Гость только появился и стоит — начинаем с покоя (idle), пункт 2.
            _appliedWalking = null;
            ApplyLocomotion(false);
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

        // Пункт 1: аура-партиклы над головой, если гость — вымышленное существо.
        int auraIdx = AuraIndexFor(type);
        if (auraIdx >= 0 && _creatureAuraPrefabs != null && auraIdx < _creatureAuraPrefabs.Length
            && _creatureAuraPrefabs[auraIdx] != null)
        {
            _auraInstance = Instantiate(
                _creatureAuraPrefabs[auraIdx],
                _visitorRoot.position + Vector3.up * _auraHeadOffset,
                Quaternion.identity,
                _visitorRoot);
            _auraInstance.transform.localScale = Vector3.one * _auraScale;
        }

        _satisfactionValue   = 50f;
        _satisfactionRunning = false;
    }

    // Какой эффект-ауру дать существу (или -1 — обычный человек, без ауры).
    private int AuraIndexFor(CharacterType t)
    {
        switch (t)
        {
            case CharacterType.FireAlchemist:  return 2; // пламя
            case CharacterType.WaterGuard:     return 3; // щит/вода
            case CharacterType.ShadowMerchant:
            case CharacterType.FogHunter:      return 1; // дым
            case CharacterType.TimeCourier:
            case CharacterType.MirrorThief:
            case CharacterType.EchoTwin:       return 4; // портал
            case CharacterType.StarShepherd:
            case CharacterType.CrystalSinger:
            case CharacterType.MoonSmith:
            case CharacterType.Lamplighter:    return 0; // искры
            default:                           return -1; // обычные люди — без ауры
        }
    }

    /// <summary>Батч 1: показывает эмоцию-реакцию над головой гостя (mood: 0 грусть,
    /// 1 ок, 2 восторг). Спрайт смотрит в камеру и плавно растворяется.</summary>
    public void ShowEmote(int mood)
    {
        if (_visitorRoot == null || _emoteSprites == null || _emoteSprites.Length == 0) return;
        int idx = Mathf.Clamp(mood, 0, _emoteSprites.Length - 1);
        var sprite = _emoteSprites[idx];
        if (sprite == null) return;

        var go = new GameObject("CustomerEmote");
        go.transform.SetParent(_visitorRoot, false);
        go.transform.localPosition = Vector3.up * _emoteHeadOffset;
        go.transform.localScale = Vector3.one * _emoteScale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        StartCoroutine(EmoteRoutine(go, sr));
    }

    private IEnumerator EmoteRoutine(GameObject go, SpriteRenderer sr)
    {
        var cam = Camera.main;
        float t = 0f, life = 1.8f;
        Vector3 baseLocal = go.transform.localPosition;
        while (t < life && go != null)
        {
            t += Time.deltaTime;
            // лёгкое всплытие + биллборд к камере
            go.transform.localPosition = baseLocal + Vector3.up * (0.3f * (t / life));
            if (cam != null) go.transform.rotation = Quaternion.LookRotation(go.transform.position - cam.transform.position);
            // поп в начале, угасание в конце
            float scale = _emoteScale * Mathf.SmoothStep(0.2f, 1f, Mathf.Min(1f, t * 5f));
            go.transform.localScale = Vector3.one * scale;
            if (sr != null && t > life - 0.5f)
            {
                var c = sr.color; c.a = Mathf.Clamp01((life - t) / 0.5f); sr.color = c;
            }
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    /// <summary>Убирает модель гостя, полоску и ауру.</summary>
    public void RemoveModel()
    {
        _satisfactionRunning = false;

        if (_auraInstance != null)    Destroy(_auraInstance);
        if (_satisfactionBar != null) Destroy(_satisfactionBar.gameObject);
        if (_currentModel != null)    Destroy(_currentModel);

        _auraInstance    = null;
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
        // Пункт 2: ходьба только когда гость реально движется по маршруту.
        // Когда стоит у стойки и общается — переключаем на покой (idle), а не
        // оставляем зацикленную анимацию ходьбы «на месте».
        if (_animator == null || _processVisitor == null) return;

        bool walking = _processVisitor.IsMoving;
        if (_appliedWalking != walking)
            ApplyLocomotion(walking);
    }

    // Переключает аниматор гостя между ходьбой и покоем.
    private void ApplyLocomotion(bool walking)
    {
        if (_animator == null) return;
        _appliedWalking = walking;

        var ctrl = walking
            ? (_walkController != null ? _walkController : (_botController != null ? _botController : _spawnDefaultController))
            : (_idleController != null ? _idleController : _spawnDefaultController);
        if (ctrl != null && _animator.runtimeAnimatorController != ctrl)
        {
            _animator.runtimeAnimatorController = ctrl;
            if (_botAvatar != null) _animator.avatar = _botAvatar;
        }

        // Если контроллер всё же параметрический — поддержим и его.
        if (_animator.HasParameter("Speed"))     _animator.SetFloat("Speed", walking ? 1f : 0f);
        if (_animator.HasParameter("IsWalking")) _animator.SetBool("IsWalking", walking);
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
