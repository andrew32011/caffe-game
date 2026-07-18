/// <summary>
/// Кнопка «Убрать рекламу» (пункт 5) — постоянная покупка через YG2 Payments (Yandex).
/// Строит свою маленькую пульсирующую кнопку в углу HUD, чтобы ненавязчиво, но заметно
/// предлагать отключить рекламу. После покупки (или если уже куплено) — прячется, а флаг
/// adsDisabled в сейве отключает межстраничную рекламу везде (GameManager, сцена сна).
///
/// Товар должен быть заведён в консоли Яндекс Игр с id = ProductId ("no_ads").
/// Сцена: MainScene
/// Зависимости: YG2 (Payments), TMPro
/// SDK: YG2 (Payments)
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;

public class AdDisableButton : MonoBehaviour
{
    // Id товара в консоли Яндекс Игр (некупляемая покупка «отключить рекламу»).
    public const string ProductId = "no_ads";

    [SerializeField] private TMP_FontAsset _font;

    private Button _button;
    private Coroutine _pulseCo;

    private void Start()
    {
        // Уже куплено — кнопка не нужна.
        if (YG2.saves != null && YG2.saves.adsDisabled) return;
        BuildButton();
        StartPulse();
    }

    private void OnEnable()
    {
        YG2.onPurchaseSuccess += OnPurchase;
    }

    private void OnDisable()
    {
        YG2.onPurchaseSuccess -= OnPurchase;
    }

    private void BuildButton()
    {
        var canvasGo = new GameObject("AdDisableCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var btnGo = new GameObject("BtnDisableAds", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)btnGo.transform;
        rt.anchorMin = new Vector2(0.02f, 0.02f);   // левый нижний угол — обычно свободен
        rt.anchorMax = new Vector2(0.22f, 0.09f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img = btnGo.GetComponent<Image>();
        img.color = new Color(0.15f, 0.12f, 0.22f, 0.92f);

        var label = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(btnGo.transform, false);
        var lrt = (RectTransform)label.transform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
        var t = label.GetComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text = Loc.T("Убрать рекламу", "Remove ads");
        t.fontSize = 24;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(1f, 0.95f, 0.7f);
        t.raycastTarget = false;
        t.enableAutoSizing = true;
        t.fontSizeMin = 14; t.fontSizeMax = 26;

        _button = btnGo.GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        AudioController.Instance?.PlayClick();
#if Payments_yg
        YG2.BuyPayments(ProductId);
#else
        // Модуль оплат не установлен — просто предупреждаем в логе (в релизе модуль включён).
        Debug.LogWarning("AdDisableButton: модуль YG2 Payments не установлен (define Payments_yg).");
#endif
    }

    private void OnPurchase(string id)
    {
        if (id != ProductId) return;
        if (YG2.saves != null)
        {
            YG2.saves.adsDisabled = true;
            GameManager.Instance?.SaveGame();
        }
        AudioController.Instance?.PlayBonus();
        StopPulse();
        if (_button != null) _button.transform.parent.gameObject.SetActive(false); // прячем канвас
    }

    private void StartPulse()
    {
        if (_button == null || _pulseCo != null) return;
        _pulseCo = StartCoroutine(PulseRoutine());
    }

    private void StopPulse()
    {
        if (_pulseCo != null) { StopCoroutine(_pulseCo); _pulseCo = null; }
        if (_button != null) _button.transform.localScale = Vector3.one;
    }

    private IEnumerator PulseRoutine()
    {
        Transform tr = _button.transform;
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime * 2.5f;
            float s = 1f + 0.05f * Mathf.Sin(t);
            tr.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }
}
