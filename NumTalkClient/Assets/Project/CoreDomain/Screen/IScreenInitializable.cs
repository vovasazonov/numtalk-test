namespace Project.CoreDomain.Screen
{
    public interface IScreenInitializable
    {
        void SetLoadingScreen(string id);
        void SetSplashScreen(string id);
    }
}