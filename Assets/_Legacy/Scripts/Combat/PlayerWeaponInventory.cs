using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum WeaponType
{
    HandBlade,
    Hammer,
}

[Serializable]
public struct WeaponStats
{
    public string displayName;
    public float damage;
    public float cooldown;
    public float attackDuration;
    public Vector2 hitboxOffset;
    public Vector2 hitboxSize;
    public Color hitboxColor;
    public bool canCharge;

    public static WeaponStats HandBlade => new()
    {
        displayName = "손날",
        damage = 1f,
        cooldown = 0.16f,
        attackDuration = 0.09f,
        hitboxOffset = new Vector2(0.55f, 0.05f),
        hitboxSize = new Vector2(0.95f, 0.65f),
        hitboxColor = new Color(0.95f, 0.98f, 1f, 0.55f),
        canCharge = true,
    };

    public static WeaponStats Hammer => new()
    {
        displayName = "망치",
        damage = 2f,
        cooldown = 0.46f,
        attackDuration = 0.22f,
        hitboxOffset = new Vector2(0.55f * 3f, 0.05f),
        hitboxSize = new Vector2(0.95f * 3f, 0.65f),
        hitboxColor = new Color(0.85f, 0.55f, 0.25f, 0.65f),
        canCharge = false,
    };
}

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerComboController))]
public class PlayerWeaponInventory : MonoBehaviour
{
    [Serializable]
    public struct WeaponState
    {
        public WeaponType currentWeapon;
        public List<WeaponType> ownedWeapons;
    }

    [SerializeField] WeaponType currentWeapon = WeaponType.HandBlade;

    readonly List<WeaponType> ownedWeapons = new() { WeaponType.HandBlade };
    PlayerInput playerInput;
    PlayerComboController comboController;
    InputAction handBladeAction;
    InputAction hammerAction;

    public WeaponType CurrentWeapon => currentWeapon;
    public bool HasWeaponSlot => ownedWeapons.Count > 1;
    public IReadOnlyList<WeaponType> OwnedWeapons => ownedWeapons;

    public event Action<WeaponType> WeaponChanged;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        comboController = GetComponent<PlayerComboController>();
        handBladeAction = playerInput.actions["Previous"];
        hammerAction = playerInput.actions["Next"];
    }

    void Update()
    {
        if (PlayerMenuUI.IsMenuOpen || TradeUI.IsTradeOpen)
            return;

        if (handBladeAction != null && handBladeAction.WasPressedThisFrame())
            HandleWeaponKey(WeaponType.HandBlade);

        if (hammerAction != null && hammerAction.WasPressedThisFrame())
            HandleWeaponKey(WeaponType.Hammer);
    }

    void HandleWeaponKey(WeaponType weapon)
    {
        if (comboController.IsSelecting)
        {
            comboController.TryAddComboInput(weapon);
            return;
        }

        if (comboController.IsComboActive)
            return;

        if (Owns(weapon))
            Equip(weapon);
    }

    public WeaponStats GetCurrentStats()
    {
        return currentWeapon == WeaponType.Hammer ? WeaponStats.Hammer : WeaponStats.HandBlade;
    }

    public bool Owns(WeaponType weapon)
    {
        return ownedWeapons.Contains(weapon);
    }

    public void UnlockWeapon(WeaponType weapon)
    {
        if (ownedWeapons.Contains(weapon))
            return;

        ownedWeapons.Add(weapon);
        Equip(weapon);
    }

    public void Equip(WeaponType weapon)
    {
        if (!ownedWeapons.Contains(weapon))
            return;

        if (currentWeapon == weapon)
            return;

        currentWeapon = weapon;
        WeaponChanged?.Invoke(currentWeapon);
    }

    void OnGUI()
    {
        const float slotSize = 52f;
        const float padding = 12f;
        float x = padding;
        float y = Screen.height - slotSize - padding;

        DrawSlot(x, y, slotSize, WeaponType.HandBlade, "1", true);
        if (HasWeaponSlot)
            DrawSlot(x + slotSize + 8f, y, slotSize, WeaponType.Hammer, "2", Owns(WeaponType.Hammer));

        if (comboController != null && comboController.IsSelecting)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                normal = { textColor = new Color(0.55f, 0.95f, 1f) },
            };
            GUI.Label(new Rect(Screen.width * 0.5f - 120f, 48f, 240f, 22f), "콤보: 무기 2개 순서 입력 (1, 2)", style);
        }
    }

    void DrawSlot(float x, float y, float size, WeaponType weapon, string hotkey, bool visible)
    {
        if (!visible)
            return;

        bool owned = Owns(weapon);
        bool selected = currentWeapon == weapon;
        WeaponStats stats = weapon == WeaponType.Hammer ? WeaponStats.Hammer : WeaponStats.HandBlade;

        var bg = new Color(0.08f, 0.1f, 0.12f, owned ? 0.85f : 0.35f);
        if (selected)
            bg = new Color(0.15f, 0.22f, 0.28f, 0.95f);

        DrawRect(x, y, size, size, bg);

        if (selected)
            DrawRect(x, y, size, size, new Color(0.45f, 0.85f, 1f, 0.9f), false);

        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11,
            normal = { textColor = owned ? Color.white : new Color(1f, 1f, 1f, 0.35f) },
        };

        GUI.Label(new Rect(x, y + 8f, size, 18f), stats.displayName, labelStyle);
        GUI.Label(new Rect(x + size - 18f, y + 2f, 16f, 16f), hotkey, labelStyle);

        if (!owned && weapon != WeaponType.HandBlade)
        {
            GUI.Label(new Rect(x, y + 24f, size, 18f), "?", labelStyle);
        }
    }

    static void DrawRect(float x, float y, float w, float h, Color color, bool filled = true)
    {
        var prev = GUI.color;
        GUI.color = color;
        if (filled)
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        else
        {
            GUI.DrawTexture(new Rect(x, y, w, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y + h - 2f, w, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x, y, 2f, h), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + w - 2f, y, 2f, h), Texture2D.whiteTexture);
        }
        GUI.color = prev;
    }

    public WeaponState ExportState()
    {
        return new WeaponState
        {
            currentWeapon = currentWeapon,
            ownedWeapons = new List<WeaponType>(ownedWeapons),
        };
    }

    public void ImportState(WeaponState state)
    {
        ownedWeapons.Clear();
        ownedWeapons.Add(WeaponType.HandBlade);

        if (state.ownedWeapons != null)
        {
            for (int i = 0; i < state.ownedWeapons.Count; i++)
            {
                WeaponType weapon = state.ownedWeapons[i];
                if (!ownedWeapons.Contains(weapon))
                    ownedWeapons.Add(weapon);
            }
        }

        if (!ownedWeapons.Contains(state.currentWeapon))
            state.currentWeapon = WeaponType.HandBlade;

        currentWeapon = state.currentWeapon;
        WeaponChanged?.Invoke(currentWeapon);
    }
}
