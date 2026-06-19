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

    [Header("Экономика (Батч 5: база = авторская кривая дня × масштаб)")]
    [Tooltip("Множитель к авторской базе оплаты дня (DayData.coinsPerCorrectOrder, 10..50). " +
             "Главный рычаг баланса под цель путешествия (CoinsUI.JourneyGoal).")]
    [SerializeField] private float _payScale = 4f;
    [Tooltip("Порог удовлетворённости, ниже которого клиент недоволен.")]
    [Range(0f, 1f)] [SerializeField] private float _satisfiedThreshold = 0.5f;

    [Header("Комбо за серию хороших напитков (Батч 3)")]
    [Tooltip("Качество напитка, начиная с которого он засчитывается в серию.")]
    [Range(0f, 1f)] [SerializeField] private float _comboGoodThreshold = 0.6f;
    [Tooltip("Прибавка к оплате за каждый уровень комбо.")]
    [SerializeField] private float _comboStep = 0.15f;
    [Tooltip("Максимум уровней комбо (ограничение множителя).")]
    [SerializeField] private int _comboMax = 5;

    // ─── Состояние ───────────────────────────────────────────────────────────

    private int  _coinsEarnedToday = 0;   // чистая прибыль за день (может быть < 0)
    private int  _currentDayNumber = 1;
    private bool _daySuccess       = false;

    public bool DaySuccess       => _daySuccess;
    public int  CoinsEarnedToday => _coinsEarnedToday;

    // ─── Главный метод ────────────────────────────────────────────────────────

    /// <summary>Запускает полный рабочий день. Awaitable coroutine.</summary>
    public IEnumerator RunDay(DayData dayData)
    {
        _coinsEarnedToday = 0;
        _currentDayNumber = dayData.dayNumber;
        _daySuccess       = false;
        _comboCount       = 0;                 // Батч 3: серия начинается заново каждый день
        UiEffects.Instance?.ShowCombo(0);

        _dialogue?.ShowDayIntro(dayData.dayNumber);
        yield return new WaitForSeconds(2f);

        // Батч 2: продолжаем день с того гостя, на котором игрока прервали.
        int startIndex = GameManager.Instance != null ? GameManager.Instance.ResumeCustomerIndex : 0;
        if (startIndex < 0 || startIndex >= dayData.customers.Count) startIndex = 0;

        for (int ci = startIndex; ci < dayData.customers.Count; ci++)
        {
            yield return StartCoroutine(
                ServeCustomer(dayData.customers[ci], dayData.coinsPerCorrectOrder));

            // Сохраняем прогресс внутри дня — обновление страницы продолжит со следующего гостя.
            GameManager.Instance?.SetCustomerIndex(ci + 1);

            yield return new WaitForSeconds(1f); // Пауза между гостями
        }

        // День завершён — сбрасываем внутридневной индекс и убираем индикатор комбо.
        GameManager.Instance?.SetCustomerIndex(0);
        UiEffects.Instance?.ShowCombo(0);

        // Пункт 9: провала по «3 ошибкам» больше нет. День считается провальным
        // только если кофейня отработала в минус (экономика — см. _coinsEarnedToday).
        _daySuccess = _coinsEarnedToday >= 0;
    }

    // ─── Один гость ───────────────────────────────────────────────────────────

    private int _consecutiveFails = 0; // подряд полностью проваленных напитков (пункт 4.1)
    private int _comboCount        = 0; // подряд хороших напитков (Батч 3)

    /// <summary>Батч 3: обновляет серию по качеству напитка и возвращает множитель
    /// оплаты (1 + уровень × шаг). Хороший напиток наращивает серию, плохой — сбрасывает.</summary>
    private float UpdateCombo(float result)
    {
        if (result >= _comboGoodThreshold)
        {
            _comboCount++;
            int level = Mathf.Clamp(_comboCount - 1, 0, _comboMax);
            if (_comboCount >= 2)
            {
                UiEffects.Instance?.ShowCombo(_comboCount);
                AudioController.Instance?.PlayStar();
            }
            return 1f + level * _comboStep;
        }

        _comboCount = 0;
        UiEffects.Instance?.ShowCombo(0);
        return 1f;
    }

    private IEnumerator ServeCustomer(DayCustomerEntry entry, int coinsPerOrder)
    {
        // 1. Ставим модель гостя на VisitorBasis. ГГ остаётся за стойкой ВИДИМЫМ
        //    (пункт 1: не пропадает на прощании/приходе — прячем его только в зонах
        //    готовки, см. CoffeeCraftingSystem.HideHeroWhenCameraLeaves).
        _craftingSystem.SetHeroVisible(true);
        _customerController.SpawnModel(GetCustomerPrefab(entry.stickmanIndex), entry.characterType);
        AudioController.Instance?.PlayCustomerIn();

        // 2. Этап 0: гость идёт к стойке (Stage0 запускает ProcessVisitor)
        yield return StartCoroutine(GoToStageAndWait(_stageGuestEnter));
        yield return StartCoroutine(_customerController.WaitForRouteEnd());

        // 3. Этап 1: приветственный диалог — ГГ виден (пункт 2)
        yield return StartCoroutine(GoToStageAndWait(_stageGreeting));
        _craftingSystem.SetHeroVisible(true);
        yield return StartCoroutine(_dialogue.PlayDialogueLines(entry.greetingLines));

        // 4. Полоса показывает ЗАПОМНЕННУЮ удовлетворённость этого клиента (пункт 4.3)
        float stored = GameManager.Instance != null
            ? GameManager.Instance.GetClientSatisfaction(entry.characterType) : 0.5f;
        _hintManager?.SetCurrentOrder(entry.order);
        _customerController.SetSatisfaction(stored);

        // 5. Готовим напиток. Рекламная подсказка доступна после 2 провалов подряд (пункт 4.1)
        _craftingSystem.SetAdHintAllowed(_consecutiveFails >= 2);
        _craftingSystem.SetTargetOrder(entry.order);
        _craftingSystem.Show();
        yield return new WaitUntil(() => _craftingSystem.IsOrderReady);
        _craftingSystem.Hide();

        // 6. Оценка результата (доля совпавших параметров 0..1)
        float result = _craftingSystem.EvaluateSatisfaction();

        // Серия провалов: полностью неудачный напиток (< 0.3) копит счётчик
        if (result < 0.3f) _consecutiveFails++; else _consecutiveFails = 0;

        // Этап 5: возвращаемся к стойке, подаём кофе (ГГ снова виден — в ServeRoutine)
        yield return StartCoroutine(GoToStageAndWait(_stageServe));
        yield return new WaitForSeconds(0.3f);

        // Пункт 3: кружка мягко переходит из руки ГГ в руку гостя и исчезает там.
        // Делаем это на этапе подачи, где иначе «ничего не происходит».
        yield return StartCoroutine(
            _craftingSystem.HandCupToCustomer(_customerController.CurrentCustomer));

        // Батч 1 (сочность): оценка напитка 1–3⭐ + празднование + динь/идеально.
        int stars = result >= 0.8f ? 3 : result >= 0.5f ? 2 : 1;
        AudioController.Instance?.PlayServeDing();
        if (stars == 3) AudioController.Instance?.PlayPerfect();
        UiEffects.Instance?.Celebrate(stars);
        yield return new WaitForSeconds(0.5f);

        // 7. Обновляем ЗАПОМНЕННУЮ шкалу клиента (среднее старого и нового) (пункт 4.3)
        float newStored = Mathf.Clamp01(stored * 0.5f + result * 0.5f);
        GameManager.Instance?.SetClientSatisfaction(entry.characterType, newStored);
        _customerController.SetSatisfaction(newStored);
        yield return new WaitForSeconds(0.6f);

        // Пункт 2: клиент огорчён — можно поднять настроение комплиментом за монеты.
        // Деньги и удовольствие связаны: потратишь монеты — клиент подобреет (и заплатит лучше).
        if (newStored < _satisfiedThreshold)
        {
            float boosted = newStored;
            yield return StartCoroutine(_craftingSystem.TryCompliment(newStored, v => boosted = v));
            if (!Mathf.Approximately(boosted, newStored))
            {
                newStored = boosted;
                GameManager.Instance?.SetClientSatisfaction(entry.characterType, newStored);
                _customerController.SetSatisfaction(newStored);
                yield return new WaitForSeconds(0.4f);
            }
        }

        // 8. Экономика (Батч 5): база оплаты = авторская кривая дня (coinsPerOrder, 10..50)
        //    × масштаб, с мягким ранним разгоном (Difficulty.EarlyEase) вместо обрыва.
        //    Дальше — по качеству напитка и отношениям с клиентом (пункт 4.3).
        //    Батч 3: апгрейд «зёрна» повышает оплату; «лояльность» добавляет чаевые
        //    (только к оплате, не к запомненной шкале); комбо множит за серию.
        float dayBase = coinsPerOrder * _payScale;
        float early   = Difficulty.EarlyEase(_currentDayNumber);
        float upgMult = GameManager.Instance != null ? GameManager.Instance.PriceMultiplier : 1f;
        float tipMood = Mathf.Clamp01(newStored + (GameManager.Instance != null ? GameManager.Instance.MoodBonus : 0f));
        float combo   = UpdateCombo(result);
        int payment = Mathf.RoundToInt(dayBase * result * (0.5f + tipMood) * early * upgMult * combo);
        GameManager.Instance?.AddCoins(payment);
        _coinsEarnedToday += payment - _craftingSystem.CurrentDrinkCost;

        // 9. Реакция гостя — реплика зависит от отношений (пункт 4.3)
        yield return StartCoroutine(GoToStageAndWait(_stageReaction));
        bool happy = newStored >= _satisfiedThreshold;
        if (happy)
        {
            _customerController.ShowEmote(newStored >= 0.8f ? 2 : 1); // 😍 / 🙂
            UiEffects.Instance?.CoinBurst(payment); // пункт 5
            _vfxController?.PlayCoinEffect(payment);
            AudioController.Instance?.PlayCoin();
            AudioController.Instance?.PlayCorrectOrder();
            yield return StartCoroutine(_dialogue.PlayDialogueLines(entry.storyRevealLines));
        }
        else
        {
            _customerController.ShowEmote(0); // 😞
            _vfxController?.ShakeCamera(0.3f, 0.15f);
            AudioController.Instance?.PlayWrongOrder();
            yield return StartCoroutine(_dialogue.PlayDialogueLines(entry.wrongOrderLines));
        }

        // 10. Гость уходит. ГГ остаётся видимым за стойкой (пункт 1).
        yield return new WaitForSeconds(0.4f);
        yield return StartCoroutine(LetCustomerLeave());
    }

    // ─── Вспомогательные ─────────────────────────────────────────────────────

    /// <summary>Этап 7: Stage7 запускает обратный маршрут; ждём ухода и убираем модель.</summary>
    private IEnumerator LetCustomerLeave()
    {
        // Пункт 1: ГГ НЕ прячем — он остаётся за стойкой, пока гость уходит и пока
        // подходит следующий (раньше он тут пропадал и появлялся вновь).
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
