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

    [Header("Настройки")]
    [SerializeField] private int _maxMistakesPerDay = 3;

    // ─── Состояние ───────────────────────────────────────────────────────────

    private int  _coinsEarnedToday = 0;
    private int  _mistakesCount    = 0;
    private bool _daySuccess       = false;

    public bool DaySuccess       => _daySuccess;
    public int  CoinsEarnedToday => _coinsEarnedToday;
    public int  MistakesCount    => _mistakesCount;

    // ─── Главный метод ────────────────────────────────────────────────────────

    /// <summary>Запускает полный рабочий день. Awaitable coroutine.</summary>
    public IEnumerator RunDay(DayData dayData)
    {
        _coinsEarnedToday = 0;
        _mistakesCount    = 0;
        _daySuccess       = false;

        _dialogue?.ShowDayIntro(dayData.dayNumber);
        yield return new WaitForSeconds(2f);

        foreach (DayCustomerEntry customerEntry in dayData.customers)
        {
            yield return StartCoroutine(
                ServeCustomer(customerEntry, dayData.coinsPerCorrectOrder));

            if (_mistakesCount >= _maxMistakesPerDay)
            {
                _daySuccess = false;
                yield break;
            }

            yield return new WaitForSeconds(1f); // Пауза между гостями
        }

        _daySuccess = true;
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

        // 4. Подсказка знает заказ; запускаем таймер настроения
        _hintManager?.SetCurrentOrder(entry.order);
        _customerController.StartSatisfactionTimer();

        // 5. Мини-игра приготовления (камеру по зонам двигает CoffeeCraftingSystem через Stages)
        bool orderCorrect = false;

        _craftingSystem.SetTargetOrder(entry.order);
        _craftingSystem.Show();

        while (!orderCorrect)
        {
            yield return new WaitUntil(() => _craftingSystem.IsOrderReady);

            CoffeeOrder prepared = _craftingSystem.GetPreparedOrder();
            orderCorrect = entry.order.Matches(prepared);

            if (orderCorrect)
            {
                // ─── ПРАВИЛЬНЫЙ ЗАКАЗ ────────────────────────────────────────
                _craftingSystem.Hide();
                _coinsEarnedToday += coinsPerOrder;

                // Этап 5: возвращаемся к стойке, подаём кофе
                yield return StartCoroutine(GoToStageAndWait(_stageServe));
                yield return new WaitForSeconds(0.5f);

                _customerController.FillSatisfactionBar();
                yield return new WaitForSeconds(0.5f);

                // Этап 6: реакция и сюжетное раскрытие
                yield return StartCoroutine(GoToStageAndWait(_stageReaction));
                yield return StartCoroutine(_dialogue.PlayDialogueLines(entry.storyRevealLines));

                _vfxController?.PlayCoinEffect(_coinsEarnedToday);
            }
            else
            {
                // ─── НЕПРАВИЛЬНЫЙ ЗАКАЗ ──────────────────────────────────────
                _mistakesCount++;
                _customerController.DecreaseSatisfaction(30f);

                yield return StartCoroutine(_dialogue.PlayDialogueLines(entry.wrongOrderLines));
                _vfxController?.ShakeCamera(0.3f, 0.15f);

                if (_mistakesCount >= _maxMistakesPerDay)
                {
                    _craftingSystem.Hide();
                    _dialogue.ShowMessage(
                        Loc.T("Слишком много ошибок! День начинается заново.",
                              "Too many mistakes! The day starts over."), 2.5f);
                    yield return new WaitForSeconds(2.5f);

                    yield return StartCoroutine(LetCustomerLeave());
                    yield break;
                }

                _craftingSystem.ResetCup();
                yield return new WaitForSeconds(0.5f);
            }
        }

        // 6. Гость уходит
        _customerController.StopSatisfactionTimer();
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(LetCustomerLeave());
        _craftingSystem.Hide();
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
