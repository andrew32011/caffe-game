/// <summary>
/// Батч 16: единый виджет валюты на готовом префабе Mini UI (Coin Count / Gem Count) — рамка +
/// иконка + число + кнопка «＋». Один класс на обе валюты, поэтому монеты и кристаллы выглядят
/// одинаково. Число берётся из GameManager; «＋» открывает соответствующий магазин (кристаллы →
/// GemShopUI; монеты → магазин-обустройство, главный сток). Строка кристаллов гаснет, пока
/// кристаллы не разблокированы (ProgressionManager), но компонент остаётся живым.
/// Сцена: MainScene. Зависимости: GameManager, ProgressionManager, GemShopUI, RenovationShopUI.
/// SDK: нет.
/// </summary>
using UnityEngine;
using UnityEngine.UI;

public class CurrencyWidget : MonoBehaviour
{
    public enum Kind { Coins, Gems }

    public Kind kind = Kind.Coins;   // задаёт билдер (public → надёжно сериализуется)

    private Text _num;                 // число (legacy Text префаба Mini UI)
    private Button _add;               // кнопка «＋»
    private CanvasGroup _cg;           // для мягкого гашения строки кристаллов
    private int _shown = int.MinValue;
    private bool _wired;

    /// <summary>Задаётся билдером сразу после AddComponent.</summary>
    public void SetKind(Kind k) { kind = k; _shown = int.MinValue; }

    private void Awake() { Wire(); }

    private void Wire()
    {
        if (_wired) return;
        _num = GetComponentInChildren<Text>(true);
        _add = GetComponentInChildren<Button>(true);
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        if (_add != null) { _add.onClick.RemoveAllListeners(); _add.onClick.AddListener(OnAdd); }
        _wired = true;
    }

    private void OnAdd()
    {
        AudioController.Instance?.PlayClick();
        if (kind == Kind.Gems) GemShopUI.Ensure().Open();
        else RenovationShopUI.Ensure().Open();  // монеты → обустройство (главный сток)
    }

    private void Update()
    {
        Wire();
        var gm = GameManager.Instance;
        if (gm == null || _num == null) return;

        if (kind == Kind.Gems)
        {
            bool on = ProgressionManager.IsUnlocked(ProgressionManager.Feature.Gems);
            float a = on ? 1f : 0f;
            if (_cg != null) { _cg.alpha = a; _cg.interactable = on; _cg.blocksRaycasts = on; }
            if (!on) return;
        }

        int v = kind == Kind.Gems ? gm.Gems : gm.TotalCoins;
        if (v == _shown) return;
        _shown = v;
        _num.text = v.ToString("N0");
    }
}
