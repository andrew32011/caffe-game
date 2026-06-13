/// <summary>
/// Контроллер одного рабочего дня. Ведёт гостей по существующей машине этапов
/// Stages (этапы 0–7, камера двигается логикой Stages через cameraTarget):
///   0 — гость идёт к стойке (Stage0 → ProcessVisitor.StartMoving)
///   1 — приветствие и заказ (диалог)
///   2 — полка ингредиентов   ┐
///   3 — кофемашина           ├ зоны готовки (навигация из CoffeeCraftingSystem)
///   4 — топпинги             ┘
///   5 — возврат к стойке, подача
///   6 — реакция гостя / сюжетная реплика
///   7 — гость уходит (Stage7 → ProcessVisitor.StartMovingBackwards)
/// Сцена: MainScene
/// Зависимости: Stages, ProcessVisitor (через CustomerController), CoffeeCraftingSystem,
///               DialogueDisplayer, HintManager
/// SDK: Нет
/// </summary>
using System.Collections;
using UnityEngine;

public class DayController : MonoBehaviour
{
    // ─── Инспектор ───────────────────────────────────────────────────────────

    [Header("Машина этапов (существующая, объект StagesScripts)")]
    [SerializeField] private Stages _stages;

    [Header("Зависимости")]
    [SerializeField] private CustomerController     _customerController;
    [SerializeField] private CoffeeCraftingSystem   _craftingSystem;
    [SerializeField] private DialogueDisplayer      _dialogue;
    [SerializeField] private HintManager            _hintManager;
    [SerializeField] private VisualEffectsController _vfxController;

    [Header("Префабы гостей (stickman_1..9 из PrefsAll)")]
    [SerializeField] private GameObject[] _customerPrefabs;

    [Header("Индексы этапов Stages")]
    [SerializeField] private int _stageGuestEnter = 0;
    [SerializeField] private int _stageGreeting   = 1;
    [SerializeField] private int _stageServe      = 5;
    [SerializeField] private int _stageReaction   = 6;
    [SerializeField] private int _stageGuestLeave = 7;

    [Header("Экономика (пункт 8)")]
    [Tooltip("Сколько максимум платит полностью довольный клиент.")]
    [SerializeField] private int _basePrice = 40;
    [Tooltip("Себестоимость основы напитка (вычитается всегда).")]
    [SerializeField] private int _ingredientCost = 8;
    [Tooltip("Себестоимость одного топпинга.")]
    [SerializeField] private int _toppingCost = 3;
    [Tooltip("Порог удовлетворённости, ниже которого клиент недоволен (показ подсказок).")]
    [Range(0f, 1f)] [SerializeField] private float _satisfiedThreshold = 0.5f;

    // ─── Состояние ───────────────────────────────────────────────────────────

    private int  _coinsEarnedToday = 0;   // чистая прибыль за день (может быть < 0)
    private bool _daySuccess       = false;

    public bool DaySuccess       => _daySuccess;
    public int  CoinsEarnedToday => _coinsEarnedToday;

    // ─── Главный метод ────────────────────────────────────────────────────────

    /// <summary>Запускает полный рабочий день. Awaitable coroutine.</summary>
    public IEnumerator RunDay(DayData dayData)
    {
        _coinsEarnedToday = 0;
        _daySuccess       = false;

        _dialogue?.ShowDayIntro(dayData.dayNumber);
        yield return new WaitForSeconds(2f);

        foreach (DayCustomerEntry customerEntry in dayData.customers)
        {
            yield return StartCoroutine(
                ServeCustomer(customerEntry, dayData.coinsPerCorrectOrder));

            yield return new WaitForSeconds(1f); // Пауза между гостями
        }

        // Пункт 9: провала по «3 ошибкам» больше нет. День считается провальным
        // только если кофейня отработала в минус (экономика — см. _coinsEarnedToday).
        _daySuccess = _coinsEarnedToday >= 0;
    }

    // ─── Один гость ───────────────────────────────────────────────────────────

    private IEnumerator ServeCustomer(DayCustomerEntry entry, int coinsPerOrder)
    {
        // 1. Ставим модель гостя на VisitorBasis
        _customerController.SpawnModel(GetCustomerPrefab(entry.stickmanIndex));

        // 2. Этап 0: гость идёт к стойке (Stage0 запускает ProcessVisitor)
        yield return StartCoroutine(GoToStageAndWait(_stageGuestEnter));
        yield return StartCoroutine(_customerController.WaitForRouteEnd());

        // 3. Этап 1: приветственный диалог у стойки
        yield return StartCoroutine(GoToStageAndWait(_stageGreeting));
        yield return StartCoroutine(_dialogue.PlayDialogueLines(entry.greetingLines));

        // 4. Подсказка знает заказ; запускаем таймер настроения (стартует с 50%)
        _hintManager?.SetCurrentOrder(entry.order);
        _customerController.StartSatisfactionTimer();

        // 5. Готовим напиток (ингредиент → машина → топпинги → «Подать»)
        _craftingSystem.SetTargetOrder(entry.order);
        _craftingSystem.Show();
        yield return new WaitUntil(() => _craftingSystem.IsOrderReady);
        _craftingSystem.Hide();

        // 6. Оценка результата (доля совпавших параметров 0..1)
        float satisfaction = _craftingSystem.EvaluateSatisfaction();
        _customerController.StopSatisfactionTimer();

        // Этап 5: возвращаемся к стойке, подаём кофе
        yield return StartCoroutine(GoToStageAndWait(_stageServe));
        yield return new WaitForSeconds(0.3f);

        // Меняем полосу удовлетворённости под результат (пункт 7)
        _customerController.SetSatisfaction(satisfaction);
        yield return new WaitForSeconds(0.6f);

        // 7. Экономика: оплата по удовлетворённости минус себестоимость (пункт 8)
        int payment = Mathf.RoundToInt(_basePrice * satisfaction);
        int cost    = _ingredientCost + _toppingCost * _craftingSystem.ChosenToppingCount;
        int profit  = payment - cost;
        _coinsEarnedToday += profit;

        // 8. Реакция гостя
        yield return StartCoroutine(GoToStageAndWait(_stageReaction));
        bool happy = satisfaction >= _satisfiedThreshold;
        if (happy)
        {
            _vfxController?.PlayCoinEffect(payment);
            yield return StartCoroutine(_dialogue.PlayDialogueLines(entry.storyRevealLines));
        }
        else
        {
            _vfxController?.ShakeCamera(0.3f, 0.15f);
            yield return StartCoroutine(_dialogue.PlayDialogueLines(entry.wrongOrderLines));
        }

        // 9. Гость уходит
        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(LetCustomerLeave());
    }

    // ─── Вспомогательные ─────────────────────────────────────────────────────

    /// <summary>Этап 7: Stage7 запускает обратный маршрут; ждём ухода и убираем модель.</summary>
    private IEnumerator LetCustomerLeave()
    {
        yield return StartCoroutine(GoToStageAndWait(_stageGuestLeave));
        yield return StartCoroutine(_customerController.WaitForRouteEnd());
        _customerController.RemoveModel();
    }

    /// <summary>Переход на этап Stages и ожидание конца перехода (движения камеры).</summary>
    private IEnumerator GoToStageAndWait(int stageIndex)
    {
        if (_stages == null) yield break;

        // Дожидаемся конца предыдущего перехода
        while (_stages.IsTransitioning) yield return null;

        _stages.JumpToStage(stageIndex);
        yield return null;

        while (_stages.IsTransitioning) yield return null;
    }

    private GameObject GetCustomerPrefab(int index)
    {
        if (_customerPrefabs == null || _customerPrefabs.Length == 0)
        {
            Debug.LogWarning("DayController: нет префабов гостей!");
            return null;
        }
        int safeIndex = Mathf.Clamp(index, 0, _customerPrefabs.Length - 1);
        return _customerPrefabs[safeIndex];
    }
}
