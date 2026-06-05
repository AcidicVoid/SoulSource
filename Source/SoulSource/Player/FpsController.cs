using FlaxEngine;
using SoulSource.Physics;
using Quaternion = FlaxEngine.Quaternion;
using Vector3 = FlaxEngine.Vector3;

#if FLAX_EDITOR
using FlaxEditor;
#endif

namespace SoulSource.Player;

/// <summary>
/// FpsController Script.
/// </summary>
[Category("SoulSource: Player")]
[RequireActor(typeof(CharacterController))]
public class FpsController : Script
{
    [Header("Inputs")]
    public InputAxis  InputLookX;
    public InputAxis  InputLookY;
    public InputAxis  InputMoveX;
    public InputAxis  InputMoveY;
    public InputEvent InputJump            = new();
    public InputEvent InputRun             = new();
    public InputEvent InputUse             = new();
    public InputEvent InputActionPrimary   = new();
    public InputEvent InputActionSecondary = new();
    public InputEvent InputCrouch          = new();
    public InputEvent InputNoClip          = new();
    
    [Header("Camera")] 
    [Range(0f, 1f)] public float RelCameraHeight = 0.8f;

    [Header("Physics")] 
    public BodyGrab BodyGrab;
    [Range(1f,20f)]
    public float PlayerMoveSpeed      = 5f;
    [Range(1.1f,2f)]
    public float PlayerRunSpeedFactor = Mathf.GoldenRatio;
    [Range(0.01f,2f)]
    public float Sensitivity          = 1f;
    
    private const float PlayerLookSmoothMax = 50;
    [Range(0f, PlayerLookSmoothMax)]
    public float PlayerLookSmooth = 50f;
    
    private const float PlayerWalkSmoothMax = 50;
    [Range(0f, PlayerWalkSmoothMax)]
    public float PlayerWalkSmooth = 50f;
    
    public float PlayerJumpForce  = 20f;
    public float PlayerWeight     = 60f;
    
    private CharacterController _controller;
    private Collider _collider;

    [Header("Settings")] 
    public bool NoClip = false;
    public bool JumpToEditorCamera = false;
    public bool CanMove = true;
    public bool ShowDebugInfo = true;

    // Physics
    private Vector3    _linearMovement        = Vector3.Zero;
    private Vector3    _linearMovementInput   = Vector3.Zero;
    private Vector3    _angularMovementInput  = Vector3.Zero;
    private Quaternion _orientationAtLastJump = Quaternion.Zero;
    
    // Internals
    private Camera        _camera;
    private bool          _jumpFired = false;
    private float         _jumpForce = 0f;
    private bool          _isRunning = false;
    private float         _linearSpeedFactor = 1;
    private float         _playerMaxSpeed = 0f;

    private void HandleHotkeys()
    {
        if (InputNoClip.State == InputActionState.Press)
        {
            NoClip = !NoClip;
        }
    }

    private void HandleActionPrimary()
    {
        if (BodyGrab == null)
            return;

        if (InputActionPrimary.State == InputActionState.Press)
        {
            // Attempt to grab the object the player is looking at.
            BodyGrab.TryGrab();
        }

        if (InputActionPrimary.State == InputActionState.Release)
        {
            // Button released – drop the held object gently.
            BodyGrab.Release();
        }
    }

    private void HandleActionSecondary()
    {
        // Secondary button throws the held object forward.
        // Only fires on the frame the button is pressed to avoid repeat throws.
        if (BodyGrab != null && InputActionSecondary.State == InputActionState.Press)
            BodyGrab.Throw();
    }

    private void UpdateInputVectors()
    {
        _isRunning              = InputRun.State == InputActionState.Pressing;
        _linearSpeedFactor      = _isRunning ? PlayerRunSpeedFactor : 1f;
        
        // Mapping
        _linearMovementInput.X  = Mathf.Lerp(_linearMovementInput.X,  InputMoveX.Value, Time.DeltaTime * PlayerWalkSmooth);
        _linearMovementInput.Z  = Mathf.Lerp(_linearMovementInput.Z,  InputMoveY.Value, Time.DeltaTime * PlayerWalkSmooth);
        _angularMovementInput.Y = Mathf.Lerp(_angularMovementInput.Y, InputLookX.Value, Time.DeltaTime * PlayerLookSmooth);
        _angularMovementInput.X = Mathf.Lerp(_angularMovementInput.X, InputLookY.Value, Time.DeltaTime * PlayerLookSmooth);
    }

    private void UpdateJumpFired()
    {
        bool canJump = _controller.IsGrounded;
        _jumpFired = canJump && InputJump.State == InputActionState.Press;
    }

    private void HandleUseButton()
    {
        if (InputUse.State == InputActionState.Press)
        {
            // InteractableManager.Instance?.RunInteraction(
            //     InteractableManager.Instance?.GetFirstInteractableInRange());
        }
    }
    
    /// <inheritdoc/>
    public override void OnEnable()
    {
        _controller = Actor as CharacterController;
        
        if (_controller == null)
            return;

        _playerMaxSpeed = PlayerMoveSpeed * PlayerRunSpeedFactor;
        _controller.AutoGravity = true;
        _camera = Actor.GetChild<Camera>();
        
        // Position the camera
        var cameralocalPos = _camera.LocalPosition;
        cameralocalPos.Y = (-_controller.Height) + (_controller.Height * RelCameraHeight);
        _camera.LocalPosition = cameralocalPos;
        
        PlayerLookSmooth = Mathf.Clamp(PlayerLookSmoothMax - PlayerLookSmooth, 10f, PlayerLookSmoothMax);
        PlayerWalkSmooth = Mathf.Clamp(PlayerWalkSmoothMax - PlayerWalkSmooth, 10f, PlayerWalkSmoothMax);
#if FLAX_EDITOR
        if (JumpToEditorCamera)
            Actor.Position = Editor.Instance.Windows.EditWin.Viewport.ViewPosition;
#endif
    }

    /// <inheritdoc/>
    public override void OnDisable()
    {
        // Here you can add code that needs to be called when script is disabled (eg. unregister from events)
    }

    /// <inheritdoc/>
    public override void OnUpdate()
    {
        HandleHotkeys();
        UpdateInputVectors();
        UpdateJumpFired();
        HandleUseButton();
        HandleActionPrimary();
        HandleActionSecondary();
    }

    public override void OnFixedUpdate()
    {
        if (!CanMove)
            return;
        
        // NoClip
        _controller.AutoGravity = !NoClip;
        if (NoClip)
        {
            _jumpForce = 0f;
            _jumpForce += (InputJump.State   == InputActionState.Pressing) ? PlayerMoveSpeed : 0f;
            _jumpForce -= (InputCrouch.State == InputActionState.Pressing) ? PlayerMoveSpeed : 0f; 
        }
        else
        {
            // Handle Jump (skipped when NoClip is active)
            if (_jumpFired)
            {
                _orientationAtLastJump = Quaternion.Euler(_controller.Orientation.EulerAngles);
                _jumpForce = PlayerJumpForce;
            }
            
            _jumpForce = Mathf.Lerp(_jumpForce, _jumpForce - (PlayerWeight / 9.18f), Time.DeltaTime * 5f);
            _jumpForce = Mathf.Clamp(_jumpForce,-_playerMaxSpeed * 2f, _playerMaxSpeed);
        }
        
        // Get linear movement
        float moveSignX = _linearMovementInput.X >= 0f ? 1f : -1f;
        float moveSignZ = _linearMovementInput.Z >= 0f ? 1f : -1f;
        _linearMovement.X = moveSignX * Mathf.Saturate(Utils.Easing.EaseInOutCubic(Mathf.Abs(_linearMovementInput.X)))    * PlayerMoveSpeed * _linearSpeedFactor;
        _linearMovement.Z = moveSignZ * Mathf.Saturate(Utils.Easing.EaseInOutCubicNeg(Mathf.Abs(_linearMovementInput.Z))) * PlayerMoveSpeed * _linearSpeedFactor;
        _linearMovement.Y = _jumpForce;

        // Apply rotation to controller
        var orientation = Actor.Orientation.EulerAngles;
        orientation.Y += _angularMovementInput.Y * Sensitivity;
        Actor.Orientation = Quaternion.Euler(orientation);
        
        // Transform local movement to world space based on actor's orientation
        Vector3 worldMovement = Vector3.Transform(_linearMovement, Actor.Orientation);
        
        // move controller
        _controller.Move(worldMovement);

        // rotate camera (pitch only, relative to controller)
        var cameraOrientation = _camera.LocalOrientation.EulerAngles;
        cameraOrientation.X += _angularMovementInput.X * Sensitivity;
        _camera.LocalOrientation = Quaternion.Euler(cameraOrientation);
    }
#if FLAX_EDITOR
    public override void OnDebugDraw()
    {
        if (!ShowDebugInfo)
            return;
        
        Color c = Color.GreenYellow;
        float posx = 30, posy = 30f, step = 30f;
        DebugDraw.DrawText("fps: " + ProfilingTools.Stats.FPS, new Float2(posx, posy), c, 16);
        posy += step;
        DebugDraw.DrawText("Jump Force: " + _jumpForce.ToString("0.00"), new Float2(posx, posy), c, 16);
        posy += step;
        DebugDraw.DrawText("Grounded:   " + (_controller != null ? _controller.IsGrounded : "controller is null"), new Float2(posx, posy), c, 16);
        // posy += step;
        // DebugDraw.DrawText("Move X:   " + _linearMovement.X.ToString("0.00"), new Float2(posx, posy), c, 16);
        // posy += step;
        // DebugDraw.DrawText("Rotate X: " + _angularMovementInput.X.ToString("0.00"), new Float2(posx, posy), c, 16);
        // posy += step;
        // DebugDraw.DrawText("Rotate Y: " + _angularMovementInput.Y.ToString("0.00"), new Float2(posx, posy), c, 16);
    }
#endif
}