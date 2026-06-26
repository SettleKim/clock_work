using UnityEngine;

[DefaultExecutionOrder(-50)]
public class UndergroundMapBootstrap : MonoBehaviour
{
    public enum MapLayout
    {
        Story,
        CombatTest,
        GrappleTest,
    }

    [Header("Background")]
    [SerializeField] Sprite backgroundSprite;
    [SerializeField] string backgroundResourcePath = "Backgrounds/지하";
    [SerializeField] float backgroundPixelsPerUnit = 26f;

    static readonly string[] BackgroundResourceFallbacks =
    {
        "Backgrounds/jiha",
        "Backgrounds/지하",
        "Backgrounds/underground_sewer",
    };

    [Header("Player Spawn")]
    [SerializeField] Vector2 playerSpawn = new(-24f, 2.2f);
    [SerializeField] bool combatTestMap = false;
    [SerializeField] bool grappleTestMap = true;

    const float CombatMapHalfWidth = 36f;
    static readonly Vector2 StoryFromCombatSpawn = new(-27.5f, 1.85f);
    static readonly Vector2 CombatFromStorySpawn = new(-33f, 1.8f);

    readonly Color floorColor = new(0.34f, 0.3f, 0.26f);
    readonly Color wallColor = new(0.22f, 0.2f, 0.18f);
    readonly Color hookMarkerColor = new(0.95f, 0.72f, 0.28f, 0.9f);
    readonly Color doorColor = new(0.25f, 0.42f, 0.62f);
    MapLayout currentMap;
    public MapLayout CurrentMap => currentMap;

    void Awake()
    {
        ResolveBackgroundSprite();
        currentMap = combatTestMap
            ? MapLayout.CombatTest
            : grappleTestMap
                ? MapLayout.GrappleTest
                : MapLayout.Story;
        BuildCurrentMap();
        PlacePlayer();
        SetupPlayerSystems();
        SetupCamera();
    }

    public void SwitchMap(MapLayout targetMap, Vector2 spawnPosition, bool updateRespawn = true)
    {
        currentMap = targetMap;
        combatTestMap = targetMap == MapLayout.CombatTest;
        grappleTestMap = targetMap == MapLayout.GrappleTest;

        ClearMapRoots();
        BuildCurrentMap();
        MovePlayerTo(spawnPosition, updateRespawn);
        SetupCamera();

        if (targetMap == MapLayout.CombatTest)
            ApplyCombatLoadout();
    }

    void BuildCurrentMap()
    {
        switch (currentMap)
        {
            case MapLayout.CombatTest:
                BuildCombatTestMap();
                break;
            case MapLayout.GrappleTest:
                BuildGrappleTestMap();
                break;
            default:
                BuildStoryMap();
                break;
        }
    }

    void ClearMapRoots()
    {
        string[] mapRootNames = { "CombatTestMap", "GrappleTestMap", "UndergroundMap" };
        foreach (string rootName in mapRootNames)
        {
            Transform root = transform.Find(rootName);
            if (root != null)
                Destroy(root.gameObject);
        }
    }

    void BuildCombatTestMap()
    {
        var mapRoot = new GameObject("CombatTestMap");
        mapRoot.transform.SetParent(transform);

        CreateBackground(mapRoot.transform);
        CreateLabel(mapRoot.transform, "Label_Title", new Vector2(0f, 5.5f), "보스 전투 · 콤보 테스트");
        CreateLabel(mapRoot.transform, "Label_Hint", new Vector2(0f, -3.8f), "W: 막기  |  1/2: 콤보  |  A: 공격  |  Tab: 인벤");

        CreatePlatform(mapRoot.transform, "Floor", new Vector2(0f, 0f), new Vector2(CombatMapHalfWidth * 2f, 1f));
        CreateWall(mapRoot.transform, "Wall_Left", new Vector2(-CombatMapHalfWidth + 0.4f, 3f), new Vector2(0.8f, 8f));
        CreateWall(mapRoot.transform, "Wall_Right", new Vector2(CombatMapHalfWidth - 0.4f, 3f), new Vector2(0.8f, 8f));
        CreateTrainingDummy(mapRoot.transform, "TrainingDummy", new Vector2(-20f, 1.8f));
        CreateBoss(mapRoot.transform, "Boss", new Vector2(24f, 1.8f));
        CreateHammerPickup(mapRoot.transform, "HammerPickup", new Vector2(-8f, 1.5f));
        CreateScrapPickup(mapRoot.transform, "ScrapPickup_A", new Vector2(-18f, 1.5f));
        CreateScrapPickup(mapRoot.transform, "ScrapPickup_B", new Vector2(-14f, 1.5f));
        CreateMapTransitionDoor(
            mapRoot.transform,
            "Door_CombatToStory",
            new Vector2(-34.6f, 1.85f),
            MapLayout.Story,
            StoryFromCombatSpawn,
            "스토리 맵");
    }

    void CreateBoss(Transform parent, string name, Vector2 position)
    {
        var boss = new GameObject(name);
        boss.transform.SetParent(parent);
        boss.transform.position = position;
        boss.AddComponent<Rigidbody2D>();
        boss.AddComponent<BoxCollider2D>();
        boss.AddComponent<SpriteRenderer>();
        boss.AddComponent<Health>();
        boss.AddComponent<BossController>();
        CreateLabel(parent, "Label_Boss", position + new Vector2(0f, 2.4f), "보스");
    }

    void CreateTrainingDummy(Transform parent, string name, Vector2 position)
    {
        var dummy = new GameObject(name);
        dummy.transform.SetParent(parent);
        dummy.transform.position = position;
        dummy.AddComponent<Health>();
        dummy.AddComponent<BoxCollider2D>();
        dummy.AddComponent<SpriteRenderer>();
        dummy.AddComponent<TrainingDummy>();
        CreateLabel(parent, "Label_Dummy", position + new Vector2(0f, 2f), "허수아비");
    }

    void CreateHammerPickup(Transform parent, string name, Vector2 position)
    {
        var pickup = new GameObject(name);
        pickup.transform.SetParent(parent);
        pickup.transform.position = position;

        var collider = pickup.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.9f, 0.9f);

        var sr = pickup.AddComponent<SpriteRenderer>();
        sr.sprite = CombatSpriteUtil.CreateRectSprite(10, 10, new Color(0.75f, 0.48f, 0.22f));
        sr.sortingOrder = 1;

        pickup.AddComponent<WeaponPickup>();
        CreateLabel(parent, "Label_Hammer", position + new Vector2(0f, 1.1f), "망치");
    }

    void CreateScrapPickup(Transform parent, string name, Vector2 position)
    {
        var pickup = new GameObject(name);
        pickup.transform.SetParent(parent);
        pickup.transform.position = position;

        var collider = pickup.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.8f, 0.8f);

        var sr = pickup.AddComponent<SpriteRenderer>();
        sr.sprite = CombatSpriteUtil.CreateRectSprite(10, 10, ItemDatabase.GetColor(ItemType.Scrap));
        sr.sortingOrder = 1;

        pickup.AddComponent<ItemPickup>();
        CreateLabel(parent, $"Label_{name}", position + new Vector2(0f, 1.1f), "고철");
    }

    void SetupPlayerSystems()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        EnsureComponent<PlayerWeaponInventory>(player);
        EnsureComponent<PlayerItemInventory>(player);
        EnsureComponent<PlayerMenuUI>(player);
        EnsureComponent<PlayerConsumableUse>(player);
        EnsureComponent<PlayerComboController>(player);
        EnsureComponent<PlayerParryController>(player);
        EnsureComponent<PlayerNpcInteractor>(player);
        EnsureComponent<PlayerSaveController>(player);
        EnsureComponent<WorldSlowMotionRunner>(player);

        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth != null)
            playerHealth.Configure(10f, false);

        if (combatTestMap)
            ApplyCombatLoadout();

        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        if (respawn == null)
            respawn = player.AddComponent<PlayerRespawn>();
        respawn.SetSpawnPosition(playerSpawn);

        if (player.GetComponent<WorldHealthBar>() == null)
        {
            var bar = player.AddComponent<WorldHealthBar>();
            bar.Bind(playerHealth);
        }

        ConfigureEnemyPassThrough(player);

        if (!combatTestMap)
            SetupPlayerGrapple(player);
    }

    static void EnsureComponent<T>(GameObject target) where T : Component
    {
        if (target.GetComponent<T>() == null)
            target.AddComponent<T>();
    }

    static void ConfigureEnemyPassThrough(GameObject player)
    {
        Collider2D playerCollider = player.GetComponent<Collider2D>();
        if (playerCollider == null)
            return;

        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Collider2D enemyCollider = enemy.GetComponent<Collider2D>();
            if (enemyCollider != null)
                Physics2D.IgnoreCollision(playerCollider, enemyCollider, true);
        }
    }

    void SetupPlayerGrapple(GameObject player)
    {
        if (player.GetComponent<GrapplingHookController>() == null)
            player.AddComponent<GrapplingHookController>();

        Transform hookOrigin = player.transform.Find("HookOrigin");
        if (hookOrigin == null)
        {
            var origin = new GameObject("HookOrigin");
            origin.transform.SetParent(player.transform);
            origin.transform.localPosition = new Vector3(0f, 0.55f, 0f);
        }
    }

    void ResolveBackgroundSprite()
    {
        if (backgroundSprite != null)
            return;

        if (!string.IsNullOrWhiteSpace(backgroundResourcePath))
            backgroundSprite = LoadBackgroundSprite(backgroundResourcePath);

        if (backgroundSprite != null)
            return;

        foreach (string path in BackgroundResourceFallbacks)
        {
            backgroundSprite = LoadBackgroundSprite(path);
            if (backgroundSprite != null)
                return;
        }

        Debug.LogWarning(
            "UndergroundMapBootstrap: 배경 스프라이트를 찾지 못했습니다. " +
            "Assets/Resources/Backgrounds/지하.png 가 Sprite로 임포트되었는지 확인하세요.");
    }

    static Sprite LoadBackgroundSprite(string resourcePath)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
            return sprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        if (sprites != null && sprites.Length > 0)
            return sprites[0];

        return null;
    }

    void BuildStoryMap()
    {
        var mapRoot = new GameObject("UndergroundMap");
        mapRoot.transform.SetParent(transform);

        CreateBackground(mapRoot.transform);
        CreateRoomLabels(mapRoot.transform);

        // 출발 (Limbus)
        CreatePlatform(mapRoot.transform, "Room_Start", new Vector2(-24f, 1f), new Vector2(11f, 1f));
        CreateWall(mapRoot.transform, "Wall_StartLeft", new Vector2(-30f, 3f), new Vector2(0.6f, 6f));

        // 지하
        CreatePlatform(mapRoot.transform, "Room_Underground", new Vector2(-24f, -5.5f), new Vector2(11f, 1f));
        CreatePlatform(mapRoot.transform, "Corridor_UnderToChurch", new Vector2(-10f, -5.5f), new Vector2(10f, 1f));
        CreatePlatform(mapRoot.transform, "Stairs_UnderUp", new Vector2(-15f, -2.2f), new Vector2(2f, 0.5f));

        // 교회 (허브)
        CreatePlatform(mapRoot.transform, "Room_Church", new Vector2(-2f, 0.5f), new Vector2(22f, 1.2f));

        // 출발 -> 교회 연결 복도
        CreatePlatform(mapRoot.transform, "Corridor_StartToChurch", new Vector2(-13f, 1f), new Vector2(8f, 0.8f));

        // 오른쪽 연결 복도
        CreatePlatform(mapRoot.transform, "Corridor_ChurchToRight", new Vector2(12f, 0.5f), new Vector2(8f, 0.8f));

        // 기술실
        CreatePlatform(mapRoot.transform, "Room_Tech", new Vector2(20f, 0.5f), new Vector2(10f, 1f));

        // 도서관 (상층)
        CreatePlatform(mapRoot.transform, "Room_Library", new Vector2(20f, 5.5f), new Vector2(10f, 1f));
        CreatePlatform(mapRoot.transform, "Stairs_TechToLibrary", new Vector2(16f, 3f), new Vector2(2f, 0.5f));

        // 쓰레기장 (하층)
        CreatePlatform(mapRoot.transform, "Room_Trash", new Vector2(20f, -5.5f), new Vector2(10f, 1f));
        CreatePlatform(mapRoot.transform, "Stairs_ChurchToTrash", new Vector2(16f, -2.5f), new Vector2(2f, 0.5f));

        // 독성 웅덩이 (장식용 바닥, 쓰레기장 아래)
        CreateHazardFloor(mapRoot.transform, "ToxicPool", new Vector2(0f, -8.5f), new Vector2(70f, 1.5f));

        CreateStoryDoors(mapRoot.transform);

        SpawnEnemies(mapRoot.transform);
    }

    void CreateStoryDoors(Transform parent)
    {
        CreateMapTransitionDoor(
            parent,
            "Door_StoryToCombat",
            new Vector2(-29.3f, 1.85f),
            MapLayout.CombatTest,
            CombatFromStorySpawn,
            "콤보 연습");
        CreateDoor(parent, "Door_StartToChurch", new Vector2(-18f, 1.85f), new Vector2(-8f, 1.85f), "교회");
        CreateDoor(parent, "Door_ChurchToStart", new Vector2(-8f, 1.85f), new Vector2(-18f, 1.85f), "출발");

        CreateDoor(parent, "Door_ChurchToUnderground", new Vector2(-12f, 1.2f), new Vector2(-14f, -4.7f), "지하");
        CreateDoor(parent, "Door_UndergroundToChurch", new Vector2(-14f, -4.7f), new Vector2(-12f, 1.2f), "교회");

        CreateDoor(parent, "Door_ChurchToTech", new Vector2(10f, 1.2f), new Vector2(16f, 1.2f), "기술실");
        CreateDoor(parent, "Door_TechToChurch", new Vector2(16f, 1.2f), new Vector2(10f, 1.2f), "교회");

        CreateDoor(parent, "Door_TechToLibrary", new Vector2(16f, 3.6f), new Vector2(20f, 6.8f), "도서");
        CreateDoor(parent, "Door_LibraryToTech", new Vector2(20f, 6.8f), new Vector2(16f, 3.6f), "기술실");

        CreateDoor(parent, "Door_ChurchToTrash", new Vector2(16f, -1.5f), new Vector2(20f, -4.2f), "쓰레기장");
        CreateDoor(parent, "Door_TrashToChurch", new Vector2(20f, -4.2f), new Vector2(16f, -1.5f), "교회");
    }

    void BuildGrappleTestMap()
    {
        var mapRoot = new GameObject("GrappleTestMap");
        mapRoot.transform.SetParent(transform);

        CreateBackground(mapRoot.transform);
        CreateLabel(mapRoot.transform, "Label_Title", new Vector2(0f, 12.5f), "갈고리 테스트 구역");
        CreateLabel(mapRoot.transform, "Label_Hint", new Vector2(0f, -7.2f), "E 꾹: 슬로우 선택  |  방향키: 포인트  |  E 떼기: 발사");

        // 시작 발판 → 건너편
        CreatePlatform(mapRoot.transform, "StartPlatform", new Vector2(-14f, 1f), new Vector2(8f, 1f));
        CreateGrapplePoint(mapRoot.transform, "Grapple_StartToFar", new Vector2(-10f, 2.8f), new Vector2(1f, 0.35f), 30f, 4.5f);

        // 건너편 → 시작
        CreatePlatform(mapRoot.transform, "FarPlatform", new Vector2(14f, 1f), new Vector2(8f, 1f));
        CreateGrapplePoint(mapRoot.transform, "Grapple_FarToStart", new Vector2(10f, 2.8f), new Vector2(-1f, 0.35f), 30f, 4.5f);

        // 중앙 → 상단
        CreatePlatform(mapRoot.transform, "MidAirPlatform", new Vector2(0f, 5f), new Vector2(5f, 0.8f));
        CreateGrapplePoint(mapRoot.transform, "Grapple_MidToTop", new Vector2(0f, 6.3f), new Vector2(0.15f, 1f), 24f, 4f);

        // 천장 → 아래 발판
        CreatePlatform(mapRoot.transform, "CeilingBeam", new Vector2(0f, 10.5f), new Vector2(28f, 0.8f));
        CreateGrapplePoint(mapRoot.transform, "Grapple_CeilingLeft", new Vector2(-11f, 9.8f), new Vector2(0.65f, -0.55f), 28f, 4f);
        CreateGrapplePoint(mapRoot.transform, "Grapple_CeilingRight", new Vector2(11f, 9.8f), new Vector2(-0.65f, -0.55f), 28f, 4f);

        // 좌우 절벽 → 중앙 상부
        CreateWall(mapRoot.transform, "LeftCliff", new Vector2(-22f, 5f), new Vector2(1.2f, 18f));
        CreateWall(mapRoot.transform, "RightCliff", new Vector2(22f, 5f), new Vector2(1.2f, 18f));
        CreateGrapplePoint(mapRoot.transform, "Grapple_LeftCliff", new Vector2(-20.5f, 6f), new Vector2(0.9f, 0.35f), 29f, 4.5f);
        CreateGrapplePoint(mapRoot.transform, "Grapple_RightCliff", new Vector2(20.5f, 6f), new Vector2(-0.9f, 0.35f), 29f, 4.5f);

        // 상부 발판
        CreatePlatform(mapRoot.transform, "UpperLeftLedge", new Vector2(-10f, 7.5f), new Vector2(6f, 0.8f));
        CreatePlatform(mapRoot.transform, "UpperRightLedge", new Vector2(10f, 7.5f), new Vector2(6f, 0.8f));
        CreateGrapplePoint(mapRoot.transform, "Grapple_UpperLeftDown", new Vector2(-10f, 8.3f), new Vector2(0.2f, -0.85f), 26f, 3.5f);
        CreateGrapplePoint(mapRoot.transform, "Grapple_UpperRightDown", new Vector2(10f, 8.3f), new Vector2(-0.2f, -0.85f), 26f, 3.5f);

        // 스윙 벽
        CreateWall(mapRoot.transform, "SwingWallLeft", new Vector2(-8f, -1f), new Vector2(0.8f, 8f));
        CreateWall(mapRoot.transform, "SwingWallRight", new Vector2(8f, -1f), new Vector2(0.8f, 8f));
        CreateGrapplePoint(mapRoot.transform, "Grapple_SwingLeft", new Vector2(-7.2f, 1.5f), new Vector2(0.85f, 0.75f), 27f, 4f);
        CreateGrapplePoint(mapRoot.transform, "Grapple_SwingRight", new Vector2(7.2f, 1.5f), new Vector2(-0.85f, 0.75f), 27f, 4f);

        // 웅덩이 탈출
        CreateHazardFloor(mapRoot.transform, "ToxicPool", new Vector2(0f, -6.5f), new Vector2(52f, 1.5f));
        CreatePlatform(mapRoot.transform, "RescueIsland", new Vector2(0f, -4.5f), new Vector2(4f, 0.8f));
        CreateGrapplePoint(mapRoot.transform, "Grapple_PitEscape", new Vector2(0f, -3.8f), new Vector2(0.25f, 1f), 25f, 3.5f);
    }

    void CreateGrapplePoint(Transform parent, string name, Vector2 position, Vector2 launchDirection, float launchSpeed, float useRadius)
    {
        var pointObject = new GameObject(name);
        pointObject.transform.SetParent(parent);
        pointObject.transform.position = position;

        var sr = pointObject.AddComponent<SpriteRenderer>();
        sr.sprite = CombatSpriteUtil.CreateRectSprite(5, 5, hookMarkerColor);
        sr.sortingOrder = 5;

        var point = pointObject.AddComponent<GrapplePoint>();
        point.Configure(launchDirection, launchSpeed, useRadius);
    }

    void CreateBackground(Transform parent)
    {
        if (backgroundSprite == null)
            return;

        var bg = new GameObject("Background");
        bg.transform.SetParent(parent);

        float ppu = backgroundSprite.pixelsPerUnit > 0f
            ? backgroundSprite.pixelsPerUnit
            : backgroundPixelsPerUnit;
        float worldWidth = backgroundSprite.rect.width / ppu;
        float worldHeight = backgroundSprite.rect.height / ppu;

        float mapWidth = combatTestMap ? CombatMapHalfWidth * 2f + 4f : grappleTestMap ? 52f : 68f;
        float mapHeight = combatTestMap ? 16f : grappleTestMap ? 28f : 24f;
        float scale = Mathf.Max(mapWidth / worldWidth, mapHeight / worldHeight, 1f);

        bg.transform.position = new Vector3(0f, 1f, 0f);
        bg.transform.localScale = new Vector3(scale, scale, 1f);

        var sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = backgroundSprite;
        sr.sortingOrder = -100;
        sr.drawMode = SpriteDrawMode.Simple;
    }

    void CreateRoomLabels(Transform parent)
    {
        CreateLabel(parent, "Label_Start", new Vector2(-24f, 4.2f), "Limbus / 출발");
        CreateLabel(parent, "Label_Underground", new Vector2(-24f, -2.8f), "지하");
        CreateLabel(parent, "Label_Church", new Vector2(-2f, 3.2f), "교회");
        CreateLabel(parent, "Label_Library", new Vector2(20f, 8f), "도서");
        CreateLabel(parent, "Label_Tech", new Vector2(20f, 3f), "기술실");
        CreateLabel(parent, "Label_Trash", new Vector2(20f, -3f), "쓰레기장");
    }

    void CreateLabel(Transform parent, string name, Vector2 position, string text)
    {
        var labelObject = new GameObject(name);
        labelObject.transform.SetParent(parent);
        labelObject.transform.position = position;

        var textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.characterSize = 0.12f;
        textMesh.fontSize = 48;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.color = new Color(0.85f, 0.9f, 0.82f, 0.85f);

        var meshRenderer = labelObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.sortingOrder = 20;
    }

    void CreateDoor(Transform parent, string name, Vector2 center, Vector2 destination, string destinationLabel)
    {
        var door = new GameObject(name);
        door.transform.SetParent(parent);
        door.transform.position = center;

        var collider = door.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.2f, 2f);

        var sr = door.AddComponent<SpriteRenderer>();
        sr.sprite = CombatSpriteUtil.CreateRectSprite(10, 20, doorColor);
        sr.sortingOrder = 2;

        var doorComponent = door.AddComponent<MapDoor>();
        doorComponent.Configure(destination);

        CreateLabel(parent, $"Label_{name}", center + new Vector2(0f, 1.5f), $"문 → {destinationLabel}");
    }

    void CreateMapTransitionDoor(
        Transform parent,
        string name,
        Vector2 center,
        MapLayout targetMap,
        Vector2 targetSpawn,
        string destinationLabel)
    {
        var door = new GameObject(name);
        door.transform.SetParent(parent);
        door.transform.position = center;

        var collider = door.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.2f, 2f);

        var sr = door.AddComponent<SpriteRenderer>();
        sr.sprite = CombatSpriteUtil.CreateRectSprite(10, 20, doorColor);
        sr.sortingOrder = 2;

        var doorComponent = door.AddComponent<MapDoor>();
        doorComponent.ConfigureMapTransition(targetMap, targetSpawn);

        CreateLabel(parent, $"Label_{name}", center + new Vector2(0f, 1.5f), $"문 → {destinationLabel}");
    }

    void CreatePlatform(Transform parent, string name, Vector2 center, Vector2 size)
    {
        var platform = new GameObject(name);
        platform.transform.SetParent(parent);
        platform.transform.position = center;
        platform.layer = LayerMask.NameToLayer("Default");

        var collider = platform.AddComponent<BoxCollider2D>();
        collider.size = size;

        var sr = platform.AddComponent<SpriteRenderer>();
        sr.sprite = CombatSpriteUtil.CreateGroundSprite(64, 8);
        sr.color = floorColor;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.sortingOrder = 0;
    }

    void CreateWall(Transform parent, string name, Vector2 center, Vector2 size)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.position = center;

        var collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;

        var sr = wall.AddComponent<SpriteRenderer>();
        sr.sprite = CombatSpriteUtil.CreateRectSprite(8, 32, wallColor);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.sortingOrder = -1;
    }

    void CreateHazardFloor(Transform parent, string name, Vector2 center, Vector2 size)
    {
        var hazard = new GameObject(name);
        hazard.transform.SetParent(parent);
        hazard.transform.position = center;

        var sr = hazard.AddComponent<SpriteRenderer>();
        sr.sprite = CombatSpriteUtil.CreateRectSprite(32, 4, new Color(0.2f, 0.85f, 0.35f, 0.55f));
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = size;
        sr.sortingOrder = -5;
    }

    void SpawnEnemies(Transform parent)
    {
        var enemyRoot = new GameObject("Enemies");
        enemyRoot.transform.SetParent(parent);

        SpawnEnemy(enemyRoot.transform, "Enemy_Underground", new Vector2(-22f, -4.2f), 2.5f);
        SpawnEnemy(enemyRoot.transform, "Enemy_Church_A", new Vector2(-6f, 1.7f), 4f);
        SpawnEnemy(enemyRoot.transform, "Enemy_Church_B", new Vector2(2f, 1.7f), 4f);
        SpawnEnemy(enemyRoot.transform, "Enemy_Library", new Vector2(20f, 6.7f), 2f);
        SpawnEnemy(enemyRoot.transform, "Enemy_Tech", new Vector2(20f, 1.7f), 2.5f);
        SpawnEnemy(enemyRoot.transform, "Enemy_Trash_A", new Vector2(17f, -4.2f), 2f);
        SpawnEnemy(enemyRoot.transform, "Enemy_Trash_B", new Vector2(23f, -4.2f), 2f);
    }

    void SpawnEnemy(Transform parent, string name, Vector2 position, float patrolDistance)
    {
        var enemy = new GameObject(name);
        enemy.transform.SetParent(parent);
        enemy.transform.position = position;
        enemy.tag = "Enemy";

        enemy.AddComponent<Rigidbody2D>();
        enemy.AddComponent<BoxCollider2D>();
        enemy.AddComponent<SpriteRenderer>();
        enemy.AddComponent<Health>();
        var testEnemy = enemy.AddComponent<TestEnemy>();
        testEnemy.Configure(patrolDistance);
    }

    void PlacePlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        player.transform.position = playerSpawn;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    void MovePlayerTo(Vector2 position, bool updateRespawn)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        player.transform.position = position;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (!updateRespawn)
            return;

        PlayerRespawn respawn = player.GetComponent<PlayerRespawn>();
        if (respawn != null)
            respawn.SetSpawnPosition(position);
    }

    void ApplyCombatLoadout()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        PlayerWeaponInventory weaponInventory = player.GetComponent<PlayerWeaponInventory>();
        if (weaponInventory != null && !weaponInventory.Owns(WeaponType.Hammer))
            weaponInventory.UnlockWeapon(WeaponType.Hammer);

        PlayerItemInventory itemInventory = player.GetComponent<PlayerItemInventory>();
        if (itemInventory != null && itemInventory.GetCount(ItemType.Potion) <= 0)
            itemInventory.AddItem(ItemType.Potion, 3);
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        cam.orthographicSize = currentMap == MapLayout.CombatTest ? 5f : 6f;
        cam.backgroundColor = new Color(0.04f, 0.06f, 0.05f);

        CameraFollow2D follow = cam.GetComponent<CameraFollow2D>();
        if (follow == null)
            follow = cam.gameObject.AddComponent<CameraFollow2D>();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            follow.SetTarget(player.transform);

        follow.SetBounds(
            currentMap == MapLayout.CombatTest ? new Vector2(-CombatMapHalfWidth, -4f) : currentMap == MapLayout.GrappleTest ? new Vector2(-26f, -9f) : new Vector2(-34f, -10f),
            currentMap == MapLayout.CombatTest ? new Vector2(CombatMapHalfWidth, 8f) : currentMap == MapLayout.GrappleTest ? new Vector2(26f, 14f) : new Vector2(28f, 12f));
    }
}
