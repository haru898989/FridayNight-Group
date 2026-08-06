using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultManager : MonoBehaviour
{
    public void BackToTitle()
    {
        if (Perfect_Online.Instance != null)
        {
            Perfect_Online.Instance.ReturnToTitle();
            return;
        }

        SceneManager.LoadScene("Title");
    }
}
