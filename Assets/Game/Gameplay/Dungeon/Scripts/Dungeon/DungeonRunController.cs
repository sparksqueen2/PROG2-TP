using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public class DungeonRunController : MonoBehaviour
{
    private struct SectionConfig
    {
        public string ChapterTitle;
        public string ChapterBody;
        public string HudHeader;
        public string HudSubtitle;
        public string ProgressLabel;
        public string CompleteHeader;
        public string CompleteMessage;
        public Vector3[] GuardianSpawns;
        public Vector3[] OptionalSpawns;
        public float BlockerX;
        public float BlockerNorthZ;
        public bool HasBlocker;
    }

    private const int EnemiesPerWave = 5;
    private const int GuardiansPerWave = 2;
    private const int OptionalPerWave = 3;

    private static readonly SectionConfig[] Sections =
    {
        new SectionConfig
        {
            ChapterTitle = "CAPITULO I\nEl Umbral Corrupto",
            ChapterBody =
                "Desterrado por portar un arma runica prohibida, Kael llega al primer umbral del bosque. " +
                "La corrupcion bloquea el paso. Para avanzar, debera limpiar a sus guardianes.",
            HudHeader = "UMBRAL SELLADO",
            HudSubtitle = "Derrota a los guardianes corruptos",
            ProgressLabel = "Guardianes",
            CompleteHeader = "UMBRAL PURIFICADO",
            CompleteMessage = "El camino empieza a abrirse.",
            GuardianSpawns = new[]
            {
                new Vector3(-2f, 0f, -25f),
                new Vector3(22f, 0f, -26f)
            },
            OptionalSpawns = new[]
            {
                new Vector3(5f, 0f, -22f),
                new Vector3(13f, 0f, -28f),
                new Vector3(19f, 0f, -24f)
            },
            BlockerX = 0f,
            BlockerNorthZ = -12f,
            HasBlocker = true
        },
        new SectionConfig
        {
            ChapterTitle = "CAPITULO II\nLos Ecos del Destierro",
            ChapterBody =
                "El bosque recuerda lo que el reino quiso olvidar. Cada sombra repite la condena de Kael: " +
                "traidor, exiliado, portador del hierro prohibido.",
            HudHeader = "ECOS DEL DESTIERRO",
            HudSubtitle = "Resiste la nueva oleada",
            ProgressLabel = "Ecos derrotados",
            CompleteHeader = string.Empty,
            CompleteMessage = "Los ecos callan. Pero el juramento sigue intacto.",
            GuardianSpawns = new[]
            {
                new Vector3(-0.1f, 0f, -10.5f),
                new Vector3(12.2f, 0f, -10.5f)
            },
            OptionalSpawns = new[]
            {
                new Vector3(5f, 0f, -9f),
                new Vector3(8f, 0f, -7.5f),
                new Vector3(16f, 0f, -11f)
            },
            BlockerX = 55f,
            BlockerNorthZ = 0f,
            HasBlocker = true
        },
        new SectionConfig
        {
            ChapterTitle = "CAPITULO III\nEl Juramento de Hierro",
            ChapterBody =
                "El ultimo umbral reconoce el arma de Kael. La corrupcion ya no intenta detenerlo: intenta reclamarlo.",
            HudHeader = "JURAMENTO DE HIERRO",
            HudSubtitle = "Rompe la ultima defensa del umbral",
            ProgressLabel = "Guardianes",
            CompleteHeader = "EL UMBRAL SE ABRE",
            CompleteMessage = "La runa del juramento queda expuesta. Segui la luz al norte.",
            GuardianSpawns = new[]
            {
                new Vector3(64f, 0f, -1f),
                new Vector3(68f, 0f, 2f)
            },
            OptionalSpawns = new[]
            {
                new Vector3(61f, 0f, -3f),
                new Vector3(66f, 0f, 0f),
                new Vector3(71f, 0f, -2f)
            },
            BlockerX = 0f,
            BlockerNorthZ = 0f,
            HasBlocker = false
        }
    };

    private const float MinSpawnDistanceFromPlayer = 12f;
    private const float SouthernCorridorMaxSpawnZ = -14.5f;
    private const float DefaultVisionRange = 3f;
    private const float DefaultLoseVisionRange = 5f;
    private const float EnemyWalkSpeed = 2.2f;
    private const float EnemyChaseSpeed = 3.4f;
    private const string ExitObjectiveHeader = "RUNA DEL JURAMENTO";
    private const string ExitObjective =
        "El sello ancestral responde al hierro de Kael. Cruza el circulo para cerrar la grieta.";
    private const string WinScreenTitle = "LA GRIETA SE CIERRA";
    private const string WinEpilogue = "El bosque queda atras. Pero el juramento todavia pesa.";
    private const string WinScreenButton = "VOLVER AL MENU";
    private const string WinMessage =
        "La corrupcion retrocede.\nLas armas nunca fueron malditas.\nLa Guardia mintio para ocultar su miedo.";

    [SerializeField] private float blockerSpanZ = 40f;
    [SerializeField] private float blockerSpanX = 55f;
    [SerializeField] private float blockerCenterZ = -20f;
    [SerializeField] private float blockerCenterX = 22f;
    [SerializeField] private float blockerHeight = 5f;
    [SerializeField] private float sectionCompleteDelay = 2.5f;
    [SerializeField] private GameObject chestMonsterPrefab;
    [SerializeField] private GameObject watcherPrefab;
    [SerializeField] private GameObject beholderPrefab;
    [SerializeField] private ChapterScreenView chapterScreen;
    [SerializeField] private ChapterScreenView chapterScreenPrefab;

    private const string ChapterScreenPrefabPath = "Assets/Game/Gameplay/Dungeon/Prefabs/UI/ChapterScreen.prefab";

    private readonly List<DungeonRequiredEnemy> activeRequiredEnemies = new List<DungeonRequiredEnemy>();
    private readonly List<List<GameObject>> sectionBlockerGroups = new List<List<GameObject>>();
    private readonly List<Enemy> carryOverOptionalEnemies = new List<Enemy>();

    private Transform progressionRoot;
    private Transform waveRoot;
    private WinZone winZone;
    private GameplayUI gameplayUI;
    private TextMeshProUGUI objectiveText;
    private PlayerController playerController;
    private ChapterScreenView chapterScreenInstance;
    private int currentSection;
    private int pendingSectionIndex = -1;
    private int totalRequiredInSection;
    private int optionalAliveInWave;
    private int optionalCountAtWaveStart;
    private bool isRunActive;
    private bool canExit;
    private bool hasStarted;
    private bool isTransitioning;
    private bool legacyEnemiesDisabled;

    private DungeonWaveSpawnGroup[] sceneSpawnGroups;

    public bool CanWin => canExit;

    private void Awake()
    {
        DisableLegacySceneEnemies();
        EnsureWavePrefabs();
        EnsureChapterScreenPrefab();
    }

    public void Init(WinZone zone, GameplayUI ui, PlayerController player)
    {
        winZone = zone;
        gameplayUI = ui;
        playerController = player;
    }

    public void BeginDungeon()
    {
        if (hasStarted)
            return;

        hasStarted = true;
        StartCoroutine(BeginDungeonRoutine());
    }

    private IEnumerator BeginDungeonRoutine()
    {
        if (winZone == null)
            winZone = FindFirstObjectByType<WinZone>();

        if (gameplayUI == null)
            gameplayUI = FindFirstObjectByType<GameplayUI>();

        EnsureWavePrefabs();

        progressionRoot = new GameObject("---DUNGEON_PROGRESSION---").transform;
        waveRoot = new GameObject("---DUNGEON_WAVES---").transform;
        waveRoot.SetParent(progressionRoot, false);

        BuildHud();
        winZone?.SetUnlocked(false);

        yield return null;
        DungeonNavMeshSetup.Build();

        try
        {
            SetupSections();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Dungeon] Error al iniciar oleada: {ex.Message}");
        }

        ShowChapterScreen(0);
    }

    private void EnsureWavePrefabs()
    {
#if UNITY_EDITOR
        chestMonsterPrefab = LoadWavePrefab("Assets/Game/Gameplay/Dungeon/Prefabs/Enemies/ChestMonster.prefab");
        watcherPrefab = LoadWavePrefab("Assets/Game/Gameplay/Dungeon/Prefabs/Enemies/Watcher.prefab");
        beholderPrefab = LoadWavePrefab("Assets/Game/Gameplay/Dungeon/Prefabs/Enemies/Beholder.prefab");
#endif

        if (!IsValidEnemyPrefab(chestMonsterPrefab)
            || !IsValidEnemyPrefab(watcherPrefab)
            || !IsValidEnemyPrefab(beholderPrefab))
        {
            Debug.LogError("[Dungeon] Faltan prefabs de enemigos válidos en DungeonRunController.");
        }
    }

    private static bool IsValidEnemyPrefab(GameObject prefab)
    {
        return prefab != null && prefab.GetComponentInChildren<EnemyHealth>(true) != null;
    }

    private static GameObject LoadWavePrefab(string assetPath)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
#else
        return null;
#endif
    }

    private static GameObject InstantiateEnemyPrefab(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        DungeonNavMeshSetup.TrySampleWalkable(ref position);
        return Object.Instantiate(prefab, position, rotation, parent);
    }

    private void OnEnable()
    {
        EnemyManager.OnEnemyDefeated += HandleEnemyDefeated;
    }

    private void OnDisable()
    {
        EnemyManager.OnEnemyDefeated -= HandleEnemyDefeated;
    }

    private void SetupSections()
    {
        DisableLegacySceneEnemies();
        EnsureWaveSpawnMarkersInScene();
        RefreshSpawnGroups();
        ActivateSceneBlockers();
        CreateSectionBlockers();
    }

    private void EnsureWaveSpawnMarkersInScene()
    {
        var progression = GameObject.Find("DungeonProgression");
        if (progression == null)
            return;

        var existing = progression.transform.Find("---WAVE_SPAWNS---");
        DungeonWaveSpawnRoot root;

        if (existing == null)
        {
            var rootObject = new GameObject("---WAVE_SPAWNS---");
            rootObject.transform.SetParent(progression.transform, false);
            root = rootObject.AddComponent<DungeonWaveSpawnRoot>();
        }
        else
        {
            root = existing.GetComponent<DungeonWaveSpawnRoot>();
            if (root == null)
                root = existing.gameObject.AddComponent<DungeonWaveSpawnRoot>();
        }

        if (root.transform.childCount == 0)
            root.EnsureDefaultLayout();
    }

    private void RefreshSpawnGroups()
    {
        sceneSpawnGroups = FindObjectsByType<DungeonWaveSpawnGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        System.Array.Sort(sceneSpawnGroups, (a, b) => a.SectionIndex.CompareTo(b.SectionIndex));
    }

    private void DisableLegacySceneEnemies()
    {
        if (legacyEnemiesDisabled)
            return;

        legacyEnemiesDisabled = true;
        var enemiesRoot = GameObject.Find("Enemies");
        if (enemiesRoot == null)
            return;

        foreach (Transform child in enemiesRoot.transform)
            child.gameObject.SetActive(false);
    }

    private void ActivateSceneBlockers()
    {
        foreach (var blocker in FindObjectsByType<SectionBlocker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (blocker == null)
                continue;

            blocker.RefreshVisual();
            blocker.gameObject.SetActive(true);
        }
    }

    private void CreateSectionBlockers()
    {
        sectionBlockerGroups.Clear();
        var sceneBlockers = FindObjectsByType<SectionBlocker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var floorY = GetFloorY();

        for (var i = 0; i < Sections.Length; i++)
        {
            var section = Sections[i];
            var group = new List<GameObject>();

            foreach (var blocker in sceneBlockers)
            {
                if (blocker != null && blocker.SectionIndex == i)
                    group.Add(blocker.gameObject);
            }

            if (group.Count == 0 && section.HasBlocker)
            {
                if (section.BlockerX > 0f)
                {
                    group.Add(CreateBlockerWall(
                        $"SectionBlocker_East_{i + 1}",
                        new Vector3(section.BlockerX, floorY + blockerHeight * 0.5f, blockerCenterZ),
                        new Vector3(2f, blockerHeight, blockerSpanZ)));
                }

                if (Mathf.Abs(section.BlockerNorthZ) > 0.01f)
                {
                    group.Add(CreateBlockerWall(
                        $"SectionBlocker_North_{i + 1}",
                        new Vector3(blockerCenterX, floorY + blockerHeight * 0.5f, section.BlockerNorthZ),
                        new Vector3(blockerSpanX, blockerHeight, 2f)));
                }
            }

            sectionBlockerGroups.Add(group);
        }
    }

    private GameObject CreateBlockerWall(string wallName, Vector3 position, Vector3 scale)
    {
        var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.name = wallName;
        blocker.transform.SetParent(progressionRoot, false);
        blocker.transform.position = position;
        blocker.transform.localScale = scale;
        blocker.isStatic = true;

        CorruptionVisual.ApplyToBlocker(blocker);

        var rootRenderer = blocker.GetComponent<MeshRenderer>();
        if (rootRenderer != null)
            rootRenderer.enabled = false;

        return blocker;
    }

    private static float GetFloorY()
    {
        var spawn = Object.FindFirstObjectByType<PlayerSpawnPoint>(FindObjectsInactive.Include);
        var anchor = spawn != null ? spawn.transform.position : new Vector3(7f, 0f, -24f);
        return DungeonGroundSnap.GetGroundY(anchor);
    }

    private void BeginSection(int sectionIndex)
    {
        currentSection = sectionIndex;
        activeRequiredEnemies.Clear();
        isTransitioning = false;

        if (sectionIndex >= Sections.Length)
        {
            UnlockExit();
            return;
        }

        if (!IsValidEnemyPrefab(chestMonsterPrefab)
            || !IsValidEnemyPrefab(watcherPrefab)
            || !IsValidEnemyPrefab(beholderPrefab))
        {
            Debug.LogError("[Dungeon] Faltan prefabs de enemigos válidos.");
            return;
        }

        var section = Sections[sectionIndex];
        totalRequiredInSection = GuardiansPerWave;
        PrepareCarryOver(section);
        SpawnWave(section, sectionIndex);
        optionalCountAtWaveStart = optionalAliveInWave;

        RefreshObjectiveDisplay();
        Debug.Log($"[Dungeon] Sección {sectionIndex + 1}: {activeRequiredEnemies.Count} guardianes, {optionalAliveInWave} opcionales activos.");
    }

    private void PrepareCarryOver(SectionConfig section)
    {
        carryOverOptionalEnemies.RemoveAll(enemy => enemy == null);
        var optionalSpawns = ResolveOptionalSpawns(currentSection, section);

        for (var i = 0; i < carryOverOptionalEnemies.Count; i++)
        {
            var enemy = carryOverOptionalEnemies[i];
            if (enemy == null)
                continue;

            var spawnIndex = Mathf.Min(i, optionalSpawns.Length - 1);
            var placedOnNavMesh = PlaceEnemy(enemy.gameObject, optionalSpawns[spawnIndex]);
            ConfigureWaveSpawnBehavior(enemy.gameObject, placedOnNavMesh);

            var waveMarker = enemy.GetComponent<DungeonWaveEnemy>();
            if (waveMarker == null)
                waveMarker = enemy.gameObject.AddComponent<DungeonWaveEnemy>();
            waveMarker.Configure(false, currentSection);
        }
    }

    private void SpawnWave(SectionConfig section, int sectionIndex)
    {
        var guardianPrefabs = GetGuardianPrefabs(sectionIndex);
        var optionalPool = GetOptionalPool(sectionIndex);
        var carryCount = carryOverOptionalEnemies.Count;
        var guardianSpawns = ResolveGuardianSpawns(sectionIndex, section);
        var optionalSpawns = ResolveOptionalSpawns(sectionIndex, section);

        for (var i = 0; i < guardianPrefabs.Length && i < guardianSpawns.Length; i++)
        {
            if (guardianPrefabs[i] == null)
                continue;

            var spawned = SpawnEnemy(guardianPrefabs[i], guardianSpawns[i], sectionIndex, true);
            if (spawned != null)
                activeRequiredEnemies.Add(spawned);
        }

        var optionalToSpawn = OptionalPerWave - carryCount;
        var spawnOffset = carryCount;

        for (var i = 0; i < optionalToSpawn; i++)
        {
            var spawnIndex = spawnOffset + i;
            if (spawnIndex >= optionalSpawns.Length || optionalPool.Length == 0)
                break;

            var prefab = optionalPool[Random.Range(0, optionalPool.Length)];
            if (prefab == null)
                continue;

            SpawnEnemy(prefab, optionalSpawns[spawnIndex], sectionIndex, false);
        }

        optionalAliveInWave = CountOptionalAlive(sectionIndex);
        WarpWaveAgents();
    }

    private Vector3[] ResolveGuardianSpawns(int sectionIndex, SectionConfig section)
    {
        var group = FindSpawnGroup(sectionIndex);
        if (group != null)
        {
            var spawns = group.GetGuardianSpawns(GuardiansPerWave);
            if (spawns != null && spawns.Length >= GuardiansPerWave)
                return SanitizeSpawns(spawns, fromScene: true);
        }

        return SanitizeSpawns(section.GuardianSpawns, fromScene: false);
    }

    private Vector3[] ResolveOptionalSpawns(int sectionIndex, SectionConfig section)
    {
        var group = FindSpawnGroup(sectionIndex);
        if (group != null)
        {
            var spawns = group.GetOptionalSpawns(OptionalPerWave);
            if (spawns != null && spawns.Length >= OptionalPerWave)
                return SanitizeSpawns(spawns, fromScene: true);
        }

        return SanitizeSpawns(section.OptionalSpawns, fromScene: false);
    }

    private DungeonWaveSpawnGroup FindSpawnGroup(int sectionIndex)
    {
        if (sceneSpawnGroups == null || sceneSpawnGroups.Length == 0)
            RefreshSpawnGroups();

        foreach (var group in sceneSpawnGroups)
        {
            if (group != null && group.SectionIndex == sectionIndex)
                return group;
        }

        return null;
    }

    private static Vector3[] SanitizeSpawns(Vector3[] spawns, bool fromScene)
    {
        if (spawns == null)
            return null;

        var copy = new Vector3[spawns.Length];
        for (var i = 0; i < spawns.Length; i++)
        {
            copy[i] = spawns[i];
            copy[i].y = 0f;

            if (!fromScene)
                EnsureMinDistanceFromPlayer(ref copy[i]);
        }

        return copy;
    }

    private int CountOptionalAlive(int sectionIndex)
    {
        var count = carryOverOptionalEnemies.Count;

        if (waveRoot == null)
            return count;

        foreach (var enemy in waveRoot.GetComponentsInChildren<Enemy>(true))
        {
            if (enemy == null)
                continue;

            var waveMarker = enemy.GetComponent<DungeonWaveEnemy>();
            var health = enemy.GetComponentInChildren<EnemyHealth>();
            if (waveMarker == null || waveMarker.IsGuardian || waveMarker.WaveSection != sectionIndex)
                continue;
            if (health != null && health.IsDead())
                continue;

            if (!carryOverOptionalEnemies.Contains(enemy))
                count++;
        }

        return count;
    }

    private DungeonRequiredEnemy SpawnEnemy(GameObject prefab, Vector3 position, int sectionIndex, bool isGuardian)
    {
        if (!IsValidEnemyPrefab(prefab))
        {
            Debug.LogError($"[Dungeon] Prefab inválido al spawnear: {(prefab != null ? prefab.name : "null")}");
            return null;
        }

        if (!DungeonNavMeshSetup.TrySampleWalkable(ref position))
            Debug.LogWarning($"[Dungeon] Spawn sin NavMesh cercano en {position} para {prefab.name}.");

        var instance = InstantiateEnemyPrefab(prefab, position, Quaternion.identity, waveRoot);
        if (instance == null)
        {
            Debug.LogError($"[Dungeon] No se pudo instanciar {prefab.name}.");
            return null;
        }
        instance.name = isGuardian ? $"Guardian_{sectionIndex + 1}_{activeRequiredEnemies.Count + 1}" : $"Loot_{sectionIndex + 1}_{optionalAliveInWave + 1}";

        var agent = instance.GetComponent<NavMeshAgent>();
        if (agent != null)
            agent.enabled = false;

        var placedOnNavMesh = PlaceEnemy(instance, position);
        SoftenEnemy(instance);

        var waveMarker = instance.GetComponent<DungeonWaveEnemy>();
        if (waveMarker == null)
            waveMarker = instance.AddComponent<DungeonWaveEnemy>();
        waveMarker.Configure(isGuardian, sectionIndex);

        if (agent != null)
        {
            if (placedOnNavMesh)
            {
                agent.enabled = true;
                DungeonNavMeshSetup.WarpAgent(agent);
            }
            else
            {
                agent.enabled = false;
                Debug.LogWarning($"[Dungeon] {instance.name} quedó sin NavMesh; movimiento por transform.");
            }
        }

        if (placedOnNavMesh)
            instance.GetComponent<EnemyNavigation>()?.EnsureReady();

        ConfigureWaveSpawnBehavior(instance, placedOnNavMesh);

        var enemyComponent = instance.GetComponent<Enemy>();
        enemyComponent?.RefreshPlayerTarget();

        if (isGuardian)
        {
            AddGuardianCorruptionAura(instance);

            var marker = instance.GetComponent<DungeonRequiredEnemy>();
            if (marker == null)
                marker = instance.AddComponent<DungeonRequiredEnemy>();

            marker.Configure(sectionIndex);
            return marker;
        }

        return null;
    }

    private static void ConfigureWaveSpawnBehavior(GameObject instance, bool placedOnNavMesh)
    {
        instance.GetComponent<EnemyBeholderController>()?.RefreshHomePosition();

        var chest = instance.GetComponent<EnemyChestController>();
        if (chest != null)
            chest.ConfigureWaveSpawn(placedOnNavMesh);

        var watcher = instance.GetComponent<EnemyWatcherController>();
        if (watcher != null)
            watcher.ConfigureWaveSpawn();
    }

    private static void AddGuardianCorruptionAura(GameObject enemy)
    {
        CorruptionVisual.ApplyGuardianAura(enemy);
    }

    private static void SoftenEnemy(GameObject instance)
    {
        foreach (var health in instance.GetComponentsInChildren<EnemyHealth>(true))
        {
            var max = health.GetMaxHealth();
            if (max <= 0)
                continue;

            var scaled = Mathf.Max(12, Mathf.RoundToInt(max * 0.55f));
            health.SetMaxHealth(scaled, refill: true);
        }

        var enemy = instance.GetComponent<Enemy>();
        if (enemy != null)
            enemy.ConfigureMovement(EnemyWalkSpeed, EnemyChaseSpeed);

        var watcher = instance.GetComponent<EnemyWatcherController>();
        if (watcher != null)
            watcher.ConfigureDetection(DefaultVisionRange, DefaultLoseVisionRange);

        var chest = instance.GetComponent<EnemyChestController>();
        if (chest != null)
            chest.ConfigureDetection(DefaultVisionRange, DefaultLoseVisionRange);

        var beholder = instance.GetComponent<EnemyBeholderController>();
        if (beholder != null)
            beholder.ConfigureDetection(DefaultVisionRange, DefaultLoseVisionRange);

        var agent = instance.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.stoppingDistance = 0.85f;
            agent.acceleration = 8f;
            agent.angularSpeed = 160f;
            agent.speed = EnemyChaseSpeed;
        }

        instance.GetComponent<EnemyNavigation>()?.RefreshSpeeds();
    }

    private static bool PlaceEnemy(GameObject instance, Vector3 position)
    {
        if (!DungeonNavMeshSetup.TrySampleWalkable(ref position))
        {
            DungeonGroundSnap.TrySnapToGround(ref position);
            instance.transform.position = position;
            return false;
        }

        instance.transform.position = position;

        var agent = instance.GetComponent<NavMeshAgent>();
        if (agent == null)
            return true;

        var wasEnabled = agent.enabled;
        agent.enabled = true;
        agent.Warp(position);
        var placed = agent.isOnNavMesh;
        agent.enabled = wasEnabled;
        return placed;
    }

    private static void EnsureMinDistanceFromPlayer(ref Vector3 position)
    {
        var player = PlayerTarget.Transform;
        if (player == null)
            return;

        var flatPlayer = player.position;
        flatPlayer.y = position.y;
        var offset = position - flatPlayer;
        offset.y = 0f;

        if (offset.sqrMagnitude >= MinSpawnDistanceFromPlayer * MinSpawnDistanceFromPlayer)
            return;

        var direction = offset.sqrMagnitude > 0.04f
            ? offset.normalized
            : new Vector3(Mathf.Sign(position.x - flatPlayer.x + 0.01f), 0f, 0.2f).normalized;

        position = flatPlayer + direction * MinSpawnDistanceFromPlayer;

        if (position.z <= SouthernCorridorMaxSpawnZ)
            return;

        position.z = SouthernCorridorMaxSpawnZ;
        var lateral = position.x - flatPlayer.x;
        if (Mathf.Abs(lateral) < 5f)
            position.x = flatPlayer.x + Mathf.Sign(lateral + 0.01f) * 7f;
    }

    private void WarpWaveAgents()
    {
        foreach (var agent in waveRoot.GetComponentsInChildren<NavMeshAgent>(true))
        {
            if (agent == null || !agent.isActiveAndEnabled)
                continue;

            if (agent.isOnNavMesh)
                continue;

            if (NavMesh.SamplePosition(agent.transform.position, out var hit, 12f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }
    }

    private GameObject[] GetGuardianPrefabs(int sectionIndex)
    {
        return sectionIndex switch
        {
            0 => new[] { chestMonsterPrefab, watcherPrefab },
            1 => new[] { beholderPrefab, beholderPrefab },
            _ => new[] { beholderPrefab, watcherPrefab }
        };
    }

    private GameObject[] GetOptionalPool(int sectionIndex)
    {
        return new[] { watcherPrefab, chestMonsterPrefab, beholderPrefab };
    }

    private void HandleEnemyDefeated(Enemy enemy)
    {
        if (!isRunActive || isTransitioning || enemy == null)
            return;

        var waveMarker = enemy.GetComponent<DungeonWaveEnemy>();
        if (waveMarker != null && !waveMarker.IsGuardian && waveMarker.WaveSection == currentSection)
        {
            carryOverOptionalEnemies.Remove(enemy);
            optionalAliveInWave = CountOptionalAlive(currentSection);
            RefreshObjectiveDisplay();
            return;
        }

        var marker = enemy.GetComponent<DungeonRequiredEnemy>();
        if (marker == null || marker.SectionIndex != currentSection)
            return;

        activeRequiredEnemies.Remove(marker);
        optionalAliveInWave = CountOptionalAlive(currentSection);

        if (activeRequiredEnemies.Count == 0)
            CompleteCurrentSection();
        else
            RefreshObjectiveDisplay();
    }

    private void CompleteCurrentSection()
    {
        if (isTransitioning)
            return;

        isTransitioning = true;

        if (currentSection < sectionBlockerGroups.Count)
        {
            foreach (var blocker in sectionBlockerGroups[currentSection])
            {
                if (blocker != null)
                    blocker.SetActive(false);
            }
        }

        CollectCarryOverForNextSection();

        Debug.Log($"[Dungeon] Sección {currentSection + 1} completada. Opcionales que pasan: {carryOverOptionalEnemies.Count}");

        var message = Sections[currentSection].CompleteMessage;
        if (carryOverOptionalEnemies.Count > 0)
            message += $"\n{carryOverOptionalEnemies.Count} enemigo(s) restante(s) pasan a la siguiente oleada.";

        StartCoroutine(AdvanceSectionAfterDelay(Sections[currentSection].CompleteHeader, message));
    }

    private void CollectCarryOverForNextSection()
    {
        carryOverOptionalEnemies.Clear();

        foreach (var enemy in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
        {
            var waveMarker = enemy.GetComponent<DungeonWaveEnemy>();
            var health = enemy.GetComponentInChildren<EnemyHealth>();
            if (waveMarker == null || waveMarker.IsGuardian || waveMarker.WaveSection != currentSection)
                continue;
            if (health != null && health.IsDead())
                continue;

            carryOverOptionalEnemies.Add(enemy);
        }
    }

    private IEnumerator AdvanceSectionAfterDelay(string completeHeader, string completeMessage)
    {
        if (objectiveText != null)
        {
            var guardiansDefeated = totalRequiredInSection - activeRequiredEnemies.Count;
            var optionalDefeated = Mathf.Max(0, optionalCountAtWaveStart - optionalAliveInWave);
            SetSectionHud(
                Sections[currentSection],
                guardiansDefeated,
                totalRequiredInSection,
                optionalDefeated,
                optionalCountAtWaveStart,
                completeHeader,
                completeMessage);
        }

        yield return new WaitForSecondsRealtime(sectionCompleteDelay);

        var nextSection = currentSection + 1;
        if (nextSection < Sections.Length)
            ShowChapterScreen(nextSection);
        else
            UnlockExit();
    }

    private void UnlockExit()
    {
        canExit = true;
        winZone?.SetUnlocked(true);

        if (objectiveText != null)
        {
            objectiveText.text = $"{ExitObjectiveHeader}\n{ExitObjective}";
            objectiveText.color = new Color(0.95f, 0.88f, 0.62f, 1f);
        }
    }

    private void RefreshObjectiveDisplay()
    {
        if (currentSection >= Sections.Length)
            return;

        var section = Sections[currentSection];
        var guardiansDefeated = totalRequiredInSection - activeRequiredEnemies.Count;
        var optionalDefeated = Mathf.Max(0, optionalCountAtWaveStart - optionalAliveInWave);
        SetSectionHud(section, guardiansDefeated, totalRequiredInSection, optionalDefeated, optionalCountAtWaveStart);
    }

    private void SetSectionHud(SectionConfig section, int guardiansDefeated, int guardianTotal, int optionalDefeated,
        int optionalTotal, string overrideHeader = null, string overrideSubtitle = null)
    {
        if (objectiveText == null)
            return;

        var header = string.IsNullOrWhiteSpace(overrideHeader) ? section.HudHeader : overrideHeader;
        var subtitle = string.IsNullOrWhiteSpace(overrideSubtitle) ? section.HudSubtitle : overrideSubtitle;
        var progress =
            $"<size=22>Guardianes: {guardiansDefeated}/{guardianTotal}\nEcos del bosque: {optionalDefeated}/{optionalTotal}</size>";

        if (guardiansDefeated >= guardianTotal && !string.IsNullOrWhiteSpace(overrideSubtitle))
        {
            objectiveText.text = string.IsNullOrWhiteSpace(header)
                ? overrideSubtitle
                : $"{header}\n{overrideSubtitle}";
            objectiveText.color = new Color(0.82f, 0.9f, 0.78f, 1f);
            return;
        }

        objectiveText.text = $"{header}\n{subtitle}\n{progress}";
        objectiveText.color = guardiansDefeated >= guardianTotal
            ? new Color(0.82f, 0.9f, 0.78f, 1f)
            : new Color(0.9f, 0.86f, 0.95f, 1f);
    }

    private void BuildHud()
    {
        var canvas = gameplayUI != null ? gameplayUI.GetComponent<RectTransform>() : null;
        if (canvas == null)
        {
            Debug.LogWarning("[Dungeon] No se encontró GameplayUI: el HUD de objetivos no se creó.");
            return;
        }

        var objectiveRoot = new GameObject("ObjectiveText", typeof(RectTransform));
        objectiveRoot.transform.SetParent(canvas, false);
        objectiveRoot.transform.SetAsLastSibling();

        var rect = objectiveRoot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -16f);
        rect.sizeDelta = new Vector2(920f, 140f);

        objectiveText = objectiveRoot.AddComponent<TextMeshProUGUI>();
        objectiveText.alignment = TextAlignmentOptions.Center;
        objectiveText.fontSize = 26f;
        objectiveText.color = new Color(0.95f, 0.92f, 0.82f, 1f);
        ApplyHudFont(objectiveText);
    }

    private TextMeshProUGUI GetHudFontSource()
    {
        return gameplayUI != null ? gameplayUI.GetComponentInChildren<TextMeshProUGUI>(true) : null;
    }

    private void ApplyHudFont(TextMeshProUGUI target)
    {
        var source = GetHudFontSource();
        if (source == null || target == null)
            return;

        target.font = source.font;
        if (source.fontSharedMaterial != null)
            target.fontSharedMaterial = source.fontSharedMaterial;
    }

    private void SetObjectiveText(string objective, string progress, string narrative = null)
    {
        if (objectiveText == null)
            return;

        if (!string.IsNullOrWhiteSpace(narrative))
            objectiveText.text = $"{narrative}\n{objective}\n<size=22>{progress}</size>";
        else if (!string.IsNullOrWhiteSpace(progress))
            objectiveText.text = $"{objective}\n<size=22>{progress}</size>";
        else
            objectiveText.text = objective;

        objectiveText.color = new Color(0.9f, 0.86f, 0.95f, 1f);
    }

    private void ShowChapterScreen(int sectionIndex)
    {
        if (sectionIndex < 0 || sectionIndex >= Sections.Length)
            return;

        pendingSectionIndex = sectionIndex;
        PauseForChapterScreen(true);

        var screen = GetChapterScreenInstance();
        if (screen == null)
        {
            Debug.LogError("[Dungeon] No hay ChapterScreen asignado. Creá el prefab con Game > UI > Crear Prefab ChapterScreen.");
            BeginChapterSection();
            return;
        }

        var section = Sections[sectionIndex];
        screen.Show(section.ChapterTitle, section.ChapterBody, BeginChapterSection);
    }

    private ChapterScreenView GetChapterScreenInstance()
    {
        if (chapterScreen != null)
            return chapterScreen;

        if (chapterScreenInstance != null)
            return chapterScreenInstance;

        if (chapterScreenPrefab != null)
        {
            var canvas = gameplayUI != null ? gameplayUI.GetComponent<RectTransform>() : null;
            if (canvas == null)
                return null;

            chapterScreenInstance = Instantiate(chapterScreenPrefab, canvas);
            chapterScreen = chapterScreenInstance;
            return chapterScreenInstance;
        }

        chapterScreen = FindFirstObjectByType<ChapterScreenView>(FindObjectsInactive.Include);
        return chapterScreen;
    }

    private void EnsureChapterScreenPrefab()
    {
        if (chapterScreenPrefab != null)
            return;

#if UNITY_EDITOR
        chapterScreenPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<ChapterScreenView>(ChapterScreenPrefabPath);
#endif
    }

    private void BeginChapterSection()
    {
        PauseForChapterScreen(false);

        if (pendingSectionIndex < 0)
            return;

        var sectionIndex = pendingSectionIndex;
        pendingSectionIndex = -1;

        if (!isRunActive)
            isRunActive = true;

        BeginSection(sectionIndex);
    }

    private void PauseForChapterScreen(bool pause)
    {
        Time.timeScale = pause ? 0f : 1f;

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        if (pause)
            playerController?.DisableInput();
        else
            playerController?.EnableInput();
    }

    public void ShowWinScreen(System.Action onContinue)
    {
        PauseForChapterScreen(true);

        var screen = GetChapterScreenInstance();
        if (screen == null)
        {
            Debug.LogWarning("[Dungeon] No hay ChapterScreen para la victoria.");
            onContinue?.Invoke();
            return;
        }

        var body = $"{WinEpilogue}\n\n{WinMessage}";
        screen.Show(WinScreenTitle, body, () =>
        {
            PauseForChapterScreen(false);
            onContinue?.Invoke();
        }, WinScreenButton);
    }

    public string GetWinMessage() => WinMessage;
}
