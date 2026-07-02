using UnityEngine;
using UnityEngine.UI;

namespace ClockWork.Game
{
    public class CombatTapWindowUI : MonoBehaviour
    {
        [SerializeField] PlayerCombatMode combatMode;
        [SerializeField] Transform followTarget;
        [SerializeField] Camera worldCamera;
        [SerializeField] Vector2 screenOffset = new(48f, 0f);
        [SerializeField] Vector2 barSize = new(8f, 80f);
        [SerializeField] Color fillColor = new(0.45f, 0.85f, 1f, 0.92f);
        [SerializeField] Color backgroundColor = new(0.12f, 0.14f, 0.18f, 0.75f);

        RectTransform barRoot;
        RectTransform fillRect;
        Canvas canvas;
        static Sprite whiteSprite;

        void Awake()
        {
            if (combatMode == null)
                combatMode = FindFirstObjectByType<PlayerCombatMode>();

            if (followTarget == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    followTarget = player.transform;
            }

            if (worldCamera == null)
                worldCamera = Camera.main;

            BuildUI();
            SetVisible(false);
        }

        void BuildUI()
        {
            var canvasObject = new GameObject("CombatTapWindowCanvas");
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;

            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("TapWindowPanel", typeof(RectTransform));
            panel.transform.SetParent(canvasObject.transform, false);

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            barRoot = new GameObject("TapWindowBar", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            barRoot.SetParent(panel.transform, false);
            barRoot.sizeDelta = barSize;

            var bgImage = barRoot.GetComponent<Image>();
            bgImage.sprite = GetWhiteSprite();
            bgImage.color = backgroundColor;

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(barRoot, false);

            fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.pivot = new Vector2(0.5f, 0f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var fillImage = fillObject.GetComponent<Image>();
            fillImage.sprite = GetWhiteSprite();
            fillImage.color = fillColor;
        }

        void Update()
        {
            if (combatMode == null || barRoot == null)
                return;

            bool visible = combatMode.IsTapWindow;
            SetVisible(visible);
            if (!visible)
                return;

            UpdatePosition();
            UpdateFill();
        }

        void UpdatePosition()
        {
            if (followTarget == null || worldCamera == null)
                return;

            Vector3 worldPos = followTarget.position + Vector3.right * 0.8f;
            Vector3 screenPos = worldCamera.WorldToScreenPoint(worldPos);
            barRoot.position = screenPos + (Vector3)screenOffset;
        }

        void UpdateFill()
        {
            float normalized = combatMode.TapWindowNormalized;
            fillRect.anchorMax = new Vector2(1f, normalized);
            fillRect.offsetMax = Vector2.zero;
        }

        void SetVisible(bool visible)
        {
            if (barRoot != null)
                barRoot.gameObject.SetActive(visible);
        }

        static Sprite GetWhiteSprite()
        {
            if (whiteSprite == null)
                whiteSprite = CombatSpriteUtil.CreateRectSprite(4, 4, Color.white);
            return whiteSprite;
        }
    }
}
