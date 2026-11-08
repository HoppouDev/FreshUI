using Aether.Modding;

namespace FreshUI;

/// <summary>FreshUI has nothing to set up before the game starts: its changes are all mixins.</summary>
public sealed class Startup : IModInitializer
{
    public const string ModId = "fresh-ui";

    public void Initialize()
    {
    }
}
