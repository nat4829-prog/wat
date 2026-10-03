using UnityEngine;
public class TitleUIController : MonoBehaviour
{
public void PlayScene(string sceneName) {
Application.LoadLevel(sceneName);
}
public void RankScene(string sceneName) {
        Application.LoadLevel(sceneName);
    }
    public void BackScene(string sceneName) {
        Application.LoadLevel(sceneName);
    }
    public void Exit() {
        Application.Quit();
    }
}