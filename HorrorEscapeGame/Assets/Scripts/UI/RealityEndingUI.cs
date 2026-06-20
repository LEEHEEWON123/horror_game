using UnityEngine;

public class RealityEndingUI : MonoBehaviour
{
    public void OnMainMenuButton()
    {
        SceneTransitioner.Instance.LoadScene("MainMenu");
    }
}
