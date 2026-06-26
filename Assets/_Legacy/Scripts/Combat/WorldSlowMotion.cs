using System.Collections;
using UnityEngine;

public static class WorldSlowMotion
{
    const float DefaultSlowScale = 0.15f;

    static float endUnscaledTime;
    static float currentSlowScale = DefaultSlowScale;
    static bool isActive;

    public static bool IsActive => isActive;
    public static float SlowScale => isActive ? currentSlowScale : 1f;

    public static float WorldDeltaTime => Time.deltaTime * SlowScale;
    public static float WorldFixedDeltaTime => Time.fixedDeltaTime * SlowScale;

    public static void Enter(float durationSeconds, float slowScale = DefaultSlowScale)
    {
        currentSlowScale = Mathf.Clamp(slowScale, 0.05f, 1f);
        endUnscaledTime = Time.unscaledTime + durationSeconds;
        isActive = true;
    }

    public static void Tick()
    {
        if (!isActive)
            return;

        if (Time.unscaledTime >= endUnscaledTime)
            Exit();
    }

    public static void Exit()
    {
        isActive = false;
    }

    public static IEnumerator WaitWorldSeconds(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += WorldDeltaTime;
            yield return null;
        }
    }
}

public class WorldSlowMotionRunner : MonoBehaviour
{
    void Awake()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        WorldSlowMotion.Exit();
    }

    void Update()
    {
        WorldSlowMotion.Tick();
    }

    void OnDestroy()
    {
        WorldSlowMotion.Exit();
    }
}
