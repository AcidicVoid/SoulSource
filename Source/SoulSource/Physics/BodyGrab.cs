// www.acidicvoid.com

#nullable enable
using FlaxEngine;
using SoulSource.Player;

namespace SoulSource.Physics;

/// <summary>
/// Half-Life 2-style object grabbing mechanic.
///
/// Attach this script to the player actor (alongside <see cref="FpsController"/>).
///
/// How it works:
///
/// When the player presses the primary action button a ray is cast from the
/// camera.  If it strikes a <see cref="RigidBody"/> tagged with
/// <see cref="GrabbableObjectTag"/> within <see cref="MaxDistance"/> centimetres,
/// the object is "grabbed".
///
/// While held, a mass-aware spring-damper applies a corrective force every
/// physics tick at the exact world-space point that was hit, driving that point
/// towards a floating target = cameraPosition + cameraForward * holdDistance.
/// Because the force is applied at the grab point (not the centre of mass) the
/// object's weight distribution and moment of inertia are fully respected -
/// heavy objects accelerate slowly, off-centre grabs induce torque, etc.
///
/// Jitter fix
/// The camera transform updates every render frame (variable rate).  Feeding
/// the raw camera position straight into the spring each fixed step would turn
/// any frame-rate spike into an instantaneous force spike.  Instead, a smoothed
/// target position is maintained with an exponential moving average; the spring
/// always drives towards that stable point.
///
/// Clip prevention
/// <see cref="MinHoldDistance"/> clamps how close the hold target can ever get
/// to the camera.  A camera-to-grab-point ray cast additionally releases the
/// object if a solid surface slips between the player and the held object
/// (e.g. the player backs into a wall while carrying something).
///
/// Flax unit note
/// Flax distances are centimetres.  All exposed distance fields use cm.
/// Forces are kg*cm/s^2. Gravity approx. 981 cm/s^2.
///
/// Input wiring
/// All input is driven by <see cref="FpsController"/> via the
/// public <see cref="TryGrab"/>, <see cref="Release"/>, and <see cref="Throw"/>
/// methods.  This script owns no input polling.
/// </summary>

public class BodyGrab : Script
{
    /// <summary>Camera whose position and forward vector define the ray and hold target.</summary>
    [Header("References")]
    public Camera Cam = null!;

    // >> Grab

    /// <summary>
    /// Maximum reach in centimetres. Objects beyond this distance cannot be
    /// grabbed.  HL2's default was roughly 150 cm (aprox. arm's reach + a little).
    /// </summary>
    [Header("Grab")]
    public float MaxDistance = 200f;
    
    public LayersMask LayersMask = new();

    /// <summary>
    /// Only <see cref="RigidBody"/> actors that carry this tag are grabbable.
    /// Objects without the tag are ignored by the ray.
    /// </summary>
    public Tag GrabbableObjectTag;

    // >> Hold tuning

    /// <summary>
    /// How quickly (s^-1, exponential) the internal hold target chases the
    /// ideal camera-forward position each render frame.
    /// Lower = smoother/laggier. Higher = snappier. 15-25 is a good range.
    /// </summary>
    [Header("Hold Tuning")]
    [Limit(5f, 60f)]
    public float TargetSmoothing = 25f;

    /// <summary>
    /// Maximum speed (cm/s) the held object is allowed to travel.
    /// Acts as a hard cap on the velocity-controller output.
    /// 800 cm/s = 8 m/s - fast enough to track quick camera sweeps.
    /// </summary>
    public float MaxHeldSpeed = 2000f;

    /// <summary>
    /// Maximum speed change (cm/s^2) per second the "hands" can impart.
    /// Heavier objects feel heavier because their velocity changes more slowly
    /// relative to a light one for the same hand force.
    /// Maps to an effective maximum hand force: F = MaxHandAcceleration * mass.
    /// </summary>
    public float MaxHandAcceleration = 15000f;

    // >> Clip prevention

    /// <summary>
    /// Minimum distance (cm) the hold target is allowed to be from the camera.
    /// Prevents the held object from clipping into the view frustum when the
    /// player walks into a wall or angles the camera steeply downward.
    /// Should be at least the radius of the largest grabbable object.
    /// </summary>
    [Header("Clip Prevention")]
    public float MinHoldDistance = 50f;

    // >> Throw
    /// <summary>
    /// Impulse (kg*cm/s) applied along the camera forward direction when the
    /// player throws.  Tune relative to the mass of typical throwable objects.
    /// </summary>
    [Header("Throw")]
    public float ThrowImpulse = 25000f;

    // >> state

    private GrabbedObjectState? _grabbed;

    /// <summary>
    /// Smoothed world-space position the spring drives the grab point towards.
    /// Maintained as an exponential moving average so per-render-frame camera
    /// noise never feeds into the spring as an instantaneous force spike.
    /// Seeded to the grab point on pickup and updated every fixed step.
    /// </summary>
    private Vector3 _smoothedTarget;

    // >> API

    /// <summary>True while an object is being held.</summary>
    public bool IsHolding => _grabbed != null;

    /// <summary>
    /// Cast a ray from the camera and grab the first tagged rigid body within
    /// <see cref="MaxDistance"/>.  Safe to call when already holding - returns
    /// true immediately without disturbing the current grab.
    /// </summary>
    /// <returns>True if an object is now being held.</returns>
    public bool TryGrab()
    {
        Debug.Log("SoulSource::BodyGrab::TryGrab()");
        if (_grabbed != null)
            return true;

        bool didHit = FlaxEngine.Physics.RayCast(
            Cam.Position,
            Cam.Direction,
            out RayCastHit hit,
            MaxDistance,
            LayersMask,
            hitTriggers: false);

        if (!didHit)
            return false;
        
        // Require a RigidBody - static or kinematic meshes cannot be moved.
        RigidBody? rb = hit.Collider.AttachedRigidBody;
        if (rb == null)
            return false;

        // Tag check: only grab objects explicitly marked as grabbable.
        if (!rb.Tags.HasTag(GrabbableObjectTag))
            return false;

        float holdDist = Mathf.Max(hit.Distance, MinHoldDistance);
        _grabbed = new GrabbedObjectState(rb, hit.Point, holdDist);

        // Seed the smoothed target at the actual grab point so the spring
        // starts at rest rather than snapping from the origin.
        _smoothedTarget = hit.Point;

        return true;
    }

    /// <summary>
    /// Drops the held object without adding velocity.  Safe to call when not
    /// holding anything.
    /// </summary>
    public void Release()
    {
        Debug.Log("SoulSource::BodyGrab::Release()");
        _grabbed?.Restore();
        _grabbed = null;
    }

    /// <summary>
    /// Throws the held object forward along the camera direction.
    /// Restores physics damping before applying the impulse so the object
    /// travels naturally after leaving the player's hands.
    /// </summary>
    public void Throw()
    {
        Debug.Log("SoulSource::BodyGrab::Throw()");
        if (_grabbed == null)
            return;

        RigidBody body = _grabbed.Body;

        // Restore damping BEFORE the impulse so drag doesn't bleed off momentum.
        _grabbed.Restore();
        _grabbed = null;

        if (body)
            body.AddForce(Cam.Direction * ThrowImpulse, ForceMode.Impulse);
    }

    // >> Script lifecycle

    public override void OnEnable()
    {
        if (Cam == null)
        {
            Debug.LogWarning("[BodyGrab] Camera is not assigned.");
            Enabled = false;
        }
    }

    public override void OnDisable()
    {
        // Always clean up so the held object doesn't retain modified damping
        // if the script is toggled off at runtime.
        Release();
    }

    public override void OnUpdate()
    {
        // Input is handled entirely by FpsController via TryGrab / Release / Throw.
        if (_grabbed == null)
            return;

        // Auto-release safety
        // If the held object escapes too far (e.g. blasted away by an explosion,
        // or the player noclips through a wall) release it cleanly.
        float distSq    = Vector3.DistanceSquared(Cam.Position, _grabbed.Body.Position);
        float breakDist = MaxDistance * 2.5f;
        if (distSq > breakDist * breakDist)
        {
            Debug.Log("SoulSource::BodyGrab::OnUpdate(): distSq > breakDist * breakDist is true - releasing");
            Release();
            return;
        }

        // Smoothed target
        // This MUST run in OnUpdate (every render frame), not OnFixedUpdate.
        // The camera transform changes every rendered frame; advancing the EMA
        // only at the physics tick rate would leave the spring seeing a stale,
        // stepped target on all frames in between - exactly the jitter we want
        // to remove.
        //
        // alpha = 1 - e^(-k*dt)   exact discrete EMA coefficient
        float   safeHoldDist = Mathf.Max(_grabbed.HoldDistance, MinHoldDistance);
        Vector3 idealTarget  = Cam.Position + Cam.Direction * safeHoldDist;
        float   alpha        = 1f - Mathf.Exp(-TargetSmoothing * Time.DeltaTime);
        _smoothedTarget = Vector3.Lerp(_smoothedTarget, idealTarget, alpha);
    }

    public override void OnFixedUpdate()
    {
        if (_grabbed == null)
            return;

        RigidBody body = _grabbed.Body;

        // Guard: actor might have been destroyed externally.
        if (!body)
        {
            _grabbed = null;
            return;
        }

        // Clip prevention ray
        // _smoothedTarget is maintained in OnUpdate every render frame.
        // If a solid surface has moved between the camera and the held object
        // (player backed into a wall, object got wedged in geometry, etc.),
        // release immediately.  We ignore the held body's own colliders by
        // checking whether the ray hits something OTHER than the held body.
        Vector3 grabWorldPos   = _grabbed.GetWorldGrabPoint();
        Vector3 camToGrab      = grabWorldPos - Cam.Position;
        float   camToGrabDist  = camToGrab.Length;

        if (camToGrabDist > Mathf.Epsilon)
        {
            bool blocked = FlaxEngine.Physics.RayCast(
                Cam.Position,
                camToGrab / camToGrabDist,    // normalised direction
                out RayCastHit blockHit,
                camToGrabDist * 0.95f,     // slightly short so the held surface itself doesn't trigger
                LayersMask,
                hitTriggers: false);

            if (blocked && blockHit.Collider.AttachedRigidBody != body)
            {
                Debug.Log("SoulSource::BodyGrab::OnUpdate(): blocked && blockHit.Collider.AttachedRigidBody != body true - releasing");
                Release();
                return;
            }
        }

        // Velocity control (HL2-style)
        //
        // A force-based spring is numerically unstable at typical physics
        // timesteps: the stiffness needed for a responsive hold causes the
        // integrator to overshoot every step, producing oscillation even when
        // the object is perfectly still.
        //
        // Instead, we use direct velocity control - the same approach used byF
        // HL2's CPhysicsPlayerController / shadow objects:
        //
        //   * Compute the velocity that would bring the grab point exactly
        //     to _smoothedTarget in one timestep.
        //   * Clamp to MaxHeldSpeed so we can't clip through walls.
        //   * Clamp the *change* in velocity (acceleration) by
        //     MaxHandAcceleration so heavy objects feel heavier - they resist
        //     sudden direction changes more than light ones.
        //   * Set LinearVelocity directly.  No integration, no overshoot.
        //   * Cancel gravity so the object doesn't sag while held.

        float   mass         = body.Mass;
        Vector3 delta        = _smoothedTarget - grabWorldPos;

        // ideal velocity to close the gap in one step
        Vector3 targetVel = delta / Time.DeltaTime;

        // hard speed cap (avoid killyeet)
        float tSpeed = targetVel.Length;
        if (tSpeed > MaxHeldSpeed)
            targetVel *= MaxHeldSpeed / tSpeed;

        // clamp velocity change by effective hand force / mass
        // maxDeltaV = (MaxHandAcceleration) * dt
        // Heavier objects get the same force cap but accelerate less (F=ma).
        float   maxDeltaV = MaxHandAcceleration * Time.DeltaTime;
        Vector3 velDelta  = targetVel - body.LinearVelocity;
        float   velDeltaLen = velDelta.Length;
        if (velDeltaLen > maxDeltaV)
            velDelta *= maxDeltaV / velDeltaLen;

        // set velocity
        body.LinearVelocity = body.LinearVelocity + velDelta;

        // cancel gravity so the held object floats at target height.
        // Applied as a continuous force (kg*cm/s^2) at the COM - not at the
        // grab point, to avoid introducing phantom torque.
        body.AddForce(-FlaxEngine.Physics.Gravity * mass, ForceMode.Force);
    }

#if FLAX_EDITOR
    public override void OnDebugDraw()
    {
        if (_grabbed == null)
            return;

        const float sphereRadius = 4f;

        var grabPt = _grabbed.GetWorldGrabPoint();

        // Red sphere   = current grab point on the object
        // Green sphere = smoothed hold target (what the spring is aiming for)
        // Cyan sphere  = raw ideal target (before smoothing, for comparison)
        // Yellow line  = spring vector
        var safeHoldDist = Mathf.Max(_grabbed.HoldDistance, MinHoldDistance);
        var idealTarget = Cam.Position + Cam.Direction * safeHoldDist;

        DebugDraw.DrawSphere(new BoundingSphere(grabPt,          sphereRadius), Color.Red,   0f, depthTest: false);
        DebugDraw.DrawSphere(new BoundingSphere(_smoothedTarget, sphereRadius), Color.Green, 0f, depthTest: false);
        DebugDraw.DrawSphere(new BoundingSphere(idealTarget,     sphereRadius), Color.CornflowerBlue, 0f, depthTest: false);
        DebugDraw.DrawLine(grabPt, _smoothedTarget, Color.Yellow, 0f, depthTest: false);

        base.OnDebugDraw();
    }
#endif
}