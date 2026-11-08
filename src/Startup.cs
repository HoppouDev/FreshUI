using Aether.Modding;

namespace FreshUI;

public sealed class Startup : IModInitializer
{
    public const string ModId = "fresh-ui";

    public void Initialize()
    {
        using var reader = new StreamReader(ModResources.OpenRead(ModId, "welcome.txt"));
        Console.WriteLine($"[{ModId}] {reader.ReadLine()}");
    }
}
