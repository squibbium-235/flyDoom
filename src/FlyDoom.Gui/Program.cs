using Avalonia;
using System;

namespace FlyDoom.Gui;

class Program
{
    // Initialization code. Do not use any Avalonia, third-party APIs, or any
    // SynchronizationContext-reliant code before AppMain is called: things are
    // not initialised yet and may break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration; do not remove. Also used by the visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
