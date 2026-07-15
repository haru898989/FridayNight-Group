using Fusion;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }


    [System.Serializable]
    public class PlayerData
    {
        public int playerID;          // 1P,2P
        public bool useController;    // Controllerならtrue
        public string objectName;     // A/B
        public PlayerRef playerRef;   // Fusion Player
    }


    // 2人分保存
    public PlayerData[] players = new PlayerData[2];


    [SerializeField] private NetworkPrefabRef playerPrefabA;
    [SerializeField] private NetworkPrefabRef playerPrefabB;
    [SerializeField] private Transform spawnPoint;



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }


        Instance = this;

        // シーン移動しても保持
        DontDestroyOnLoad(gameObject);


        players[0] = new PlayerData();
        players[1] = new PlayerData();


        Debug.Log("GameManagerが残ります");
    }



    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
            return;


        int index = GetPlayerIndex();


        if (index == -1)
        {
            Debug.LogError("プレイヤー上限です");
            return;
        }



        // 基本情報保存
        players[index].playerID = index + 1;
        players[index].playerRef = player;



        if (index == 0)
        {
            // 1P
            players[index].useController = true;
            players[index].objectName = "A";
        }
        else
        {
            // 2P
            players[index].useController = false;
            players[index].objectName = "B";
        }



        // プレイヤー生成
        NetworkPrefabRef prefab =
            index == 0 ? playerPrefabA : playerPrefabB;


        runner.Spawn(
            prefab,
            spawnPoint.position,
            Quaternion.identity,
            player
        );



        Debug.Log(
            $"{players[index].playerID}P : " +
            $"Controller={players[index].useController} " +
            $"Object={players[index].objectName}"
        );
    }



    private int GetPlayerIndex()
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].playerRef == default)
            {
                return i;
            }
        }

        return -1;
    }



    public PlayerData GetPlayerData(int index)
    {
        return players[index];
    }
}