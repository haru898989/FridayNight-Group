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
    public GameObject[] PressurePlate;    //[34]
    public GameObject[] RollingRock;      //[35]
    public GameObject[] StoneTablet;      //[36]

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

                            case 4: //
                                Instantiate(PressurePlate[0], spawnPos, Quaternion.identity, mapParent);
                            break;

                            case 5: //
                                Instantiate(RollingRock[0], spawnPos, Quaternion.identity, mapParent);
                            break;

                            case 6: //
                                Instantiate(StoneTablet[0], spawnPos, Quaternion.identity, mapParent);
                            break;
                        }
                        break;

                }
            }
        }

        Debug.Log($"階層 {floorIndex} のマップ生成が完了しました！");
    }
}
