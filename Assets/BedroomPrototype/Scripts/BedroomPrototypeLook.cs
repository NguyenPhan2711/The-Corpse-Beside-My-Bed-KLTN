using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class BedroomPrototypeLook : MonoBehaviour
{
    public float sensitivity = 0.12f;
    float yaw;
    float pitch;
    void Start() { yaw = transform.eulerAngles.y; pitch = transform.eulerAngles.x; }
    void Update()
    {
        bool click;
        bool escape;
        Vector2 delta;
#if ENABLE_INPUT_SYSTEM
        click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        delta = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
        click = Input.GetMouseButtonDown(0);
        escape = Input.GetKeyDown(KeyCode.Escape);
        delta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 12f;
#endif
        if (click) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        if (escape) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        if (Cursor.lockState != CursorLockMode.Locked) return;
        yaw += delta.x * sensitivity;
        pitch = Mathf.Clamp(pitch - delta.y * sensitivity, -80f, 80f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
    void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
}
