/// <summary>
/// Туториал: пошаговое обучение с текстом снизу и подсветкой объектов.
/// Текст — через существующий DialogueDisplayer, камера — через машину этапов Stages.
/// Сцена: MainScene
/// Зависимости: DialogueDisplayer, Stages, CoffeeCraftingSystem
/// SDK: Нет
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    // ─── Инспектор ───────────────────────────────────────────────────────────

    [Header("Зависимости")]
    [SerializeField] private DialogueDisplayer    _dialogue;
    [SerializeField] private CoffeeCraftingSystem _craftingSystem;

    [Header("Объекты для подсветки (назначить в инспекторе)")]
    [SerializeField] private GameObject _ingredientsZoneObject; // Полка с ингредиентами
    [SerializeField] private GameObject _machineZoneObject;     // Кофемашина
    [SerializeField] private GameObject _toppingsZoneObject;    // Стойка топпингов
    [SerializeField] private GameObject _counterObject;         // Прилавок

    [Header("Эффект подсветки")]
    [SerializeField] private GameObject _highlightRingPrefab;   // Светящееся кольцо (UI или 3D)

    [Header("Камера (через машину этапов)")]
    [SerializeField] private Stages _stages;

    // ─── Данные туториала ─────────────────────────────────────────────────────

    [System.Serializable]
    public class TutorialStep
    {
        public string   speakerName   = "Мастер кофе";
        [TextArea(2, 4)]
        public string   text          = "";
        public GameObject highlightObject; // Что подсвечивать (может быть null)
        public int      stageIndex    = -1; // Этап Stages для камеры; -1 = не двигаем
        public float    pauseAfter    = 0.3f;
    }

    [Header("Шаги туториала")]
    [SerializeField] private List<TutorialStep> _steps = new List<TutorialStep>();

    // ─── Состояние ───────────────────────────────────────────────────────────

    private List<GameObject> _activeHighlights = new List<GameObject>();

    // ─── Публичное API ───────────────────────────────────────────────────────

    /// <summary>Запускает полный туториал. Awaitable.</summary>
    public IEnumerator RunTutorial()
    {
        // Если шаги не настроены в инспекторе — используем дефолтные
        if (_steps.Count == 0)
            BuildDefaultSteps();

        foreach (var step in _steps)
        {
            // Двигаем камеру через машину этапов
            if (step.stageIndex >= 0 && _stages != null)
            {
                while (_stages.IsTransitioning) yield return null;
                _stages.JumpToStage(step.stageIndex);
                yield return null;
                while (_stages.IsTransitioning) yield return null;
            }

            // Подсвечиваем объект
            ClearHighlights();
            if (step.highlightObject != null)
                HighlightObject(step.highlightObject);

            // Показываем текст
            var lines = new List<DialogueLine>
            {
                new DialogueLine { speakerName = step.speakerName, text = step.text }
            };
            yield return StartCoroutine(_dialogue.PlayDialogueLines(lines));

            ClearHighlights();

            if (step.pauseAfter > 0f)
                yield return new WaitForSeconds(step.pauseAfter);
        }

        // Туториал закончен — первый реальный заказ
        var finalLines = new List<DialogueLine>
        {
            new DialogueLine
            {
                speakerName = Loc.T("Мастер кофе", "Coffee Master"),
                text = Loc.T("Отлично! Ты всё поняла. Кажется, первый гость уже у двери... Удачи, хозяйка «Междумирья»!",
                             "Great! You've got it. Looks like the first guest is already at the door... Good luck, keeper of the Inbetween!")
            }
        };
        yield return StartCoroutine(_dialogue.PlayDialogueLines(finalLines));
    }

    // ─── Подсветка объекта ───────────────────────────────────────────────────

    private void HighlightObject(GameObject target)
    {
        if (target == null) return;

        if (_highlightRingPrefab != null)
        {
            // Создаём кольцо вокруг объекта
            GameObject ring = Instantiate(_highlightRingPrefab, target.transform.position, Quaternion.identity);
            ring.transform.SetParent(target.transform);
            _activeHighlights.Add(ring);
        }
        else
        {
            // Fallback: меняем материал/цвет рендерера
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                // Сохраняем оригинальный цвет через PropertyBlock
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                block.SetColor("_EmissionColor", new Color(0.3f, 0.8f, 0.3f) * 2f);
                r.SetPropertyBlock(block);

                // Создаём GameObject для сброса
                var resetter = target.AddComponent<HighlightResetter>();
                resetter.TargetRenderer = r;
                _activeHighlights.Add(resetter.gameObject);
                break; // Только первый рендерер
            }
        }

        // Пульсация масштаба
        StartCoroutine(PulseObject(target));
    }

    private IEnumerator PulseObject(GameObject target)
    {
        if (target == null) yield break;

        Vector3 originalScale = target.transform.localScale;
        float elapsed = 0f;

        while (_activeHighlights.Count > 0)
        {
            elapsed += Time.deltaTime;
            float pulse = 1f + 0.05f * Mathf.Sin(elapsed * 4f);
            if (target != null)
                target.transform.localScale = originalScale * pulse;
            yield return null;
        }

        if (target != null)
            target.transform.localScale = originalScale;
    }

    private void ClearHighlights()
    {
        foreach (var h in _activeHighlights)
        {
            if (h != null)
            {
                // Убираем component HighlightResetter если он есть
                var resetter = h.GetComponent<HighlightResetter>();
                resetter?.Reset();
                if (h.transform.parent != null && h != h.transform.parent.gameObject)
                    Destroy(h);
            }
        }
        _activeHighlights.Clear();
    }

    // ─── Дефолтные шаги туториала ────────────────────────────────────────────

    private void BuildDefaultSteps()
    {
        string master = Loc.T("Мастер кофе", "Coffee Master");

        _steps = new List<TutorialStep>
        {
            new TutorialStep
            {
                speakerName = master,
                text        = Loc.T("Добрый день, хозяйка! Я — Мастер Кофейного Дела. Позволь показать тебе, как работает «Междумирье».",
                                    "Good day, keeper! I am the Master of the Coffee Craft. Let me show you how the Inbetween works."),
                stageIndex = 1
            },
            new TutorialStep
            {
                speakerName    = master,
                text           = Loc.T("Когда к тебе придёт гость — он расскажет, что хочет выпить. Над его головой ты увидишь полоску. Чем дольше ждёшь — тем она короче.",
                                       "When a guest arrives, they will tell you what they want to drink. You'll see a bar above their head. The longer you wait — the shorter it gets."),
                highlightObject= _counterObject,
                stageIndex = 1
            },
            new TutorialStep
            {
                speakerName    = master,
                text           = Loc.T("Начало готовки — вот эта полка с ингредиентами. Нажми на напиток, который заказал гость.",
                                       "Brewing starts at this ingredient shelf. Tap the drink the guest ordered."),
                highlightObject= _ingredientsZoneObject,
                stageIndex = 2
            },
            new TutorialStep
            {
                speakerName    = master,
                text           = Loc.T("После выбора ингредиента — сюда, к кофемашине. Здесь ты выбираешь объём чашки и сладость. Нажми «Заварить».",
                                       "After picking the ingredient — over here, to the coffee machine. Choose the cup size and sweetness. Press \"Brew\"."),
                highlightObject= _machineZoneObject,
                stageIndex = 3
            },
            new TutorialStep
            {
                speakerName    = master,
                text           = Loc.T("И последнее — топпинги! Можно добавить сливки, корицу или карамель. Если гость не просил — выбери «Без топпинга».",
                                       "And finally — toppings! You can add cream, cinnamon or caramel. If the guest didn't ask — pick \"No topping\"."),
                highlightObject= _toppingsZoneObject,
                stageIndex = 4
            },
            new TutorialStep
            {
                speakerName    = master,
                text           = Loc.T("Когда всё готово — возвращайся к стойке и нажми «Подать». Если угадаешь заказ — гость будет доволен и расскажет кое-что интересное...",
                                       "When everything is ready — return to the counter and press \"Serve\". Guess the order right, and the guest will be pleased and share something interesting..."),
                highlightObject= _counterObject,
                stageIndex = 1
            },
            new TutorialStep
            {
                speakerName = master,
                text        = Loc.T("Три ошибки за день — и день придётся начать заново. Будь внимательна! И помни: если совсем не понятно — можно взять подсказку (кнопка снизу).",
                                    "Three mistakes in a day — and the day starts over. Stay sharp! And remember: if you're lost, you can take a hint (button at the bottom)."),
                stageIndex = 1
            }
        };
    }
}

// ─── Вспомогательный компонент для сброса подсветки ──────────────────────────

public class HighlightResetter : MonoBehaviour
{
    public Renderer TargetRenderer;
    private MaterialPropertyBlock _block;

    private void Awake()
    {
        _block = new MaterialPropertyBlock();
    }

    public void Reset()
    {
        if (TargetRenderer == null) return;
        TargetRenderer.GetPropertyBlock(_block);
        _block.SetColor("_EmissionColor", Color.black);
        TargetRenderer.SetPropertyBlock(_block);
        Destroy(this);
    }
}
