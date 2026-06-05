// www.acidicvoid.com

using FlaxEngine;

namespace SoulSource.Physics;

/// <summary>
/// PrefabSpawner Script.
/// </summary>
public class PrefabSpawner : Script
{
    public InputEvent FireBtn = new();
    public Prefab Fab;
    public float YeetForce = 500;

    private void Spawn()
    { 
        if (Fab == null)
            return;

        Camera mainCamera = Camera.MainCamera;
        Vector3 nDir = mainCamera.Direction.Normalized;
        Vector3 spawnPoint = mainCamera.Position + (nDir * 100f);
        
        var spawned = PrefabManager.SpawnPrefab(Fab, spawnPoint);
        var rb = spawned.As<RigidBody>();
        rb.AddForce(nDir * YeetForce, ForceMode.VelocityChange);
        
    }
    
    /// <inheritdoc/>
    public override void OnEnable()
    {
        // Here you can add code that needs to be called when script is enabled (eg. register for events)
    }

    /// <inheritdoc/>
    public override void OnUpdate()
    {
        if (FireBtn.State == InputActionState.Press)
        {
            Spawn();
        }
    }
}