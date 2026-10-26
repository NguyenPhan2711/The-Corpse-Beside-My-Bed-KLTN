using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class BedroomPlayerController : MonoBehaviour
{
    public InputActionAsset inputActions;
    public Transform eye;
    public float walkSpeed = 1.8f;
    public float sprintSpeed = 3.2f;
    public float mouseSensitivity = 0.12f;
    public float pitchLimit = 80f;
    CharacterController controller;
    InputActionAsset runtimeActions;
    InputAction move, look, sprint;
    float pitch, verticalVelocity;
    void Awake()
    {
        controller = GetComponent<CharacterController>();
        runtimeActions = Instantiate(inputActions);
        runtimeActions.bindingMask = InputBinding.MaskByGroup("Keyboard&Mouse");
        move = runtimeActions.FindAction("Player/Move", true);
        look = runtimeActions.FindAction("Player/Look", true);
        sprint = runtimeActions.FindAction("Player/Sprint", true);
    }
    void OnEnable() { if (runtimeActions != null) runtimeActions.FindActionMap("Player").Enable(); }
    void Start() { CaptureCursor(); }
    void CaptureCursor() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) CaptureCursor();
        bool captured = Cursor.lockState == CursorLockMode.Locked;
        if (captured)
        {
            var delta = look.ReadValue<Vector2>() * mouseSensitivity;
            transform.Rotate(0f, delta.x, 0f);
            pitch = Mathf.Clamp(pitch - delta.y, -pitchLimit, pitchLimit);
            eye.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
        Vector2 input = captured ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;
        Vector3 velocity = (transform.right * input.x + transform.forward * input.y) * (sprint.IsPressed() ? sprintSpeed : walkSpeed);
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity = Mathf.Max(verticalVelocity - 20f * Time.deltaTime, -40f);
        velocity.y = verticalVelocity;
        var flags = controller.Move(velocity * Time.deltaTime);
        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f) verticalVelocity = 0f;
    }
    public void ResetViewPitch() { pitch = 0; verticalVelocity = 0; }
    void OnDisable()
    {
        if (runtimeActions != null) runtimeActions.Disable();
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }
    void OnDestroy() { if (runtimeActions != null) Destroy(runtimeActions); }
}

