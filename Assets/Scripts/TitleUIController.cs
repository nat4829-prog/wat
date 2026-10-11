using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ปุ่มเปลี่ยนฉาก (ชื่อเมธอดและพารามิเตอร์เหมือนเดิมทุกอย่าง ปุ่มใน Inspector ไม่ต้องตั้งใหม่)
/// แก้จาก Application.LoadLevel ที่เลิกใช้แล้ว มาเป็น SceneManager.LoadScene
/// </summary>
public class TitleUIController : MonoBehaviour
{
    public void PlayScene(string sceneName)
    {
        Load(sceneName);
    }

    public void RankScene(string sceneName)
    {
        Load(sceneName);
    }

    public void BackScene(string sceneName)
    {
        Load(sceneName);
    }

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;     // Application.Quit ไม่ทำอะไรใน Editor
#else
        Application.Quit();
#endif
    }

    static void Load(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("[TitleUIController] ไม่พบฉาก \"" + sceneName + "\" ใน Build Profiles > Scene List หรือชื่อไม่ตรง");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }
}
