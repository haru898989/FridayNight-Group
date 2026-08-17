/// <summary>
/// ステージ選択画面からMapシーンへ、選択したResourcesパスを引き継ぎます。
/// Perfect_Onlineが両クライアントへ同じ値を同期してからシーンを切り替えます。
/// </summary>
public static class StageSelectionContext
{
    public static string SelectedStageResourcePath { get; private set; }

    public static bool HasSelection => !string.IsNullOrWhiteSpace(SelectedStageResourcePath);

    public static void SetSelectedStage(string resourcePath)
    {
        SelectedStageResourcePath = Normalize(resourcePath);
    }

    public static void Clear()
    {
        SelectedStageResourcePath = null;
    }

    private static string Normalize(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? null
            : path.Trim().Replace('\\', '/').Trim('/');
    }
}
