using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerItemInventory))]
[RequireComponent(typeof(PlayerWeaponInventory))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(PlayerRespawn))]
public class PlayerSaveController : MonoBehaviour
{
    [Serializable]
    class SaveData
    {
        public UndergroundMapBootstrap.MapLayout mapLayout;
        public Vector2 playerPosition;
        public Vector2 respawnPosition;
        public float playerHealth;
        public PlayerWeaponInventory.WeaponState weaponState;
        public List<PlayerItemInventory.ItemEntry> itemEntries = new();
    }

    [SerializeField] Key saveKey = Key.F5;
    [SerializeField] Key loadKey = Key.F9;
    [SerializeField] bool autoLoadOnStart = true;

    PlayerInput playerInput;
    PlayerItemInventory itemInventory;
    PlayerWeaponInventory weaponInventory;
    Health health;
    PlayerRespawn respawn;
    UndergroundMapBootstrap bootstrap;
    string savePath;

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        itemInventory = GetComponent<PlayerItemInventory>();
        weaponInventory = GetComponent<PlayerWeaponInventory>();
        health = GetComponent<Health>();
        respawn = GetComponent<PlayerRespawn>();
        bootstrap = FindFirstObjectByType<UndergroundMapBootstrap>();
        savePath = Path.Combine(Application.persistentDataPath, "savegame.json");
    }

    void Start()
    {
        if (autoLoadOnStart && File.Exists(savePath))
            LoadGame();
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current[saveKey].wasPressedThisFrame)
            SaveGame();

        if (Keyboard.current[loadKey].wasPressedThisFrame)
            LoadGame();
    }

    public void SaveGame()
    {
        if (bootstrap == null)
            bootstrap = FindFirstObjectByType<UndergroundMapBootstrap>();

        var data = new SaveData
        {
            mapLayout = bootstrap != null ? bootstrap.CurrentMap : UndergroundMapBootstrap.MapLayout.Story,
            playerPosition = transform.position,
            playerHealth = health.CurrentHealth,
            weaponState = weaponInventory.ExportState(),
            itemEntries = itemInventory.ExportEntries(),
            respawnPosition = respawn.SpawnPosition,
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"Save complete: {savePath}");
    }

    public void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning($"Save file not found: {savePath}");
            return;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(savePath));
        if (data == null)
        {
            Debug.LogWarning("Save file parse failed.");
            return;
        }

        if (bootstrap == null)
            bootstrap = FindFirstObjectByType<UndergroundMapBootstrap>();

        if (bootstrap != null)
            bootstrap.SwitchMap(data.mapLayout, data.playerPosition, false);
        else
            transform.position = data.playerPosition;

        itemInventory.ImportEntries(data.itemEntries);
        weaponInventory.ImportState(data.weaponState);
        health.SetCurrentHealth(Mathf.Max(1f, data.playerHealth));
        respawn.SetSpawnPosition(data.respawnPosition);

        Debug.Log($"Load complete: {savePath}");
    }
}
