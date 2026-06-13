/// <summary>
/// Минигейм кофемашины: два вертикальных заполнения (Image: Filled, Vertical) с
/// бегающим уровнем. Сначала «бегает» температура — игрок кликает, уровень
/// фиксируется; затем так же объём. Зафиксированные значения (0..1) уходят в
/// CoffeeCraftingSystem и сравниваются с желанием клиента.
/// Сцена: MainScene (UI на Canvas)
/// Зависимости: GameInput, UnityEngine.UI, TMPro, Loc
/// SDK: Нет
/// </summary>
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MachineMinigame : MonoBehaviour
{
    [Header("Панель машины (включается на зоне машины)")]
    [SerializeField] private GameObject _panel;

    [Header("Заполнение температуры (Image: Filled, Vertical)")]
    [SerializeField] private Image _tempFill;
    [SerializeField] private TextMeshProUGUI _tempLabel;

    [Header("Заполнение объёма (Image: Filled, Vertical)")]
    [SerializeField] private Image _volumeFill;
    [SerializeField] private TextMeshProUGUI _volumeLabel;

    [Header("Скорость бегунка")]
    [SerializeField] private float _speed = 0.8f;

    private bool  _running;
    private int   _phase;   // 0 — температура, 1 — объём, 2 — конец
    private float _t;       // фаза пинг-понга
    private float _temp, _volume;
    private Action<float, float> _onDone;

    /// <summary>Запускает минигейм. onDone(temp, volume) — зафиксированные значения 0..1.</summary>
    public void BeginGame(Action<float, float> onDone)
    {
        _onDone  = onDone;
        _temp    = 0f;
        _volume  = 0f;
        _phase   = 0;
        _t       = 0f;
        _running = true;
        if (_panel != null) _panel.SetActive(true);
        UpdateLabels();
    }

    public void HidePanel()
    {
        _running = false;
        if (_panel != null) _panel.SetActive(false);
    }

    private void Update()
    {
        if (!_running) return;

        // Пинг-понг бегунка 0..1..0
        _t += Time.deltaTime * _speed;
        float level = Mathf.PingPong(_t, 1f);

        if (_phase == 0 && _tempFill   != null) _tempFill.fillAmount   = level;
        if (_phase == 1 && _volumeFill != null) _volumeFill.fillAmount = level;

        bool clicked = Input.GetMouseButtonDown(0) ||
                       (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
        if (!clicked) return;

        if (_phase == 0)
        {
            _temp = level;
            if (_tempFill != null) _tempFill.fillAmount = level;
            _phase = 1;
            _t = 0f;
            UpdateLabels();
        }
        else if (_phase == 1)
        {
            _volume = level;
            if (_volumeFill != null) _volumeFill.fillAmount = level;
            _phase = 2;
            _running = false;
            UpdateLabels();
            _onDone?.Invoke(_temp, _volume);
        }
    }

    private void UpdateLabels()
    {
        if (_tempLabel != null)
            _tempLabel.text = Loc.T("Температура", "Temperature") + (_phase == 0 ? " ◄" : "");
        if (_volumeLabel != null)
            _volumeLabel.text = Loc.T("Объём", "Volume") + (_phase == 1 ? " ◄" : "");
    }
}
