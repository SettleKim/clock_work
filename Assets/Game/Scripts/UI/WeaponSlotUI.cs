using UnityEngine;
using UnityEngine.UI;

namespace ClockWork.Game
{
    public class WeaponSlotUI : MonoBehaviour
    {
        [SerializeField] PlayerWeaponController weaponController;
        [SerializeField] Vector2 screenPadding = new(24f, 24f);
        [SerializeField] Vector2 slotSize = new(128f, 36f);
        [SerializeField] Color backgroundColor = new(0.12f, 0.12f, 0.14f, 0.82f);
        [SerializeField] Color textColor = new(0.95f, 0.93f, 0.88f, 1f);

        Text weaponLabel;
        static Sprite whiteSprite;
        static Font builtinFont;

        void Awake()
        {
            if (weaponController == null)
                weaponController = FindFirstObjectByType<PlayerWeaponController>();

            BuildUI();

            if (weaponController != null)
            {
                weaponController.WeaponChanged += OnWeaponChanged;
                OnWeaponChanged(weaponController.CurrentWeapon);
            }
        }

        void OnDestroy()
        {
            if (weaponController != null)
                weaponController.WeaponChanged -= OnWeaponChanged;
        }

        void Start()
        {
            if (weaponController != null)
                OnWeaponChanged(weaponController.CurrentWeapon);
        }

        void BuildUI()
        {
            var canvasObject = new GameObject("WeaponSlotCanvas");
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("WeaponSlotPanel", typeof(RectTransform));
            panel.transform.SetParent(canvasObject.transform, false);

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var slot = new GameObject("WeaponSlot", typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(panel.transform, false);

            var slotRect = slot.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0f, 0f);
            slotRect.anchorMax = new Vector2(0f, 0f);
            slotRect.pivot = new Vector2(0f, 0f);
            slotRect.anchoredPosition = new Vector2(screenPadding.x, screenPadding.y);
            slotRect.sizeDelta = slotSize;

            var slotImage = slot.GetComponent<Image>();
            slotImage.sprite = GetWhiteSprite();
            slotImage.type = Image.Type.Sliced;
            slotImage.color = backgroundColor;

            var labelObject = new GameObject("WeaponLabel", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(slot.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 4f);
            labelRect.offsetMax = new Vector2(-10f, -4f);

            weaponLabel = labelObject.GetComponent<Text>();
            weaponLabel.font = GetBuiltinFont();
            weaponLabel.fontSize = 16;
            weaponLabel.alignment = TextAnchor.MiddleLeft;
            weaponLabel.color = textColor;
            weaponLabel.text = "무기: -";
        }

        void OnWeaponChanged(WeaponDefinition weapon)
        {
            if (weaponLabel == null)
                return;

            string name = weapon != null && !string.IsNullOrEmpty(weapon.DisplayName)
                ? weapon.DisplayName
                : "-";
            weaponLabel.text = $"무기: {name}";
        }

        static Sprite GetWhiteSprite()
        {
            if (whiteSprite == null)
                whiteSprite = CombatSpriteUtil.CreateRectSprite(4, 4, Color.white);
            return whiteSprite;
        }

        static Font GetBuiltinFont()
        {
            if (builtinFont == null)
                builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return builtinFont;
        }
    }
}
