using System;
using UnityEngine;

/// <summary>
/// O'yinchi (kapsula) boshqaruvi: joystik bilan harakat, harakat yo'nalishiga burilish,
/// sakrash (SFX) va tanga yig'ish (SFX).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Boshqaruv")]
    public VirtualJoystick joystick;
    public Transform cameraTransform;

    [Header("Sozlamalar")]
    public float moveSpeed = 6f;
    public float rotateSpeed = 720f;   // daraja/soniya
    public float jumpHeight = 1.6f;
    public float gravity = -22f;

    /// <summary>Menyuda false bo'ladi - o'yinchi qimirlamaydi</summary>
    public bool canMove = false;

    public int Score { get; private set; }
    public event Action<int> OnScoreChanged;

    CharacterController controller;
    float verticalVelocity;
    bool jumpRequested;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        // 1) Kirish: joystik (mobil) yoki WASD/strelkalar (kompyuterda sinash uchun)
        Vector2 input = Vector2.zero;
        if (joystick != null) input = joystick.Direction;

        Vector2 keyboard = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (keyboard.sqrMagnitude > input.sqrMagnitude) input = keyboard;

        if (Input.GetKeyDown(KeyCode.Space)) jumpRequested = true;

        if (!canMove)
        {
            input = Vector2.zero;
            jumpRequested = false;
        }

        // 2) Yo'nalishni kameraga nisbatan hisoblaymiz
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;
        if (cameraTransform != null)
        {
            forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        }

        Vector3 move = forward * input.y + right * input.x;
        if (move.sqrMagnitude > 1f) move.Normalize();

        // 3) Harakat yo'nalishiga qarab silliq burilish
        if (move.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, rotateSpeed * Time.deltaTime);
        }

        // 4) Sakrash va gravitatsiya
        bool grounded = controller.isGrounded;
        if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;

        if (jumpRequested && grounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayJump(); // SFX: sakrash
        }
        jumpRequested = false;

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * moveSpeed + Vector3.up * verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>"Sakrash" tugmasi shu metodni chaqiradi</summary>
    public void Jump()
    {
        jumpRequested = true;
    }

    void OnTriggerEnter(Collider other)
    {
        Coin coin = other.GetComponent<Coin>();
        if (coin == null) return;

        coin.Collect();
        Score++;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayCoin(); // SFX: bonus olish
        OnScoreChanged?.Invoke(Score);
    }
}
