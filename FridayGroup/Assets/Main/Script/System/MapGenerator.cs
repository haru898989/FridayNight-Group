using UnityEngine;

// CSVからマップを自動生成する基礎を学ぶためのクラス
public class MapGenerator : MonoBehaviour
{
    [Header("マップデータ（階層ごとにセット）")]
    // 複数の「シート（階層）」を表現するために、配列でCSVを持たせます
    // [0]地下 [1]1階 [2]2階 のようにInspectorから設定します
    public TextAsset[] mapFloorData;

    [Header("生成するブロックのプレハブ")]
    public GameObject[] floorB1;          //[11]地下床 
    public GameObject[] floor1;           //[12]床
    public GameObject[] pitfall;          //[13]落とし穴

    public GameObject[] normalWallPrefab; // [21]壁
    public GameObject[] lampWallPrefab;   //     ランプ付きの壁
    public GameObject[] door;             // [22]扉
    public GameObject[] dark;             // []未定

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
                            case 1: // 壁ブロックの場合
                                continuousWallCount++; // 壁が連続しているのでカウントアップ！

                                // 「3個おき」の判定：カウントが3で割り切れる時だけランプ付きにする
                                if (continuousWallCount % 3 == 0)
                                {
                                    Instantiate(lampWallPrefab[0], spawnPos, Quaternion.identity, mapParent);
                                }
                                else
                                {
                                    Instantiate(normalWallPrefab[0], spawnPos, Quaternion.identity, mapParent);
                                }
                                break;

                            case 2: // 扉
                                    Instantiate(door[0], spawnPos, Quaternion.identity, mapParent);
                                    continuousWallCount = 0; // 壁が途切れたのでカウントリセット
                            break;
                        }
                        break;

                }
            }
        }

        Debug.Log($"階層 {floorIndex} のマップ生成が完了しました！");
    }
}