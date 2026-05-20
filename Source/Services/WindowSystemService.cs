using Dalamud.Interface.Windowing;

namespace TyrianTargeting.Services;

public sealed class WindowSystemService : IDisposable
{
    private readonly WindowSystem windowSystem;

    public WindowSystemService(string namespaceName)
    {
        this.windowSystem = new WindowSystem(namespaceName);
    }

    public void AddWindow(Window window) => this.windowSystem.AddWindow(window);
    public void Draw() => this.windowSystem.Draw();

    public void Dispose()
    {
        this.windowSystem.RemoveAllWindows();
    }
}
