using cowsins;
using UnityEngine;

public class CameraLookBehaviour
{
    private MovementContext context;
    private InputManager inputManager;
    private Rigidbody rb;
    private Transform camera;
    private IPlayerMovementStateProvider playerMovement;
    private IPlayerMovementEventsProvider playerEvents;
    private IWeaponBehaviourProvider weaponBehaviourProvider;
    private IWeaponReferenceProvider weaponReference;
    private IWeaponRecoilProvider weaponRecoil;
    private PlayerMovementSettings playerSettings;

    private float cameraPitch;
    private float cameraYaw;
    private float cameraRoll;

    // Controls the current sensitivity.
    // Sensitivity can be overrided by the Game Settings Manager
    private float currentSensX, currentSensY, currentControllerSensX, currentControllerSensY;

    private PlayerOrientation orientation => playerMovement.Orientation;

    public CameraLookBehaviour(MovementContext context)
    {
        this.context = context;
        this.rb = context.Rigidbody;
        this.camera = context.Camera;
        this.inputManager = context.InputManager;

        this.playerMovement = context.Dependencies.PlayerMovementState;
        this.playerEvents = context.Dependencies.PlayerMovementEvents;
        this.weaponBehaviourProvider = context.Dependencies.WeaponBehaviour;
        this.weaponReference = context.Dependencies.WeaponReference;
        this.weaponRecoil = context.Dependencies.WeaponRecoil;
        this.playerSettings = context.Settings;

        GatherSensitivityValues();
    }

    public void Tick()
    {
        int sensYInverted = playerSettings.invertYSensitivty ? -1 : 1;
        int sensYInvertedController = playerSettings.invertYControllerSensitivty? 1 : -1;
        float sensitivityMultiplier = weaponBehaviourProvider.IsAiming ? playerSettings.aimingSensitivityMultiplier : 1;

        // Grab the Inputs from the user.
        float rawMouseX = inputManager.GatherRawMouseX(currentSensX, currentControllerSensX);
        float rawMouseY = inputManager.GatherRawMouseY(sensYInverted, sensYInvertedController, currentSensY, currentControllerSensY);
        float mouseX = rawMouseX * sensitivityMultiplier;
        float mouseY = rawMouseY * sensitivityMultiplier;

        // Calculate new yaw rotation ( around the y axis )
        cameraYaw = camera.localRotation.eulerAngles.y + mouseX + weaponRecoil.RecoilYawOffset * Time.deltaTime;
        //Rotate Camera Pitch ( around x axis )
        cameraPitch -= mouseY - weaponRecoil.RecoilPitchOffset * Time.deltaTime;
        // Make sure we dont over- or under-rotate.
        // The reason why the value is 89.7 instead of 90 is to prevent errors with the wallrun
        cameraPitch = Mathf.Clamp(cameraPitch, -playerSettings.maxCameraAngle, playerSettings.maxCameraAngle);

        CalculateCameraRoll();

        ApplyCameraRotation();

        HandleAimAssist();
    }

    public void VerticalLook()
    {
        if (PauseMenu.isPaused || !playerSettings.allowVerticalLookWhileClimbing) return;

        int sensYInverted = playerSettings.invertYSensitivty ? -1 : 1;
        int sensYInvertedController = playerSettings.invertYControllerSensitivty ? -1 : 1;
        float sensitivityMultiplier = weaponBehaviourProvider.IsAiming ? playerSettings.aimingSensitivityMultiplier : 1;

        float rawMouseY = inputManager.GatherRawMouseY(
            sensYInverted,
            sensYInvertedController,
            currentSensY,
            currentControllerSensY
        );

        float mouseY = rawMouseY * sensitivityMultiplier;

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -playerSettings.maxCameraAngle, playerSettings.maxCameraAngle);

        ApplyCameraRotation();
    }

    private void ApplyCameraRotation()
    {
        camera.localRotation = Quaternion.Euler(cameraPitch, cameraYaw, cameraRoll);
        orientation.UpdateOrientation(context.Rigidbody.transform.position, cameraYaw);
    }


    private void CalculateCameraRoll()
    {
        if (playerMovement.IsWallRunning) cameraRoll = context.WallLeft ? Mathf.Lerp(cameraRoll, -playerSettings.wallrunCameraTiltAmount, Time.deltaTime * playerSettings.cameraTiltTransitionSpeed) : Mathf.Lerp(cameraRoll, playerSettings.wallrunCameraTiltAmount, Time.deltaTime * playerSettings.cameraTiltTransitionSpeed);
        else if (playerMovement.IsCrouching && playerMovement.CurrentSpeed >= playerMovement.WalkSpeed && playerEvents.Events.InvokeAllowSlide() && !context.HasJumped) cameraRoll = Mathf.Lerp(cameraRoll, playerSettings.slidingCameraTiltAmount, Time.deltaTime * playerSettings.cameraTiltTransitionSpeed);
        else cameraRoll = Mathf.Lerp(cameraRoll, 0, Time.deltaTime * playerSettings.cameraTiltTransitionSpeed);
    }

    private void HandleAimAssist()
    {
        // Decide wether to use aim assist or not
        if (!playerSettings.applyAimAssist) return;
        Transform target = AimAssistHit();
        if (target == null || Vector3.Distance(target.position, context.Rigidbody.transform.position) > playerSettings.maximumDistanceToAssistAim) return;
        // Get the direction to look at
        Vector3 direction = (target.position - context.Rigidbody.transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        // Smoothly override our current camera rotation towards the selected enemy
        camera.localRotation = Quaternion.Lerp(camera.localRotation, targetRotation, Time.deltaTime * playerSettings.aimAssistSpeed);
    }

    /// <summary>
    /// Returns the transform you want your camera to be sticked to 
    /// </summary>
    private Transform AimAssistHit()
    {
        // Aim assist will work on enemies only, since we dont wanna snap our camera on any object around the environment
        // max range to snap
        float range = 40;
        // Max range depends on the weapon range if you are holding a weapon
        if (weaponReference.Weapon != null) range = weaponReference.Weapon.bulletRange;

        // Detect our potential transform
        RaycastHit hit;
        if (Physics.SphereCast(camera.GetChild(0).position, playerSettings.aimAssistSensitivity, camera.GetChild(0).transform.forward, out hit, range) && hit.transform.CompareTag("Enemy"))
        {
            return hit.collider.transform;
        }
        else return null;
    }

    private void GatherSensitivityValues()
    {
        var inst = GameSettingsManager.Instance;
        if (inst)
        {
            currentSensX = inst.playerSensX;
            currentSensY = inst.playerSensY;
            currentControllerSensX = inst.playerControllerSensX;
            currentControllerSensY = inst.playerControllerSensY;
        }
        else
        {
            currentSensX = playerSettings.sensitivityX;
            currentSensY = playerSettings.sensitivityY;
            currentControllerSensX = playerSettings.controllerSensitivityX;
            currentControllerSensY = playerSettings.controllerSensitivityY;
        }
    }
}
