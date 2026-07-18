using UnityEngine;
using Unity.AI.Navigation;

// CSVからマップを自動生成する基礎を学ぶためのクラス
public class MapGenerator : MonoBehaviour
{
    private const float PlayerSpawnHeight = 1.5f;

    [Header("マップデータ（階層ごとにセット）")]
    // 複数の「シート（階層）」を表現するために、配列でCSVを持たせます
    // [0]地下 [1]1階 [2]2階 のようにInspectorから設定します
    public TextAsset[] mapFloorData;

    [Header("生成するブロックのプレハブ")]
    public GameObject[] floorB1;          //[11]地下床 
    public GameObject[] floor1;           //[12]床
    //public GameObject[] pitfall;          //[13]落とし穴

    public GameObject[] normalWallPrefab; // [21]壁
    public GameObject[] lampWallPrefab;   // [22]ランプ付きの壁
    public GameObject[] door;             // [23]扉
    public GameObject[] B1normalWallPrefab; //[24]
    public GameObject[] dark;             // []未定

    public GameObject[] BearTrap;         //[31]
    public GameObject[] Crystal;          //[32]
    public GameObject[] pitfall;          //[33]
    public GameObject[] PressurePlate;    //[40-49] 一の位が連動チャンネル
    public GameObject[] RollingRock;      //[35]
    public GameObject[] StoneTablet;      //[36]
    public GameObject[] TwoPlayerDoor;    //[50-59] 一の位が連動チャンネル
    public GameObject[] Ladder;           //[38]

    public GameObject[] Goal;             //[90]

    [Header("マップ設定")]
    public float tileSize = 1f;         // 1マスのサイズ
    public float floorHeight = 3f;      // 1階層あたりの高さ（Y軸のオフセット）
    public Transform mapParent;         // 生成したブロックをまとめる親オブジェクト
    public NavMeshSurface surface;

    private int playerSpawnCount = 0;
    private Vector3 playerSpawnPosition;

    void Start()
    {
        // テストとして、ゲーム開始時に「1階（配列の1番目）」と「2階（2番目）」を生成してみる
        GenerateFloorMap(0);
        GenerateFloorMap(1);
        GenerateFloorMap(2);
        GenerateFloorMap(3);
        GenerateFloorMap(4);
        GenerateFloorMap(5);
        GenerateFloorMap(6);
        GenerateFloorMap(7);
        GenerateFloorMap(8);

        surface.BuildNavMesh();

        // CSVの100で指定された位置をGameManagerへ渡す
        if (playerSpawnCount == 1)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetMapSpawnPosition(playerSpawnPosition);
            }
            else
            {
                Debug.LogError("GameManagerが見つかりません！");
            }
        }
        else
        {
            Debug.LogError($"プレイヤー生成位置100は全CSVに1個必要です。現在: {playerSpawnCount}個");
        }
    }

    /// <summary>
    /// 指定した階層のマップを生成する関数
    /// </summary>
    /// <param name="floorIndex">生成したい階層のインデックス（0=地下, 1=1階...）</param>
    public void GenerateFloorMap(int floorIndex)
    {
        // 階層のデータが存在するかチェック
        if (floorIndex < 0 || floorIndex >= mapFloorData.Length || mapFloorData[floorIndex] == null)
        {
            Debug.LogWarning($"階層 {floorIndex} のデータがありません！");
            return;
        }

        // CSVデータをテキストとして読み込み、改行('\n')で行ごとに分割
        string csvText = mapFloorData[floorIndex].text;
        string[] rows = csvText.Trim().Split('\n');

        int height = rows.Length;
        int width = rows[0].Trim().Split(',').Length;

        

        // Y軸（行）のループ
        for (int y = 0; y < height; y++)
        {
            // カンマ(',')で区切って1マスずつのデータ(列)に分割
            string[] columns = rows[y].Replace("\r", "").Split(',');
            //int width = columns.Length;

            // ★重要ポイント：連続した壁をカウントするための変数（新しい行に行くたびにリセット）
            int continuousWallCount = 0;

            // X軸（列）のループ
            for (int x = 0; x < width; x++)
            {
                // 文字列を整数に変換
                string cell = columns[x].Trim();

                if (string.IsNullOrEmpty(cell))
                {
                    continue;
                }

                int key = int.Parse(cell);


                // 生成する位置を計算
                // floorIndex * floorHeight で、階層ごとにマップのY座標（高さ）を変えます
                Vector3 spawnPos = new Vector3(x * tileSize, floorIndex * floorHeight, y * tileSize);

                // 100はPlayerの生成位置。足元には通常床を生成する
                if (key == 100)
                {
                    Instantiate(floor1[0], spawnPos, Quaternion.identity, mapParent);
                    playerSpawnCount++;

                    if (playerSpawnCount == 1)
                    {
                        playerSpawnPosition = spawnPos + Vector3.up * PlayerSpawnHeight;
                    }

                    continue;
                }

                // 90はゴール
                if (key == 90)
                {
                    Instantiate(Goal[0], spawnPos, Quaternion.identity, mapParent);
                    continue;
                }

                // 読み込んだ数値（key）によって生成するブロックを変える


                int team = key / 10;
                int type = key % 10;

                switch (team)
                {
                    case 1: // 床チーム
                        switch (type)
                        {
                            case 1: // B1の床
                                Instantiate(floorB1[0], spawnPos, Quaternion.identity, mapParent);
                                continuousWallCount = 0; // 壁が途切れたのでカウントリセット
                                break;

                            case 2: //1Fの床
                                Instantiate(floor1[0], spawnPos, Quaternion.identity, mapParent);
                                continuousWallCount = 0; // 壁が途切れたのでカウントリセット
                                break;
    
                            case 3: // 落とし穴
                                Instantiate(pitfall[0], spawnPos, Quaternion.identity, mapParent);
                                continuousWallCount = 0; // 壁が途切れたのでカウントリセット
                                break;
                        }
                    break;

                    case 2: // 壁チーム
                        switch (type)
                        {
                            case 1: // 壁
                                    Instantiate(normalWallPrefab[0], spawnPos, Quaternion.identity, mapParent);
                                    continuousWallCount = 0; // 壁が途切れたのでカウントリセット
                            break;

                            case 2: // 明かり付きの壁
                                    Instantiate(lampWallPrefab[0], spawnPos, Quaternion.identity, mapParent);
                                    continuousWallCount = 0; // 壁が途切れたのでカウントリセット
                            break;

                            case 3: // 扉
                                    Instantiate(door[0], spawnPos, Quaternion.identity, mapParent);
                                    continuousWallCount = 0; // 壁が途切れたのでカウントリセット
                            break;

                            case 4: // 扉
                                    Instantiate(B1normalWallPrefab[0], spawnPos, Quaternion.identity, mapParent);
                                    continuousWallCount = 0; // 壁が途切れたのでカウントリセット
                            break;
                        }
                        break;

                    case 3: //ギミック
                        switch (type)
                        {
                            case 1: //
                                Instantiate(BearTrap[0], spawnPos, Quaternion.identity, mapParent);
                            break;

                            case 2: //
                                Instantiate(Crystal[0], spawnPos, Quaternion.identity, mapParent);
                            break;

                            case 3: //
                                Instantiate(pitfall[0], spawnPos, Quaternion.identity, mapParent);
                            break;

                            case 5: //
                                Instantiate(RollingRock[0], spawnPos, Quaternion.identity, mapParent);
                            break;

                            case 6: //
                                Instantiate(StoneTablet[0], spawnPos, Quaternion.identity, mapParent);
                            break;

                            case 8: // 地下から上階へ戻る梯子
                                InstantiateGimmickFloor(floorIndex, spawnPos);
                                Instantiate(Ladder[0], spawnPos, Quaternion.identity, mapParent);
                            break;
                        }
                        break;

                    case 4: // 40-49: 感圧板（一の位が連動チャンネル）
                        InstantiateChannelPressurePlate(floorIndex, spawnPos, type);
                        break;

                    case 5: // 50-59: 扉（一の位が連動チャンネル）
                        InstantiateChannelDoor(floorIndex, spawnPos, type);
                        break;

                }
            }
        }

        Debug.Log($"階層 {floorIndex} のマップ生成が完了しました！");
    }

    /// <summary>
    /// CSVでギミックを置いたセルにも歩行用の床を生成する関数
    /// </summary>
    private void InstantiateGimmickFloor(int floorIndex, Vector3 spawnPos)
    {
        GameObject[] floorPrefabs = floorIndex < 4 ? floorB1 : floor1;

        if (floorPrefabs == null || floorPrefabs.Length == 0 || floorPrefabs[0] == null)
        {
            Debug.LogWarning($"階層 {floorIndex} のギミック用床Prefabが設定されていません");
            return;
        }

        Instantiate(floorPrefabs[0], spawnPos, Quaternion.identity, mapParent);
    }

    private void InstantiateChannelPressurePlate(int floorIndex, Vector3 spawnPos, int channelId)
    {
        InstantiateGimmickFloor(floorIndex, spawnPos);
        GameObject plateObject = Instantiate(PressurePlate[0], spawnPos, Quaternion.identity, mapParent);
        PressurePlate plate = plateObject.GetComponentInChildren<PressurePlate>(true);

        if (plate == null)
        {
            Debug.LogError($"感圧板PrefabにPressurePlateがありません。CSVチャンネル: {channelId}");
            return;
        }

        plate.ConfigureChannel(channelId, GetGimmickChannelColor(channelId));
    }

    private void InstantiateChannelDoor(int floorIndex, Vector3 spawnPos, int channelId)
    {
        InstantiateGimmickFloor(floorIndex, spawnPos);
        GameObject doorObject = Instantiate(TwoPlayerDoor[0], spawnPos, Quaternion.identity, mapParent);
        TwoPlayerDoor linkedDoor = doorObject.GetComponentInChildren<TwoPlayerDoor>(true);

        if (linkedDoor == null)
        {
            Debug.LogError($"扉PrefabにTwoPlayerDoorがありません。CSVチャンネル: {channelId}");
            return;
        }

        linkedDoor.ConfigureChannel(channelId, GetGimmickChannelColor(channelId));
    }

    private static Color GetGimmickChannelColor(int channelId)
    {
        switch (channelId)
        {
            case 0: return new Color(0.55f, 0.55f, 0.55f); // グレー
            case 1: return new Color(0.90f, 0.20f, 0.20f); // 赤
            case 2: return new Color(0.20f, 0.45f, 0.95f); // 青
            case 3: return new Color(0.20f, 0.80f, 0.35f); // 緑
            case 4: return new Color(0.65f, 0.25f, 0.90f); // 紫
            case 5: return new Color(1.00f, 0.50f, 0.10f); // オレンジ
            case 6: return new Color(0.10f, 0.80f, 0.90f); // 水色
            case 7: return new Color(0.95f, 0.80f, 0.10f); // 黄
            case 8: return new Color(1.00f, 0.30f, 0.65f); // ピンク
            case 9: return new Color(0.85f, 0.85f, 0.85f); // 白
            default: return Color.white;
        }
    }
}
