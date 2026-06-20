using UnityEngine;

public class ComingSoonUI : MonoBehaviour
{
    public void OnMainMenuButton()
    {
        SceneTransitioner.Instance.LoadScene("MainMenu");
    }
}
