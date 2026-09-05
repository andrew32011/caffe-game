/// <summary>
/// Батч 16: очередь блокирующих окон — показываем модалки ПО ОДНОМУ, а не стеком. Каждое окно
/// задаётся либо корутиной показа (выполняется до конца), либо парой (открыть / признак «ещё
/// открыто»). Следующее окно стартует только после закрытия предыдущего. Убирает наложения
/// попапов на старте/в конце дня.
/// Сцена: MainScene (ленивый singleton-раннер). Зависимости: UnityEngine. SDK: нет.
/// </summary>
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UiQueue : MonoBehaviour
{
    private static UiQueue _inst;
    private static UiQueue Inst
    {
        get
        {
            if (_inst == null)
            {
                var go = new GameObject("UiQueue");
                _inst = go.AddComponent<UiQueue>();
            }
            return _inst;
        }
    }

    private readonly Queue<Func<IEnumerator>> _q = new Queue<Func<IEnumerator>>();
    private bool _running;

    /// <summary>Есть ли сейчас показ/ожидающие окна (чтобы не пихать баннеры поверх).</summary>
    public static bool IsBusy => _inst != null && (_inst._running || _inst._q.Count > 0);

    /// <summary>Поставить окно-корутину в очередь.</summary>
    public static void Enqueue(Func<IEnumerator> show)
    {
        if (show == null) return;
        Inst._q.Enqueue(show);
        Inst.Kick();
    }

    /// <summary>Открыть окно и ждать, пока isOpen() не станет false (окно закрылось).</summary>
    public static void Enqueue(Action open, Func<bool> isOpen)
    {
        Enqueue(() => WaitRoutine(open, isOpen));
    }

    private static IEnumerator WaitRoutine(Action open, Func<bool> isOpen)
    {
        open?.Invoke();
        yield return null; // дать окну открыться (Awake/SetActive)
        float safety = 0f;
        while (isOpen != null && isOpen() && safety < 180f)
        {
            safety += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void Kick() { if (!_running) StartCoroutine(Run()); }

    private IEnumerator Run()
    {
        _running = true;
        while (_q.Count > 0)
        {
            var show = _q.Dequeue();
            IEnumerator co = null;
            try { co = show(); }
            catch (Exception e) { Debug.LogWarning("UiQueue: ошибка показа окна — " + e.Message); }
            if (co != null) yield return StartCoroutine(co);
            yield return null; // короткий зазор между окнами
        }
        _running = false;
    }

    private void OnDestroy() { if (_inst == this) _inst = null; }
}
