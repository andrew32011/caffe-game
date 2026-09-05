/// <summary>
/// Батч 15: зримый слой обустройства. Мебель/декор ПРЕДРАЗМЕЩЕНЫ в сцене билдером (внутри
/// объекта зала, в объекте RenoStages) — по одному объекту на стадию. Визуализатор лишь
/// ВКЛЮЧАЕТ/ВЫКЛЮЧАЕТ их по прогрессу: показаны стадии 0..Stage-1, при завершении — «поп».
/// Так дизайнер видит и правит мебель прямо в редакторе (объекты активны в сцене), а в игре
/// они появляются постепенно. Всё null-safe.
/// Сцена: MainScene (билдер). Зависимости: RenovationManager. SDK: нет.
/// </summary>
using System.Collections;
using UnityEngine;

public class RenovationVisualizer : MonoBehaviour
{
    public static RenovationVisualizer Instance { get; private set; }

    [Tooltip("Предразмещённые объекты мебели по стадиям (индекс = стадия). Ставит билдер.")]
    [SerializeField] private GameObject[] _stageObjects;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void Start()
    {
        int done = RenovationManager.Stage;
        for (int i = 0; i < Count; i++)
            if (_stageObjects[i] != null) _stageObjects[i].SetActive(i < done);
    }

    private int Count => _stageObjects != null ? _stageObjects.Length : 0;
    public int StageCount => Count;

    // ─── Батч 16: камера-магазин обустройства ───────────────────────────────────
    private Vector3 _camSaved; private Quaternion _camSavedRot; private bool _camSavedValid;
    private Coroutine _camMoveCo, _blinkCo; private int _blinkIndex = -1;

    /// <summary>Запоминает текущее положение игровой камеры перед облётом магазина.</summary>
    public void EnterShopCamera()
    {
        var cam = Camera.main; if (cam == null) return;
        if (!_camSavedValid) { _camSaved = cam.transform.position; _camSavedRot = cam.transform.rotation; _camSavedValid = true; }
    }

    /// <summary>Плавно наводит камеру на точку стадии i (не покидая её).</summary>
    public void FrameStage(int i)
    {
        var cam = Camera.main;
        if (cam == null || i < 0 || i >= Count || _stageObjects[i] == null) return;
        if (!_camSavedValid) EnterShopCamera();
        Vector3 look = _stageObjects[i].transform.position;
        Vector3 dir = _camSaved - look; dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = -cam.transform.forward;
        dir.Normalize();
        Vector3 dest = look + dir * 3.0f + Vector3.up * 1.2f;
        Quaternion rot = Quaternion.LookRotation((look - dest).normalized, Vector3.up);
        if (_camMoveCo != null) StopCoroutine(_camMoveCo);
        _camMoveCo = StartCoroutine(LerpCam(cam.transform, cam.transform.position, cam.transform.rotation, dest, rot, 0.5f));
    }

    /// <summary>Возвращает камеру на исходное игровое место.</summary>
    public void ExitShopCamera()
    {
        StopPreview();
        var cam = Camera.main;
        if (cam != null && _camSavedValid)
        {
            if (_camMoveCo != null) StopCoroutine(_camMoveCo);
            _camMoveCo = StartCoroutine(LerpCam(cam.transform, cam.transform.position, cam.transform.rotation, _camSaved, _camSavedRot, 0.5f));
        }
        _camSavedValid = false;
    }

    /// <summary>Мигающий «призрак» будущего предмета стадии i (превью покупки).</summary>
    public void PreviewStage(int i)
    {
        StopPreview();
        if (i < 0 || i >= Count || _stageObjects[i] == null) return;
        _blinkIndex = i;
        _blinkCo = StartCoroutine(Blink(_stageObjects[i]));
    }

    /// <summary>Останавливает мигание и возвращает объект в состояние по прогрессу.</summary>
    public void StopPreview()
    {
        if (_blinkCo != null) StopCoroutine(_blinkCo);
        _blinkCo = null;
        if (_blinkIndex >= 0 && _blinkIndex < Count && _stageObjects[_blinkIndex] != null)
            _stageObjects[_blinkIndex].SetActive(_blinkIndex < RenovationManager.Stage);
        _blinkIndex = -1;
    }

    private IEnumerator Blink(GameObject go)
    {
        while (true)
        {
            go.SetActive(true);  yield return new WaitForSecondsRealtime(0.55f);
            go.SetActive(false); yield return new WaitForSecondsRealtime(0.35f);
        }
    }

    /// <summary>Показать только что завершённую стадию (с «поп»-анимацией).</summary>
    public void ShowStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= Count) return;
        var go = _stageObjects[stageIndex];
        if (go == null) return;
        go.SetActive(true);
        StartCoroutine(PopIn(go.transform));
    }

    private IEnumerator PopIn(Transform tr)
    {
        Vector3 target = tr.localScale;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            tr.localScale = target * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            yield return null;
        }
        tr.localScale = target;
    }

    /// <summary>ОПЦИОНАЛЬНО: плавно показать камерой зону обустройства и вернуть обратно.
    /// Вызывать вне активной подачи (иначе конфликт со Stages/движением камеры).</summary>
    public IEnumerator ShowcaseStage(int stageIndex)
    {
        var cam = Camera.main;
        if (stageIndex < 0 || stageIndex >= Count || _stageObjects[stageIndex] == null || cam == null) yield break;
        Vector3 look = _stageObjects[stageIndex].transform.position;
        Vector3 startPos = cam.transform.position; Quaternion startRot = cam.transform.rotation;
        Vector3 dest = look - cam.transform.forward * 3.5f + Vector3.up * 1.2f;
        yield return LerpCam(cam.transform, startPos, startRot, dest, Quaternion.LookRotation(look - dest), 0.6f);
        yield return new WaitForSeconds(1.1f);
        yield return LerpCam(cam.transform, cam.transform.position, cam.transform.rotation, startPos, startRot, 0.6f);
    }

    private IEnumerator LerpCam(Transform cam, Vector3 p0, Quaternion r0, Vector3 p1, Quaternion r1, float dur)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, dur);
            float e = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
            cam.position = Vector3.Lerp(p0, p1, e);
            cam.rotation = Quaternion.Slerp(r0, r1, e);
            yield return null;
        }
    }
}
