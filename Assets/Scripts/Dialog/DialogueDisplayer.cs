/// <summary>
/// Отображение диалогов. Базовый скрипт игры, расширен для сюжетного режима:
///  - старый API (ShowLine / ShowUniqueLine / HideDialogue + DialogueManager) сохранён;
///  - добавлены: печатная машинка, имя говорящего, клик для продолжения,
///    бубнёж (SpeechMixer), заставка дня, быстрые сообщения по центру экрана,
///    проигрывание списка сюжетных реплик (PlayDialogueLines) из StoryDatabase.
/// Сцена: MainScene
/// Зависимости: DialogueManager (опционально), SpeechMixer (опционально), TMPro
/// SDK: Нет
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueDisplayer : MonoBehaviour
{
    [Header("UI Elements (базовые)")]
    public Text dialogText;          // Старое текстовое поле (UI.Text) — можно оставить пустым
    public GameObject dialogBox;     // Панель диалога

    [Header("UI Elements (расширенные, TMP — приоритетнее UI.Text)")]
    public TextMeshProUGUI dialogTextTMP;   // Текст реплики (TMP)
    public TextMeshProUGUI speakerNameText; // Имя говорящего
    public GameObject continueHint;         // «Нажми, чтобы продолжить»

    [Header("Заставка дня")]
    public GameObject dayIntroPanel;
    public TextMeshProUGUI dayIntroText;

    [Header("Быстрое сообщение (центр экрана)")]
    public TextMeshProUGUI messageText;
    public CanvasGroup messageGroup;

    [Header("Печать текста")]
    public float typewriterSpeed = 35f;   // Символов в секунду
    public float autoAdvanceDelay = 0f;   // 0 = ждём клик

    [Header("References")]
    public DialogueManager manager;       // База реплик по ID (старая система)
    public SpeechMixer speechMixer;       // Бубнёж (уже был в проекте)

    // ─── Состояние ───────────────────────────────────────────────────────────

    private bool _isTyping        = false;
    private bool _clickedToSkip   = false;
    private bool _waitingForClick = false;

    private void Start()
    {
        if (dialogBox != null) dialogBox.SetActive(false);
        if (continueHint != null) continueHint.SetActive(false);
        if (dayIntroPanel != null) dayIntroPanel.SetActive(false);

        if (messageGroup != null)
        {
            messageGroup.alpha = 0f;
            messageGroup.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        // Клик/тап для пропуска печати или продолжения
        if (Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            if (_isTyping)
                _clickedToSkip = true;
            else if (_waitingForClick)
                _waitingForClick = false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  СТАРЫЙ API — работа через DialogueManager / DialogueDatabase
    // ═══════════════════════════════════════════════════════════════════════

    // Показать конкретную реплику
    public void ShowLine(string lineID, string characterID, bool conditionMet = false)
    {
        if (manager == null) return;

        string text = manager.GetLineWithFallback(lineID, characterID, conditionMet);

        if (!string.IsNullOrEmpty(text))
        {
            if (dialogBox != null) dialogBox.SetActive(true);
            SetDialogueText(text);
        }
        else
        {
            HideDialogue();
        }
    }

    // Показать случайную уникальную реплику (если условие выполнено)
    public void ShowUniqueLine(string characterID, bool conditionMet = true)
    {
        if (manager == null) return;

        string text = conditionMet
            ? manager.GetRandomUniqueLine(characterID, true)
            : manager.GetRandomCommonLine(characterID);

        if (!string.IsNullOrEmpty(text))
        {
            if (dialogBox != null) dialogBox.SetActive(true);
            SetDialogueText(text);
        }
        else
        {
            HideDialogue();
        }
    }

    public void HideDialogue()
    {
        if (dialogBox != null) dialogBox.SetActive(false);
        if (continueHint != null) continueHint.SetActive(false);
        SetDialogueText("");
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  НОВЫЙ API — сюжетные реплики (StoryDatabase) с печатной машинкой
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>Проигрывает список сюжетных реплик поочерёдно. Awaitable.</summary>
    public IEnumerator PlayDialogueLines(List<DialogueLine> lines)
    {
        if (lines == null || lines.Count == 0) yield break;

        if (dialogBox != null) dialogBox.SetActive(true);

        foreach (var line in lines)
        {
            yield return StartCoroutine(ShowLineRoutine(line.GetSpeaker(), line.GetText(), line.triggerSpeech));
        }

        HideDialogue();
    }

    /// <summary>Одна реплика с эффектом печати, ждёт клика для продолжения.</summary>
    private IEnumerator ShowLineRoutine(string speaker, string text, bool playSpeech = true)
    {
        if (speakerNameText != null) speakerNameText.text = speaker;
        if (continueHint != null) continueHint.SetActive(false);

        SetDialogueText("");
        _isTyping      = true;
        _clickedToSkip = false;

        // Бубнёж через существующий SpeechMixer
        if (playSpeech && speechMixer != null)
            speechMixer.PlayRandomFragment();

        // Печатная машинка
        int charIndex = 0;
        float interval = 1f / Mathf.Max(1f, typewriterSpeed);

        while (charIndex < text.Length)
        {
            if (_clickedToSkip)
            {
                SetDialogueText(text);
                break;
            }

            charIndex++;
            SetDialogueText(text.Substring(0, charIndex));
            yield return new WaitForSeconds(interval);
        }

        _isTyping = false;

        // Ждём клика (или авто-переход)
        if (continueHint != null) continueHint.SetActive(true);

        if (autoAdvanceDelay > 0f)
        {
            yield return new WaitForSeconds(autoAdvanceDelay);
        }
        else
        {
            _waitingForClick = true;
            yield return new WaitUntil(() => !_waitingForClick);
        }

        if (continueHint != null) continueHint.SetActive(false);
    }

    // ─── Заставка начала дня ─────────────────────────────────────────────────

    public void ShowDayIntro(int dayNumber)
    {
        if (dayIntroPanel == null || dayIntroText == null) return;

        dayIntroText.text = Loc.T($"ДЕНЬ {dayNumber}", $"DAY {dayNumber}");
        StartCoroutine(ShowDayIntroRoutine());
    }

    private IEnumerator ShowDayIntroRoutine()
    {
        dayIntroPanel.SetActive(true);
        yield return new WaitForSeconds(1.8f);
        dayIntroPanel.SetActive(false);
    }

    // ─── Быстрое сообщение в центре экрана ───────────────────────────────────

    /// <summary>Показывает сообщение и скрывает через displayTime (0 = ждать клика).</summary>
    public void ShowMessage(string text, float displayTime = 2.5f)
    {
        if (messageText != null) messageText.text = text;
        StartCoroutine(ShowMessageRoutine(displayTime));
    }

    private IEnumerator ShowMessageRoutine(float displayTime)
    {
        if (messageGroup == null) yield break;

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            messageGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        if (displayTime > 0f)
        {
            yield return new WaitForSeconds(displayTime);
        }
        else
        {
            bool clicked = false;
            while (!clicked)
            {
                if (Input.GetMouseButtonDown(0)) clicked = true;
                if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) clicked = true;
                yield return null;
            }
        }

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            messageGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        messageGroup.alpha = 0f;
        messageGroup.blocksRaycasts = false;
    }

    // ─── Вспомогательные ─────────────────────────────────────────────────────

    // Пишем и в старое поле (UI.Text), и в новое (TMP) — какое назначено
    private void SetDialogueText(string text)
    {
        if (dialogText != null) dialogText.text = text;
        if (dialogTextTMP != null) dialogTextTMP.text = text;
    }
}
