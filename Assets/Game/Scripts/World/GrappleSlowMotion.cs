using UnityEngine;

namespace ClockWork.Game
{
    public static class GrappleSlowMotion
    {
        static float savedTimeScale = 1f;
        static float savedFixedDeltaTime;
        static int activeCount;

        public static bool IsActive => activeCount > 0;

        public static void Enter(float timeScale = 0.16f)
        {
            if (activeCount == 0)
            {
                savedTimeScale = Time.timeScale;
                savedFixedDeltaTime = Time.fixedDeltaTime;
                Time.timeScale = timeScale;
                Time.fixedDeltaTime = savedFixedDeltaTime * timeScale;
            }

            activeCount++;
        }

        public static void Exit()
        {
            activeCount = Mathf.Max(0, activeCount - 1);
            if (activeCount != 0)
                return;

            Time.timeScale = savedTimeScale;
            Time.fixedDeltaTime = savedFixedDeltaTime;
        }
    }
}
