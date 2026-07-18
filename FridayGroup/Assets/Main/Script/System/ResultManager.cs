using UnityEngine;
using UnityEngine.SceneManagement;

public class ResultManager : MonoBehaviour
{
    public void BackToTitle()
    {
        SceneManager.LoadScene("Title");
    }
}