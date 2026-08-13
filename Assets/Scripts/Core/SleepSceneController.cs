/// <summary>
/// Контроллер сцены сна («Sleepy scene»). Проигрывает «сон» между днями: ночной звук,
/// титр «СОН», визуальный эффект и текст сновидения — затем реклама и возврат в MainScene
/// на следующий день. Полностью самодостаточен: строит свой оверлей/тексты в коде, не
/// зависит от систем MainScene (те уничтожаются при полной смене сцены).
///
/// Поток задаётся GameManager: он инкрементит день, пишет sleepFromDay в сейв, затемняет
/// экран и грузит эту сцену. Здесь проигрываем сон и грузим MainScene обратно — она
/// возобновляет игру с сохранённого дня.
///
/// Сцена: Sleepy scene
/// Зависимости: YG2 (saves, реклама), TMPro
/// SDK: YG2 (InterstitialAdv)
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;

public class SleepSceneController : MonoBehaviour
{
    [Header("Звук ночи (шёпот призраков) — назначается билдером сцены)")]
    [SerializeField] private AudioClip _nightClip;
    [Range(0f, 1f)] [SerializeField] private float _nightVolume = 0.7f;

    [Header("Шрифт для текста сна (Nunito SDF) — назначается билдером")]
    [SerializeField] private TMP_FontAsset _font;

    [Header("Куда возвращаемся после сна")]
    [SerializeField] private string _mainSceneName = "MainScene";

    // Реплики сна (RU/EN) — те же, что показывались раньше оверлеем в MainScene.
    private static readonly string[][] DreamLines =
    {
        new[] { "Это всего лишь сон... всего лишь сон.", "It's only a dream... only a dream." },
        new[] { "Кай зовёт меня сквозь туман: «Найди меня...»", "Kai calls through the fog: \"Find me...\"" },
        new[] { "Три круга горят в темноте. Они смотрят.", "Three circles burn in the dark. They are watching." },
        new[] { "На столе — письмо, пахнущее пеплом. «Я ещё жив.»", "A letter on the table, smelling of ash. \"I am still alive.\"" },
        new[] { "Стены дышат. Я просыпаюсь — но не уверена, что проснулась.", "The walls breathe. I wake — but I'm not sure I'm awake." },
    };

    // Рантайм-UI
    private Image _black;
    private Image _white;
    private TextMeshProUGUI _title;
    private TextMeshProUGUI _line;
    private AudioSource _nightSource;

    private void Start()
    {
        BuildOverlayUI();
        StartCoroutine(RunDream());
    }

    // ─── Построение оверлея (канвас + затемнение + вспышка + два текста) ──────────

    private void BuildOverlayUI()
    {
        var canvasGo = new GameObject("SleepCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500; // поверх всего
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _black = FullscreenImage(canvasGo.transform, "Black", new Color(0f, 0f, 0f, 1f)); // старт — чёрный
        _white = FullscreenImage(canvasGo.transform, "White", new Color(1f, 1f, 1f, 0f));

        _title = MakeText(canvasGo.transform, "Title", 96, new Vector2(0f, 120f));
        _title.fontStyle = FontStyles.Bold;
        _title.color = new Color(1f, 1f, 1f, 0f);

        _line = MakeText(canvasGo.transform, "DreamLine", 44, new Vector2(0f, -220f));
        _line.color = new Color(0.9f, 0.92f, 1f, 0f);

        // Ночной звук — отдельный зацикленный источник на этом объекте.
        _nightSource = gameObject.AddComponent<AudioSource>();
        _nightSource.loop = true;
        _nightSource.playOnAwake = false;
    }

    private Image FullscreenImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private TextMeshProUGUI MakeText(Transform parent, string name, float size, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1600, 300);
        rt.anchoredPosition = pos;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        t.enableWordWrapping = true;
        return t;
    }

    // ─── Сам сон ─────────────────────────────────────────────────────────────────

    private IEnumerator RunDream()
    {
        int day = (YG2.saves != null ? YG2.saves.sleepFromDay : 0);

        // 1) Ночной звук — вместе с началом сна (глохнет до рекламы).
        if (_nightClip != null)
        {
            _nightSource.clip = _nightClip;
            _nightSource.volume = _nightVolume;
            _nightSource.Play();
        }

        // 2) Приоткрываем сцену со спящим ГГ, но держим её сумрачной (сон).
        yield return Fade(_black, 1f, 0.55f, 1.0f);

        // 3) Титр «СОН».
        yield return Fade(_title, 0f, 1f, 0.6f);
        yield return new WaitForSeconds(1.4f);
        yield return Fade(_title, 1f, 0f, 0.6f);

        // 4) Эффект сновидения (вспышки/дрожь) + текст.
        string text = (Loc.IsRu ? DreamLines[day % DreamLines.Length][0]
                                 : DreamLines[day % DreamLines.Length][1]);
        _line.text = text;
        StartCoroutine(Fade(_line, 0f, 1f, 0.8f));
        yield return DreamEffect(day);

        // 5) Держим текст, даём прочитать.
        yield return new WaitForSeconds(2.2f);
        yield return Fade(_line, 1f, 0f, 0.7f);

        // 6) Просыпаемся: уводим в чёрный, глушим ночной звук ДО рекламы.
        yield return Fade(_black, 0.55f, 1f, 0.8f);
        if (_nightSource != null && _nightSource.isPlaying) _nightSource.Stop();

        // 7) Реклама на переходе (если не куплено отключение и модуль установлен).
        yield return ShowInterstitial();

        // 8) Возврат в основную сцену — она продолжит игру со следующего дня.
        UnityEngine.SceneManagement.SceneManager.LoadScene(_mainSceneName);
    }

    // Простые эффекты сна: пара вспышек белым + лёгкая дрожь камеры (если есть).
    private IEnumerator DreamEffect(int day)
    {
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 camBase = cam != null ? cam.localPosition : Vector3.zero;

        int kind = day % 3;
        if (kind == 0)
        {
            // Вспышки темноты/света.
            for (int i = 0; i < 3; i++)
            {
                yield return Fade(_white, 0f, 0.35f, 0.15f);
                yield return Fade(_white, 0.35f, 0f, 0.25f);
                yield return new WaitForSeconds(0.15f);
            }
        }
        else if (kind == 1 && cam != null)
        {
            // Дрожь камеры.
            float t = 0f;
            while (t < 1.4f)
            {
                t += Time.deltaTime;
                cam.localPosition = camBase + (Vector3)(Random.insideUnitCircle * 0.15f);
                yield return null;
            }
            cam.localPosition = camBase;
        }
        else
        {
            // Медленная тёмная пелена (наплыв чёрного и обратно).
            yield return Fade(_black, 0.55f, 0.8f, 0.6f);
            yield return new WaitForSeconds(0.4f);
            yield return Fade(_black, 0.8f, 0.55f, 0.8f);
        }
    }

    private IEnumerator ShowInterstitial()
    {
        if (YG2.saves != null && YG2.saves.adsDisabled) yield break;
#if InterstitialAdv_yg
        Analytics.Send(Analytics.AdInterstitial); // метрика: показ межстраничной в сцене сна
        YG2.InterstitialAdvShow();
        yield return new WaitUntil(() => !YG2.nowInterAdv);
#else
        yield return null;
#endif
    }

    // ─── Утилита плавного изменения альфы Graphic ────────────────────────────────
    private IEnumerator Fade(Graphic g, float from, float to, float dur)
    {
        if (g == null) yield break;
        float t = 0f;
        Color c = g.color;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, dur);
            c.a = Mathf.Lerp(from, to, Mathf.Clamp01(t));
            g.color = c;
            yield return null;
        }
        c.a = to; g.color = c;
    }
}
