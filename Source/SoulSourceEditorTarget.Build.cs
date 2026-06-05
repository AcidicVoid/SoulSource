using Flax.Build;

public class SoulSourceEditorTarget : GameProjectEditorTarget
{
    /// <inheritdoc />
    public override void Init()
    {
        base.Init();

        // Reference the modules for editor
        Modules.Add("SoulSource");
        Modules.Add("SoulSourceEditor");
    }
}
