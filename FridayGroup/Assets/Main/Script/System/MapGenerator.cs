using UnityEngine;

// CSVからマップを自動生成する基礎を学ぶためのクラス
public class MapGenerator : MonoBehaviour
{
    [Header("マップデータ（階層ごとにセット）")]
    // 複数の「シート（階層）」を表現するために、配列でCSVを持たせます
    // [0]地下 [1]1階 [2]2階 のようにInspectorから設定します
    public TextAsset[] mapFloorData;

    [Header("生成するブロックのプレハブ")]
    public GameObject floorB1;          // [1] 地下の床
    public GameObject floor1;          // [11]床
    public GameObject floorR;          // [21]天井
    public GameObject normalWallPrefab; // [10] 通常の壁
    public GameObject lampWallPrefab;   // [10] ランプ付きの壁（3個おきに生成）
    public GameObject pitfall;          // [31]落とし穴
    public GameObject door;             // [13]扉
    public GameObject dark;             // [12]暗闇

    [Header("マップ設定")]
    public float tileSize = 1f;         // 1マスのサイズ
    public float floorHeight = 3f;      // 1階層あたりの高さ（Y軸のオフセット）
    public Transform mapParent;         // 生成したブロックをまとめる親オブジェクト

    void Start()
    {
        // テストとして、ゲーム開始時に「1階（配列の1番目）」と「2階（2番目）」を生成してみる
        GenerateFloorMap(0);
        GenerateFloorMap(1);
        GenerateFloorMap(2);
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

        // Y軸（行）のループ
        for (int y = 0; y < height; y++)
        {
            // カンマ(',')で区切って1マスずつのデータ(列)に分割
            string[] columns = rows[y].Trim().Split(',');
            int width = columns.Length;

            // ★重要ポイント：連続した壁をカウントするための変数（新しい行に行くたびにリセット）
            int continuousWallCount = 0;

            // X軸（列）のループ
            for (int x = 0; x < width; x++)
            {
                // 文字列を整数に変換
                int key = int.Parse(columns[x]);

                // 生成する位置を計算
                // floorIndex * floorHeight で、階層ごとにマップのY座標（高さ）を変えます
                Vector3 spawnPos = new Vector3(x * tileSize, floorIndex * floorHeight, y * tileSize);

                // 読み込んだ数値（key）によって生成するブロックを変える
                switch (key)
                {
                    case 1: // B1の床
                        Instantiate(floorB1, spawnPos, Quaternion.identity, mapParent);
                        continuousWallCount = 0; // 壁が途切れたのでカウントリセット
                        break;

                    case 11: //1Fの床
                        Instantiate(floor1, spawnPos, Quaternion.identity, mapParent);
                        continuousWallCount = 0; // 壁が途切れたのでカウントリセット
                        break;

                    case 21: // 天井
                        Instantiate(floorR, spawnPos, Quaternion.identity, mapParent);
                        continuousWallCount = 0; // 壁が途切れたのでカウントリセット
                        break;
                    
                    case 31: // 落とし穴
                        Instantiate(pitfall, spawnPos, Quaternion.identity, mapParent);
                        continuousWallCount = 0; // 壁が途切れたのでカウントリセット
                        break;

                    case 10: // 壁ブロックの場合
                        continuousWallCount++; // 壁が連続しているのでカウントアップ！

                        // 「3個おき」の判定：カウントが3で割り切れる時だけランプ付きにする
                        if (continuousWallCount % 3 == 0)
                        {
                            Instantiate(lampWallPrefab, spawnPos, Quaternion.identity, mapParent);
                        }
                        else
                        {
                            Instantiate(normalWallPrefab, spawnPos, Quaternion.identity, mapParent);
                        }
                        break;
                    
                    case 12: // 暗闇
                        Instantiate(dark, spawnPos, Quaternion.identity, mapParent);
                        continuousWallCount = 0; // 壁が途切れたのでカウントリセット
                        break;
                    
                    case 13: // 扉
                        Instantiate(door, spawnPos, Quaternion.identity, mapParent);
                        continuousWallCount = 0; // 壁が途切れたのでカウントリセット
                        break;

                    default: // 設定されていない数値（空欄など）の場合
                        continuousWallCount = 0; // 何もない空間なのでカウントリセット
                        break;
                }
            }
        }

        Debug.Log($"階層 {floorIndex} のマップ生成が完了しました！");
    }
}