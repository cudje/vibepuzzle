/*
 * UnityMainThreadDispatcher (Lazy-init + Early execution order)
 * - 씬에 미리 넣지 않아도 Instance() 첫 호출 시 자동 생성
 * - [DefaultExecutionOrder(-10000)]로 매우 이르게 Awake 실행
 * - 스레드에서 메인스레드 작업을 안전하게 큐잉
 * - Action, IEnumerator, async(Task) 모두 지원
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance;
    private static bool _quitting = false;

    // ======= Public Static Helpers (권장 호출부) =======
    /// <summary>메인 스레드에서 action 실행</summary>
    public static void EnqueueOnMain(Action action)
    {
        Instance().Enqueue(action);
    }

    /// <summary>메인 스레드에서 IEnumerator 실행(코루틴)</summary>
    public static void EnqueueOnMain(IEnumerator routine)
    {
        Instance().Enqueue(routine);
    }

    /// <summary>메인 스레드에서 action 실행을 await 할 수 있게 해줌</summary>
    public static Task EnqueueOnMainAsync(Action action)
    {
        return Instance().EnqueueAsync(action);
    }

    // ======= Instance (Lazy Initialization) =======
    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance != null) return _instance;

        if (_quitting)
            throw new Exception("Application is quitting. UnityMainThreadDispatcher is no longer available.");

        // 씬에 없으면 자동 생성
        var go = new GameObject("UnityMainThreadDispatcher");
        _instance = go.AddComponent<UnityMainThreadDispatcher>();
        DontDestroyOnLoad(go);
        return _instance;
    }

    // ======= MonoBehaviour Lifecycle =======
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            // 중복 생성을 방지
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit()
    {
        _quitting = true;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Update()
    {
        // 메인 스레드에서 큐 비우기
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                try
                {
                    _executionQueue.Dequeue()?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }

    // ======= Queueing APIs =======
    /// <summary>IEnumerator(코루틴)를 메인 스레드에서 실행</summary>
    public void Enqueue(IEnumerator action)
    {
        if (action == null) return;
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(() => StartCoroutine(action));
        }
    }

    /// <summary>Action을 메인 스레드에서 실행</summary>
    public void Enqueue(Action action)
    {
        if (action == null) return;
        Enqueue(ActionWrapper(action));
    }

    /// <summary>Action을 메인 스레드에서 실행하고 완료를 await 가능</summary>
    public Task EnqueueAsync(Action action)
    {
        var tcs = new TaskCompletionSource<bool>();

        void Wrapped()
        {
            try
            {
                action?.Invoke();
                tcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        Enqueue(ActionWrapper(Wrapped));
        return tcs.Task;
    }

    private IEnumerator ActionWrapper(Action a)
    {
        a?.Invoke();
        yield return null;
    }
}
