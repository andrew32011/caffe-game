/// <summary>
/// Вступление в первой сцене (SampleScene). Когда камера доезжает до последней точки
/// и фокусируется на книге — показываем полноэкранный UI с историей Миры. Снизу кнопка
/// «Продолжить»: сначала серая и неактивная, через 2 секунды загорается. По нажатию —
/// загрузка основной сцены игры.
/// Сцена: SampleScene
/// Зависимости: SmoothCameraWaypointController (вызывает Begin), Loc, TMPro
/// SDK: Нет
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class IntroStoryUI : MonoBehaviour
{
    public static IntroStoryUI Instance { get; private set; }

    [Header("UI (заполняется сборкой Build Intro)")]
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private TextMeshProUGUI _storyText;
    [SerializeField] private Button _continueButton;
    [SerializeField] private TextMeshProUGUI _continueLabel;

    [Header("Настройки")]
    [Tooltip("Через сколько секунд кнопка «Продолжить» загорается.")]
    [SerializeField] private float _activateDelay = 2f;
    [Tooltip("Индекс основной сцены в Build Settings (обычно 1).")]
    [SerializeField] private int _mainSceneIndex = 1;

    private bool _started;

    private void Awake()
    {
        Instance = this;
        if (_group != null)
        {
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.gameObject.SetActive(false);
        }
    }

    /// <summary>Запускает показ истории. Вызывается камерой на финальной точке.</summary>
    public void Begin()
    {
        if (_started) return;
        _started = true;
        StartCoroutine(Routine());
    }

    private IEnumerator Routine()
    {
        if (_storyText != null) _storyText.text = StoryText();
        SetButtonLit(false); // серая и неактивная

        if (_continueButton != null)
        {
            _continueButton.onClick.RemoveListener(OnContinue);
            _continueButton.onClick.AddListener(OnContinue);
        }

        if (_group != null)
        {
            _group.gameObject.SetActive(true);
            _group.blocksRaycasts = true;
            // Плавное появление (на unscaled-времени, чтобы не зависеть от Time.timeScale).
            float t = 0f;
            while (t < 1f) { t += Time.unscaledDeltaTime * 1.5f; _group.alpha = Mathf.Clamp01(t); yield return null; }
            _group.alpha = 1f;
        }

        yield return new WaitForSecondsRealtime(_activateDelay);
        SetButtonLit(true); // загорается через 2 секунды
    }

    private void SetButtonLit(bool lit)
    {
        if (_continueButton != null)
        {
            _continueButton.interactable = lit;
            var img = _continueButton.targetGraphic as Image;
            if (img != null)
                img.color = lit ? Color.white : new Color(0.45f, 0.45f, 0.5f, 0.7f);
        }
        if (_continueLabel != null)
            _continueLabel.color = lit ? Color.white : new Color(0.8f, 0.8f, 0.85f, 0.5f);
    }

    private void OnContinue()
    {
        if (_continueButton != null) _continueButton.interactable = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainSceneIndex);
    }

    private string StoryText()
    {
        return Loc.T(
            "В маленькой кофейне на самом краю миров жила Мира — и не было её сердцу " +
            "теплее места, чем рядом с её возлюбленным, Каем.\n\n" +
            "Они должны были обвенчаться на рассвете. Но в последнюю ночь перед свадьбой " +
            "за Каем пришли из тумана — и унесли его за грань, во тьму Зеркального Ущелья.\n\n" +
            "Теперь у Миры остались лишь аромат кофе, пустой стул напротив и одно обещание: " +
            "найти Кая, чего бы это ни стоило.\n\n" +
            "Свари надежду — по чашке за раз. И пусть она приведёт тебя к нему.",

            "In a little coffee house at the very edge of the worlds lived Mira — and her heart " +
            "knew no warmer place than beside her beloved, Kai.\n\n" +
            "They were to be wed at dawn. But on the last night before the wedding, they came for " +
            "Kai out of the fog — and carried him beyond the veil, into the dark of the Mirror Gorge.\n\n" +
            "Now all Mira has left is the scent of coffee, an empty chair across the table, and a " +
            "single promise: to find Kai, whatever it takes.\n\n" +
            "Brew hope — one cup at a time. And may it lead you to him.");
    }
}
