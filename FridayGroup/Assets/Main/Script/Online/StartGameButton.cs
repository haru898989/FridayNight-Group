using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGameButton : MonoBehaviour
{
    [SerializeField] private Perfect_Online online;

    public void StartGame()
    {
        if (online == null)
        {
            Debug.LogError("Perfect_Online が設定されていません");
            return;
        }

        NetworkRunner runner = online.Runner;

        if (runner == null)
        {
            Debug.LogError("NetworkRunner がありません");
            return;
        }

        // Scene Authority(1P)だけがシーンを切り替える
        if (!runner.IsSceneAuthority)
        {
            Debug.Log("SceneAuthorityではないため開始できません");
            return;
        }

        int buildIndex = SceneUtility.GetBuildIndexByScenePath("Assets/Main/Scene/Main.unity");

        runner.LoadScene(SceneRef.FromIndex(buildIndex), LoadSceneMode.Single);

        Debug.Log("ゲーム開始！");
    }
}