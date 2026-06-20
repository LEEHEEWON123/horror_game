using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void OnStartButton()
    {
        GameManager.Instance?.BeginNewRun("Map_00");
    }

    public void OnQuitButton()
    {
        Application.Quit();
    }
}
