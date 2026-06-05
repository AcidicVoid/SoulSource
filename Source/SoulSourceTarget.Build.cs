using Flax.Build;

public class SoulSourceTarget : GameProjectTarget
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();

        // Reference the modules for game
        Modules.Add("SoulSource");
    }
}
