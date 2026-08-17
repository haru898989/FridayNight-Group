using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [System.Serializable]
    public class PlayerData
    {
        public int playerID;
        public bool useController;
        public string objectName;
        public PlayerRef playerRef;
        public NetworkObject playerObject;
    }

    [Header("Player Settings")]
    public PlayerData[] players = new PlayerData[2];
    [SerializeField] private NetworkPrefabRef playerPrefabA;
    [SerializeField] private NetworkPrefabRef playerPrefabB;
    [SerializeField] private float playerSpawnSpacing = 1.25f;

    [Header("Fallback Spawn Point")]
    [SerializeField] private Transform spawnPoint;

    public Vector3 currentSpawnPosition;

    private NetworkRunner runner;
    private bool isMapSpawnReady;
    private bool isLocalPlayerSpawnPending;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializePlayerSlots();

        if (spawnPoint != null)
        {
            currentSpawnPosition = spawnPoint.position;
        }

        Debug.Log("GameManagerをOnlineConnectから引き継ぎます");
    }

    private void LateUpdate()
    {
        RefreshPlayerObjectReferences();
    }

    public void SetRunner(NetworkRunner networkRunner)
    {
        if (networkRunner == null)
        {
            return;
        }

        runner = networkRunner;
        RebuildPlayerSlots();
        TrySpawnLocalPlayer();
    }

    public void OnPlayerJoined(NetworkRunner networkRunner, PlayerRef player)
    {
        runner = networkRunner;
        RebuildPlayerSlots();

        int index = FindPlayerIndex(player);
        if (index < 0)
        {
            Debug.LogWarning($"参加者 {player.PlayerId} は2人の上限を超えたため、プレイヤーを生成しません");
            return;
        }

        Debug.Log($"参加者を登録しました: Player={player.PlayerId}, Slot={index + 1}");
        TrySpawnLocalPlayer();
    }

    public void OnPlayerLeft(NetworkRunner networkRunner, PlayerRef player)
    {
        runner = networkRunner;

        int index = FindPlayerIndex(player);
        if (index >= 0)
        {
            NetworkObject playerObject = players[index].playerObject;
            if (playerObject != null && playerObject.HasStateAuthority)
            {
                networkRunner.Despawn(playerObject);
            }
        }

        RebuildPlayerSlots();
        Debug.Log($"退出した参加者を解除しました: Player={player.PlayerId}");
    }

    /// <summary>
    /// MapGeneratorが全マップを生成した後に一度だけ呼び出します。
    /// </summary>
    public void SetMapSpawnPosition(Vector3 newPosition)
    {
        currentSpawnPosition = newPosition;
        isMapSpawnReady = true;

        Debug.Log($"Mapのプレイヤー生成位置を確定しました: {currentSpawnPosition}");
        RebuildPlayerSlots();
        TrySpawnLocalPlayer();
        TeleportOwnedPlayerToMapSpawn();
    }

    public void SetMapNotReady()
    {
        isMapSpawnReady = false;
    }

    /// <summary>
    /// 既存コードとの互換用です。新規処理ではSetMapSpawnPositionを使用します。
    /// </summary>
    public void UpdateSpawnPosition(Vector3 newPosition)
    {
        SetMapSpawnPosition(newPosition);
    }

    public PlayerData GetPlayerData(int index)
    {
        if (index < 0 || index >= players.Length)
        {
            return null;
        }

        return players[index];
    }

    private async void TrySpawnLocalPlayer()
    {
        if (!isMapSpawnReady || runner == null || !runner.IsRunning || isLocalPlayerSpawnPending)
        {
            return;
        }

        PlayerRef localPlayer = runner.LocalPlayer;
        int index = FindPlayerIndex(localPlayer);
        if (index < 0)
        {
            return;
        }

        if (runner.TryGetPlayerObject(localPlayer, out NetworkObject existingObject) && existingObject != null)
        {
            players[index].playerObject = existingObject;
            return;
        }

        NetworkPrefabRef prefab = index == 0 ? playerPrefabA : playerPrefabB;
        Vector3 spawnPosition = GetSpawnPosition(index);

        isLocalPlayerSpawnPending = true;
        NetworkObject playerObject = null;

        try
        {
            // NetworkPrefabRefはシーン遷移直後にまだロードされていない場合があるため、
            // Fusionの非同期Spawnを使い、Prefabのロード完了を待ってから生成する。
            playerObject = await runner.SpawnAsync(
                prefab,
                spawnPosition,
                Quaternion.identity,
                localPlayer,
                (spawnRunner, spawnedObject) =>
                {
                    spawnedObject.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
                },
                NetworkSpawnFlags.SharedModeStateAuthLocalPlayer
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            isLocalPlayerSpawnPending = false;
        }

        if (playerObject == null)
        {
            Debug.LogError($"Player {localPlayer.PlayerId} の生成に失敗しました");
            return;
        }

        runner.SetPlayerObject(localPlayer, playerObject);
        players[index].playerObject = playerObject;
        ApplySpawnTransform(playerObject, spawnPosition);

        PlayerBase playerBase = playerObject.GetComponent<PlayerBase>();
        if (playerBase != null)
        {
            playerBase.SetPlayerDevice(players[index].playerID, players[index].useController);
        }

        Debug.Log($"Mapに{players[index].objectName}プレイヤーを生成しました: Requested={spawnPosition}, Actual={playerObject.transform.position}");
    }

    private void TeleportOwnedPlayerToMapSpawn()
    {
        if (runner == null || !runner.IsRunning)
        {
            return;
        }

        PlayerRef localPlayer = runner.LocalPlayer;
        int index = FindPlayerIndex(localPlayer);
        if (index < 0 || !runner.TryGetPlayerObject(localPlayer, out NetworkObject playerObject) || playerObject == null)
        {
            return;
        }

        if (!playerObject.HasStateAuthority)
        {
            return;
        }

        PlayerBase playerBase = playerObject.GetComponent<PlayerBase>();
        if (playerBase != null)
        {
            playerBase.ResetGoalSpectatorMode();
        }

        ApplySpawnTransform(playerObject, GetSpawnPosition(index));
    }

    private static void ApplySpawnTransform(NetworkObject playerObject, Vector3 targetPosition)
    {
        if (playerObject == null || !playerObject.HasStateAuthority)
        {
            return;
        }

        CharacterController characterController = playerObject.GetComponent<CharacterController>();
        bool wasControllerEnabled = characterController != null && characterController.enabled;

        if (wasControllerEnabled)
        {
            characterController.enabled = false;
        }

        NetworkTransform networkTransform = playerObject.GetComponent<NetworkTransform>();
        if (networkTransform != null)
        {
            networkTransform.Teleport(targetPosition, Quaternion.identity);
        }

        // Teleport直後の同一フレームでもHierarchy上のTransformが正しい値になるよう、
        // Transformにも明示的に反映する。
        playerObject.transform.SetPositionAndRotation(targetPosition, Quaternion.identity);

        if (wasControllerEnabled)
        {
            characterController.enabled = true;
        }

        Physics.SyncTransforms();
    }

    private Vector3 GetSpawnPosition(int index)
    {
        return currentSpawnPosition + Vector3.right * (playerSpawnSpacing * index);
    }

    private void RebuildPlayerSlots()
    {
        if (runner == null || !runner.IsRunning)
        {
            return;
        }

        Dictionary<PlayerRef, NetworkObject> knownObjects = new Dictionary<PlayerRef, NetworkObject>();
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].playerRef != PlayerRef.None && players[i].playerObject != null)
            {
                knownObjects[players[i].playerRef] = players[i].playerObject;
            }
        }

        List<PlayerRef> activePlayers = new List<PlayerRef>();
        foreach (PlayerRef activePlayer in runner.ActivePlayers)
        {
            activePlayers.Add(activePlayer);
        }

        activePlayers.Sort((left, right) => left.PlayerId.CompareTo(right.PlayerId));
        InitializePlayerSlots();

        int count = Mathf.Min(players.Length, activePlayers.Count);
        for (int i = 0; i < count; i++)
        {
            PlayerRef player = activePlayers[i];
            players[i].playerID = i + 1;
            players[i].playerRef = player;
            players[i].useController = i == 0;
            players[i].objectName = i == 0 ? "A" : "B";

            if (runner.TryGetPlayerObject(player, out NetworkObject registeredObject) && registeredObject != null)
            {
                players[i].playerObject = registeredObject;
            }
            else if (knownObjects.TryGetValue(player, out NetworkObject knownObject))
            {
                players[i].playerObject = knownObject;
            }
        }
    }

    private void RefreshPlayerObjectReferences()
    {
        if (runner == null || !runner.IsRunning)
        {
            return;
        }

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null || players[i].playerRef == PlayerRef.None)
            {
                continue;
            }

            if (runner.TryGetPlayerObject(players[i].playerRef, out NetworkObject playerObject))
            {
                players[i].playerObject = playerObject;
            }
        }
    }

    private int FindPlayerIndex(PlayerRef player)
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].playerRef == player)
            {
                return i;
            }
        }

        return -1;
    }

    private void InitializePlayerSlots()
    {
        if (players == null || players.Length != 2)
        {
            players = new PlayerData[2];
        }

        for (int i = 0; i < players.Length; i++)
        {
            players[i] = new PlayerData
            {
                playerRef = PlayerRef.None,
                objectName = string.Empty
            };
        }
    }
}
