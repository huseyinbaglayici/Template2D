namespace Project.Contexts.Application
{
    /// <summary>Scene names in Build Profiles order. Boot must stay at index 0.</summary>
    public static class SceneNames
    {
        public const string Boot = "Boot";
        public const string Home = "Home";
        public const string Game = "Game";

        public static bool IsContent(string sceneName) => sceneName == Home || sceneName == Game;
    }
}
