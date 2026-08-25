using UnityEngine;
using Unity.AI.Navigation;

// CSVからマップを自動生成する基礎を学ぶためのクラス
public class MapGenerator : MonoBehaviour
{
    private const float PlayerSpawnHeight = 1.0f;

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
    public GameObject[] monitorWallPrefab;  // [26]モニター付き壁
    public GameObject[] dark;             // []未定
    public GameObject[] lantern;             //[25]ランタン

    public GameObject[] BearTrap;         // [31] トラばさみ
    public GameObject[] RollingRock;      // [35] 大岩
    public GameObject[] Ladder;           // [38] 梯子
    public GameObject[] MonitorDecoyPressurePlate; // [39] デコイ起動用感圧板

    public GameObject[] PressurePlate;    // [40-49] 感圧板
    public GameObject[] TwoPlayerDoor;    // [50-59] 連動ドア

    public GameObject[] Crystal;          // [60-62] 炎・氷・雷
    public GameObject[] CrystalGear;      // [63-65] 炎・氷・雷の歯車
    public GameObject[] pitfall;          // [70] 落とし穴
    public GameObject[] StoneTablet;      // [80-84] 石板
    public GameObject[] Goal;             //[90]

    [Header("マップ設定")]
    public float tileSize = 1f;         // 1マスのサイズ
    public float floorHeight = 3f;      // 1階層あたりの高さ（Y軸のオフセット）
    public Transform mapParent;         // 生成したブロックをまとめる親オブジェクト
    public NavMeshSurface surface;

    [Header("監視用地下マップ設定")]
    [SerializeField]
    private Vector3 monitorMazeOffset = new Vector3(100f, 0f, 0f);  // 監視用地下マップを元の地下マップからどれだけ離して生成するかを指定する座標オフセット

    private Transform monitorMazeParent;                            // 監視用地下マップで生成した床や壁をまとめる親オブジェクト

    private int playerSpawnCount = 0;
    private Vector3 playerSpawnPosition;

    void Start()
    {
        if (!TryApplySelectedStageData())
        {
            return;
        }

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

        // 地下のCSVを監視用として別座標にも生成する
        // 地下4層を監視用迷路としてコピー生成
        GenerateMonitorMazeCopy(0); // BF
        GenerateMonitorMazeCopy(1); // 1=3
        GenerateMonitorMazeCopy(2); // 2
        GenerateMonitorMazeCopy(3); // 1=3
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

    private bool TryApplySelectedStageData()
    {
        if (!StageSelectionContext.HasSelection)
        {
            return true;
        }

        TextAsset[] selectedStageData = StageCatalog.LoadMapFloorData(
            StageSelectionContext.SelectedStageResourcePath
        );

        
        if (selectedStageData.Length != 9)
        {
            Debug.LogError($"選択ステージのCSVは9層必要です。現在: {selectedStageData.Length}層");
            return false;
        }

        mapFloorData = selectedStageData;
        Debug.Log($"選択ステージを読み込みます: {StageSelectionContext.SelectedStageResourcePath}");
        return true;
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
                            case 1: // 21：壁
                                Instantiate(
                                    normalWallPrefab[0],
                                    spawnPos,
                                    Quaternion.identity,
                                    mapParent
                                );
                                continuousWallCount = 0;
                                break;

                            case 2: // 22：明かり付きの壁
                                Instantiate(
                                    lampWallPrefab[0],
                                    spawnPos,
                                    Quaternion.identity,
                                    mapParent
                                );
                                continuousWallCount = 0;
                                break;

                            case 3: // 23：扉
                                Instantiate(
                                    door[0],
                                    spawnPos,
                                    Quaternion.identity,
                                    mapParent
                                );
                                continuousWallCount = 0;
                                break;

                            case 4: // 地下壁
                                    Instantiate(B1normalWallPrefab[0], spawnPos, Quaternion.identity, mapParent);
                                    continuousWallCount = 0; // 壁が途切れたのでカウントリセット
                            break;

                            case 5: // ランタン
                                    Instantiate(lantern[0], spawnPos, Quaternion.identity, mapParent);
                            break;

                            case 6: // 26：モニター付き壁
                                Instantiate(
                                    monitorWallPrefab[0],
                                    spawnPos,
                                    Quaternion.identity,
                                    mapParent
                                );
                                continuousWallCount = 0;
                                break;

                            case 8: // 感圧板1枚で開く扉
                                int pressurePlateChannel = FindAdjacentPressurePlateChannel(rows, x, y);
                                if (pressurePlateChannel < 0)
                                {
                                    pressurePlateChannel = type;
                                    Debug.LogWarning($"CSV番号28の隣に感圧板がありません: ({x}, {y})");
                                }

                                InstantiateChannelDoor(floorIndex, spawnPos, pressurePlateChannel, 1);
                                continuousWallCount = 0;
                            break;

                        }
                        break;
                    case 3: // 単体ギミック
                        switch (type)
                        {
                            case 1: // 31：トラばさみ
                                Instantiate(
                                    BearTrap[0],
                                    spawnPos,
                                    Quaternion.identity,
                                    mapParent
                                );
                                break;

                            case 5: // 35：大岩
                                Instantiate(
                                    RollingRock[0],
                                    spawnPos,
                                    Quaternion.identity,
                                    mapParent
                                );
                                break;

                            case 8: // 38：梯子
                                InstantiateGimmickFloor(floorIndex, spawnPos);

                                Instantiate(
                                    Ladder[0],
                                    spawnPos,
                                    Quaternion.identity,
                                    mapParent
                                );
                                break;

                            case 9: // 39：デコイ起動用感圧板
                                InstantiateGimmickFloor(floorIndex, spawnPos);

                                Vector3 platePos = spawnPos + Vector3.up * 0.55f;

                                Instantiate(
                                    MonitorDecoyPressurePlate[0],
                                    platePos,
                                    Quaternion.identity,
                                    mapParent
                                );
                                break;
                        }
                        break;
                    case 4: // 40-49: 感圧板（一の位が連動チャンネル）
                        InstantiateChannelPressurePlate(floorIndex, spawnPos, type);
                        break;

                    case 5: // 50-59: 扉（一の位が連動チャンネル）
                        InstantiateChannelDoor(floorIndex, spawnPos, type);
                        break;

                    case 6: // 60～62：クリスタル、63～65：歯車

                        InstantiateGimmickFloor(floorIndex, spawnPos);

                        // 60～62：クリスタル
                        if (type <= 2)
                        {
                            if (Crystal != null &&
                                type < Crystal.Length &&
                                Crystal[type] != null)
                            {
                                Instantiate(
                                    Crystal[type],
                                    spawnPos,
                                    Quaternion.identity,
                                    mapParent
                                );
                            }
                            else
                            {
                                Debug.LogError(
                                    $"クリスタルPrefabが設定されていません。CSV番号: {key}, 配列番号: {type}"
                                );
                            }
                        }

                        // 63～65：クリスタル歯車
                        else if (type >= 3 && type <= 5)
                        {
                            int gearIndex = type - 3;

                            if (CrystalGear != null &&
                                gearIndex < CrystalGear.Length &&
                                CrystalGear[gearIndex] != null)
                            {
                                Instantiate(
                                    CrystalGear[gearIndex],
                                    spawnPos,
                                    Quaternion.identity,
                                    mapParent
                                );
                            }
                            else
                            {
                                Debug.LogError(
                                    $"クリスタル歯車Prefabが設定されていません。CSV番号: {key}, 配列番号: {gearIndex}"
                                );
                            }
                        }

                        break;
                    case 7: // 70：落とし穴

                        InstantiateGimmickFloor(floorIndex, spawnPos);

                        if (pitfall != null &&
                            pitfall.Length > 0 &&
                            pitfall[0] != null)
                        {
                            Instantiate(
                                pitfall[0],
                                spawnPos,
                                Quaternion.identity,
                                mapParent
                            );
                        }
                        else
                        {
                            Debug.LogError("落とし穴Prefabが設定されていません。");
                        }
                        break;

                    case 8: // 80～84：石板

                        InstantiateGimmickFloor(floorIndex, spawnPos);

                        if (StoneTablet != null &&
                            type < StoneTablet.Length &&
                            StoneTablet[type] != null)
                        {
                            Instantiate(
                                StoneTablet[type],
                                spawnPos,
                                Quaternion.identity,
                                mapParent
                            );
                        }
                        else
                        {
                            Debug.LogError($"石板Prefabが設定されていません。CSV番号: {key}");
                        }
                        break;
                }
            }
        }

        Debug.Log($"階層 {floorIndex} のマップ生成が完了しました！");
    }

    /// <summary>
    /// 地下マップを監視用として別の座標にコピー生成する関数
    /// </summary>
    private void GenerateMonitorMazeCopy(int floorIndex)
    {
        // 指定した階層のCSVデータが存在するか確認
        if (floorIndex < 0 ||
            floorIndex >= mapFloorData.Length ||
            mapFloorData[floorIndex] == null)
        {
            Debug.LogWarning($"監視用マップ：階層 {floorIndex} のデータがありません");
            return;
        }
        // 監視用地下マップをまとめる親オブジェクトを作成
        if (monitorMazeParent == null)
        {
            GameObject monitorRoot = new GameObject("MonitorMaze");
            monitorMazeParent = monitorRoot.transform;
        }
        // 指定した階層のCSVデータを文字列として読み込む
        string csvText = mapFloorData[floorIndex].text;

        // CSVを改行ごとに分けて、1行ずつ扱えるようにする
        string[] rows = csvText.Trim().Split('\n');

        // CSVの縦方向のマス数を取得
        int height = rows.Length;

        // CSVの1行目をカンマで分けて、横方向のマス数を取得
        int width = rows[0].Replace("\r", "").Split(',').Length;

        // CSVを上から1行ずつ読み込む
        for (int y = 0; y < height; y++)
        {
            // 1行分のデータをカンマで分割する
            string[] columns = rows[y].Replace("\r", "").Split(',');

            if (columns.Length != width)
            {
                Debug.LogError(
                    $"CSV列数不一致: {mapFloorData[floorIndex].name} " +
                    $"行={y + 1}, 正常列数={width}, 実際={columns.Length}"
                );
                return;
            }

            // 1行の中を左から1マスずつ読み込む
            for (int x = 0; x < width; x++)
            {
                // 現在のマスのデータを取得する
                string cell = columns[x].Trim();

                // 空欄の場合は何もせず次のマスへ進む
                if (string.IsNullOrEmpty(cell))
                {
                    continue;
                }

                // CSVの文字列を整数に変換する
                int key = int.Parse(cell);

                // 元の地下マップの座標を計算し、監視用マップの位置までずらす
                Vector3 spawnPos = new Vector3(
                    x * tileSize,
                    floorIndex * floorHeight,
                    y * tileSize
                ) + monitorMazeOffset;
                // CSVの数値によって、監視用マップに生成するオブジェクトを変更する
                switch (key)
                {
                    case 11: // 地下床
                        Instantiate(
                            floorB1[0],
                            spawnPos,
                            Quaternion.identity,
                            monitorMazeParent
                        );
                        break;

                    case 21: // 通常の壁
                        Instantiate(
                            normalWallPrefab[0],
                            spawnPos,
                            Quaternion.identity,
                            monitorMazeParent
                        );
                        break;

                    case 22: // ランプ付きの壁
                        Instantiate(
                            lampWallPrefab[0],
                            spawnPos,
                            Quaternion.identity,
                            monitorMazeParent
                        );
                        break;

                    case 24: // 地下用の壁
                        Instantiate(
                            B1normalWallPrefab[0],
                            spawnPos,
                            Quaternion.identity,
                            monitorMazeParent
                        );
                        break;

                    case 100: // プレイヤー開始地点
                              // 監視用マップではプレイヤーを生成せず、床だけ生成する
                        Instantiate(
                            floorB1[0],
                            spawnPos,
                            Quaternion.identity,
                            monitorMazeParent
                        );
                        break;
                }
            }
        }
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

    private void InstantiateChannelDoor(
        int floorIndex,
        Vector3 spawnPos,
        int channelId,
        int requiredPlateCount = 2
    )
    {
        InstantiateGimmickFloor(floorIndex, spawnPos);
        GameObject doorObject =
            Instantiate(
                TwoPlayerDoor[0],
                spawnPos,
                Quaternion.Euler(0f, 90f, 0f),
                mapParent
            ); TwoPlayerDoor linkedDoor = doorObject.GetComponentInChildren<TwoPlayerDoor>(true);

        if (linkedDoor == null)
        {
            Debug.LogError($"扉PrefabにTwoPlayerDoorがありません。CSVチャンネル: {channelId}");
            return;
        }

        linkedDoor.ConfigureChannel(
            channelId,
            GetGimmickChannelColor(channelId),
            requiredPlateCount
        );
    }

    private static int FindAdjacentPressurePlateChannel(string[] rows, int x, int y)
    {
        int[] offsetX = { -1, 1, 0, 0 };
        int[] offsetY = { 0, 0, -1, 1 };

        for (int i = 0; i < offsetX.Length; i++)
        {
            int targetY = y + offsetY[i];
            if (targetY < 0 || targetY >= rows.Length)
            {
                continue;
            }

            string[] targetColumns = rows[targetY].Replace("\r", "").Split(',');
            int targetX = x + offsetX[i];
            if (targetX < 0 || targetX >= targetColumns.Length)
            {
                continue;
            }

            if (int.TryParse(targetColumns[targetX].Trim(), out int key) && key >= 40 && key <= 49)
            {
                return key % 10;
            }
        }

        return -1;
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
