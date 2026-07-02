using UnityEngine;
using UnityEngine.UI;

namespace ClockWork.Game
{
    public class EnergyGaugeUI : MonoBehaviour
    {
        const int SegmentsPerGroup = 5;

        [SerializeField] PlayerEnergyGauge gauge;
        [SerializeField] Vector2 screenPadding = new(24f, 24f);
        [SerializeField] float segmentWidth = 10f;
        [SerializeField] float segmentHeight = 18f;
        [SerializeField] float segmentSpacing = 2f;
        [SerializeField] float groupDividerWidth = 4f;
        [SerializeField] Color emptyColor = new(0.2f, 0.2f, 0.22f, 0.65f);
        [SerializeField] Color filledColor = new(1f, 0.78f, 0.2f, 0.95f);
        [SerializeField] Color dividerColor = new(0.08f, 0.08f, 0.1f, 0.9f);

        Image[] segmentImages;
        static Sprite whiteSprite;

        void Awake()
        {
            if (gauge == null)
                gauge = FindFirstObjectByType<PlayerEnergyGauge>();

            BuildUI();

            if (gauge != null)
            {
                gauge.OnEnergyChanged += OnEnergyChanged;
                OnEnergyChanged(gauge.CurrentEnergy, gauge.MaxEnergy);
            }
        }

        void OnDestroy()
        {
            if (gauge != null)
                gauge.OnEnergyChanged -= OnEnergyChanged;
        }

        void BuildUI()
        {
            int segmentCount = gauge != null ? gauge.SegmentCount : 20;
            segmentImages = new Image[segmentCount];

            var canvasObject = new GameObject("EnergyGaugeCanvas");
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            var panel = CreateContainer("EnergyGaugePanel", canvasObject.transform);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var bar = CreateContainer("EnergyBar", panel.transform);
            var barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = new Vector2(1f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(1f, 0f);
            barRect.anchoredPosition = new Vector2(-screenPadding.x, screenPadding.y);

            float x = 0f;
            for (int i = 0; i < segmentCount; i++)
            {
                var segmentObject = CreateRect($"Segment_{i + 1}", bar.transform);
                var segmentRect = segmentObject.GetComponent<RectTransform>();
                segmentRect.anchorMin = new Vector2(0f, 0.5f);
                segmentRect.anchorMax = new Vector2(0f, 0.5f);
                segmentRect.pivot = new Vector2(0f, 0.5f);
                segmentRect.sizeDelta = new Vector2(segmentWidth, segmentHeight);
                segmentRect.anchoredPosition = new Vector2(x, 0f);

                var image = segmentObject.GetComponent<Image>();
                image.sprite = GetWhiteSprite();
                image.type = Image.Type.Simple;
                image.color = emptyColor;
                segmentImages[i] = image;

                x += segmentWidth + segmentSpacing;

                if ((i + 1) % SegmentsPerGroup == 0 && i < segmentCount - 1)
                {
                    var dividerObject = CreateRect($"Divider_{(i + 1) / SegmentsPerGroup}", bar.transform);
                    var dividerRect = dividerObject.GetComponent<RectTransform>();
                    dividerRect.anchorMin = new Vector2(0f, 0.5f);
                    dividerRect.anchorMax = new Vector2(0f, 0.5f);
                    dividerRect.pivot = new Vector2(0f, 0.5f);
                    dividerRect.sizeDelta = new Vector2(groupDividerWidth, segmentHeight + 4f);
                    dividerRect.anchoredPosition = new Vector2(x, 0f);

                    var dividerImage = dividerObject.GetComponent<Image>();
                    dividerImage.sprite = GetWhiteSprite();
                    dividerImage.color = dividerColor;

                    x += groupDividerWidth + segmentSpacing;
                }
            }

            barRect.sizeDelta = new Vector2(x - segmentSpacing, segmentHeight);
        }

        static GameObject CreateContainer(string name, Transform parent)
        {
            var rectObject = new GameObject(name, typeof(RectTransform));
            rectObject.transform.SetParent(parent, false);
            return rectObject;
        }

        static GameObject CreateRect(string name, Transform parent)
        {
            var rectObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            rectObject.transform.SetParent(parent, false);
            return rectObject;
        }

        static Sprite GetWhiteSprite()
        {
            if (whiteSprite == null)
                whiteSprite = CombatSpriteUtil.CreateRectSprite(4, 4, Color.white);
            return whiteSprite;
        }

        void OnEnergyChanged(float current, float max)
        {
            if (segmentImages == null || segmentImages.Length == 0)
                return;

            int filled = gauge != null
                ? gauge.FilledSegmentCount
                : Mathf.Clamp(Mathf.FloorToInt(current / 5f), 0, segmentImages.Length);

            for (int i = 0; i < segmentImages.Length; i++)
            {
                if (segmentImages[i] == null)
                    continue;

                segmentImages[i].color = i < filled ? filledColor : emptyColor;
            }
        }
    }
}
