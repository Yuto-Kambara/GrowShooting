using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public const string TitleScene = "TitleScene";
    public const string GameScene = "PlayScene";

    public static void LoadTitle() => SceneManager.LoadScene(TitleScene);
    public static void LoadGame() => SceneManager.LoadScene(GameScene);
}
