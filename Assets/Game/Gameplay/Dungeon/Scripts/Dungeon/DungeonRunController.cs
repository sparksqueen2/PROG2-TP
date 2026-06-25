using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class DungeonRunController : MonoBehaviour
{
    private struct SectionConfig
    {
        public string Narrative;
        public string Objective;
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
            Narrative = string.Empty,
            Objective = "Derrotá a los guardianes corruptos",
            CompleteMessage = "El camino está abierto",
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
            Narrative = "La cámara central aún respira magia rota.",
            Objective = "Cámara: eliminá a los 2 guardianes.",
            CompleteMessage = "El camino al núcleo queda abierto.",
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
            Narrative = "El guardián custodia la grieta. La Guardia no quiso que llegues.",
            Objective = "Grieta: derrotá a los 2 guardianes finales.",
            CompleteMessage = "La grieta se estabiliza. Buscá el núcleo al norte.",
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
    private const string IntroTitle = "Kael y el Juramento de Hierro";
    private const string IntroChapter = "CAPÍTULO I\nEl Umbral Corrupto";
    private const string IntroBody =
        "Desterrado por portar un arma rúnica prohibida,\n" +
        "Kael debe limpiar la corrupción para abrir el camino.";
    private const string ThresholdSealedHeader = "UMBRAL SELLADO";
    private const string ThresholdPurifiedHeader = "UMBRAL PURIFICADO";
    private const string ExitObjective = "Entrá al núcleo (cilindro verde al norte).";
    private const string WinMessage =
        "Cerraste la grieta.\nLas armas no eran malditas.\nLa Guardia mintió.";

    [SerializeField] private float blockerSpanZ = 40f;
    [SerializeField] private float blockerSpanX = 55f;
    [SerializeField] private float blockerCenterZ = -20f;
    [SerializeField] private float blockerCenterX = 22f;
    [SerializeField] private float blockerHeight = 5f;
    [SerializeField] private float sectionCompleteDelay = 2.5f;
    [SerializeField] private GameObject chestMonsterPrefab;
    [SerializeField] private GameObject watcherPrefab;
    [SerializeField] private GameObject beholderPrefab;

    private readonly List<DungeonRequiredEnemy> activeRequiredEnemies = new List<DungeonRequiredEnemy>();
    private readonly List<List<GameObject>> sectionBlockerGroups = new List<List<GameObject>>();
    private readonly List<Enemy> carryOverOptionalEnemies = new List<Enemy>();

    private Transform progressionRoot;
    private Transform waveRoot;
    private WinZone winZone;
    private GameplayUI gameplayUI;
    private TextMeshProUGUI objectiveText;
    private GameObject introPanel;
    private int currentSection;
    private int totalRequiredInSection;
    private int optionalAliveInWave;
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
    }

    public void Init(WinZone zone, GameplayUI ui, Transform player)
    {
        winZone = zone;
        gameplayUI = ui;
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

        ShowIntro();
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
        BeginSection(0);
        isRunActive = true;
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

                if (section.BlockerNorthZ > 0f)
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

        StartCoroutine(AdvanceSectionAfterDelay(message));
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

    private IEnumerator AdvanceSectionAfterDelay(string message)
    {
        if (objectiveText != null)
        {
            if (currentSection == 0)
                SetThresholdHud(ThresholdPurifiedHeader, Sections[0].CompleteMessage, totalRequiredInSection, totalRequiredInSection);
            else
                objectiveText.text = message;
        }

        yield return new WaitForSeconds(sectionCompleteDelay);
        BeginSection(currentSection + 1);
    }

    private void UnlockExit()
    {
        canExit = true;
        winZone?.SetUnlocked(true);
        SetObjectiveText(ExitObjective, string.Empty);
    }

    private void RefreshObjectiveDisplay()
    {
        if (currentSection >= Sections.Length)
            return;

        var section = Sections[currentSection];
        var defeated = totalRequiredInSection - activeRequiredEnemies.Count;

        if (currentSection == 0)
        {
            SetThresholdHud(ThresholdSealedHeader, section.Objective, defeated, totalRequiredInSection);
            return;
        }

        var progress = $"Guardianes: {defeated}/{totalRequiredInSection}";
        SetObjectiveText(section.Objective, progress, section.Narrative);
    }

    private void SetThresholdHud(string header, string subtitle, int defeated, int total)
    {
        if (objectiveText == null)
            return;

        objectiveText.text = $"{header}\n{subtitle}\n<size=22>Guardianes: {defeated}/{total}</size>";
        objectiveText.color = defeated >= total
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
        rect.sizeDelta = new Vector2(920f, 110f);

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

    private void ShowIntro()
    {
        var canvas = gameplayUI != null ? gameplayUI.GetComponent<RectTransform>() : null;
        if (canvas == null)
            return;

        introPanel = new GameObject("IntroPanel", typeof(RectTransform), typeof(Image));
        introPanel.transform.SetParent(canvas, false);

        var background = introPanel.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.82f);

        var panelRect = introPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        CreateIntroText(introPanel.transform, IntroTitle, 24f, new Vector2(0f, 190f), FontStyles.Italic,
            new Color(0.72f, 0.7f, 0.78f, 1f));
        CreateIntroText(introPanel.transform, IntroChapter, 38f, new Vector2(0f, 70f), FontStyles.Bold,
            new Color(0.95f, 0.9f, 0.82f, 1f));
        CreateIntroText(introPanel.transform, IntroBody, 24f, new Vector2(0f, -40f), FontStyles.Normal,
            new Color(0.86f, 0.84f, 0.9f, 1f));

        var buttonRoot = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonRoot.transform.SetParent(introPanel.transform, false);

        var buttonRect = buttonRoot.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = new Vector2(0f, -190f);
        buttonRect.sizeDelta = new Vector2(300f, 58f);

        buttonRoot.GetComponent<Image>().color = new Color(0.1f, 0.09f, 0.14f, 0.88f);

        var buttonLabel = CreateIntroText(buttonRoot.transform, "COMENZAR", 22f, Vector2.zero, FontStyles.Bold,
            new Color(0.88f, 0.82f, 0.95f, 1f));
        buttonLabel.raycastTarget = false;

        buttonRoot.GetComponent<Button>().onClick.AddListener(CloseIntro);
        RefreshObjectiveDisplay();
    }

    private TextMeshProUGUI CreateIntroText(Transform parent, string text, float fontSize, Vector2 position, FontStyles style,
        Color color)
    {
        var textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(900f, 220f);

        var label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        ApplyHudFont(label);
        return label;
    }

    private void CloseIntro()
    {
        if (introPanel != null)
            Destroy(introPanel);

        introPanel = null;
    }

    public string GetWinMessage() => WinMessage;
}
