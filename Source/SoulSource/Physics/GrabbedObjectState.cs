// www.acidicvoid.com

using FlaxEngine;

namespace SoulSource.Physics;

/// <summary>
/// Holds all per-grab state for a single picked-up rigid body.
///
/// Responsibilities:
///   * Store the grab point in the object's local space (so it tracks rotation).
///   * Store the initial camera-to-object distance so the object floats at the
///     same depth it was grabbed from (exactly like HL2).
///   * Override the body's linear/angular damping while held and restore it on
///     release, so objects don't keep spinning or drifting after being dropped.
/// </summary>
internal sealed class GrabbedObjectState
{
    // Public read-only data
    /// <summary>The rigid body currently being held.</summary>
    public RigidBody Body { get; }

    /// <summary>
    /// The grab point expressed relative to the body's origin, in the body's
    /// own local space.  Storing it in local space means it naturally follows
    /// the object as it rotates, so forces are always applied at the correct
    /// physical location on the mesh.
    /// </summary>
    public Vector3 LocalGrabOffset { get; }

    /// <summary>
    /// Camera-to-object distance at the moment of pickup.  The object will be
    /// kept at this depth in front of the camera, replicating the HL2 behaviour
    /// where you hold objects at arm's reach, not snapped to a fixed position.
    /// </summary>
    public float HoldDistance { get; }

    // Damping applied while the object is held
    // High angular damping stops objects from spin-flailing mid-air.
    // Linear damping adds a small drag that helps the spring settle faster.
    private const float HoldLinearDamping  = 6f;
    private const float HoldAngularDamping = 10f;

    // Originals - restored verbatim on release so no physics property is
    // permanently mutated by the grabber.
    private readonly float _savedLinearDamping;
    private readonly float _savedAngularDamping;

    // >> Construction

    /// <param name="body">The rigid body to hold.</param>
    /// <param name="worldHitPoint">World-space ray-hit point on the collider.</param>
    /// <param name="holdDistance">Distance from camera to worldHitPoint.</param>
    public GrabbedObjectState(RigidBody body, Vector3 worldHitPoint, float holdDistance)
    {
        Body         = body;
        HoldDistance = holdDistance;

        // Convert the world hit point into the body's local space so it
        // travels correctly when the body rotates.
        //   offset_world = hitPoint - bodyOrigin
        //   offset_local = Inverse(bodyRotation) * offset_world
        Vector3 worldOffset = worldHitPoint - body.Position;
        LocalGrabOffset = worldOffset * Quaternion.Invert(body.Orientation);

        // Save and override damping
        _savedLinearDamping  = body.LinearDamping;
        _savedAngularDamping = body.AngularDamping;
        body.LinearDamping   = HoldLinearDamping;
        body.AngularDamping  = HoldAngularDamping;
    }

    // >> Helpers

    /// <summary>
    /// Returns the current world-space position of the grab point, accounting
    /// for all translation and rotation the body has undergone since pickup.
    /// </summary>
    public Vector3 GetWorldGrabPoint()
    {
        // Rotate local offset back to world space, then shift by body position.
        return Body.Position + LocalGrabOffset * Body.Orientation;
    }

    /// <summary>
    /// Restores the body's original damping values.  Must be called exactly once
    /// when releasing or throwing.  Safe to call even if the actor was destroyed.
    /// </summary>
    public void Restore()
    {
        if (!Body) return;
        Body.LinearDamping  = _savedLinearDamping;
        Body.AngularDamping = _savedAngularDamping;
    }
}