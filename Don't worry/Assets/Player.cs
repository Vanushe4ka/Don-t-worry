using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpForce = 5f;
    public float staminaMax = 100f;
    public float staminaDrainRate = 10f;  // Скорость траты стамины при беге
    public float staminaRegenRateWalk = 5f; // Скорость восстановления при ходьбе
    public float staminaRegenRateIdle = 15f; // Скорость восстановления при стоянии
    public float groundCheckDistance = 0.1f;
    public LayerMask groundMask;

    [Header("Camera Settings")]
    public Transform playerCamera;
    public float mouseSensitivity = 100f;
    private float xRotation = 0f;

    [Header("Audio Settings")]
    public AudioClip walkSound;
    public AudioClip jumpSound;
    public AudioClip falledSound;
    public AudioClip fatigueSound;
    public AudioSource walkAudioSource;
    public AudioSource jumpAudioSource;
    public AudioSource fatigueAudioSource; // Отдельный источник для звука усталости
    public float runPitchMultiplier = 1.5f; // Ускорение звука бега
    public float runVolumeMultiplier = 1.2f; // Увеличение громкости звука бега

    private Rigidbody rb;
    private Vector3 movement;
    [SerializeField] bool isGrounded = false;
    
    [SerializeField] float stamina;
    private bool isRunning = false;
    private bool isMoving = false;
    private bool jumpRequest = false;
    private bool isFatigued = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        stamina = staminaMax;

        // Отключаем автоматический захват курсора
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        HandleInput();
        HandleStamina();
        PlayFootstepSound();
        HandleCamera();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleInput()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        movement = transform.right * moveX + transform.forward * moveZ;

        isMoving = movement.magnitude > 0;

        // Проверка на бег
        isRunning = Input.GetKey(KeyCode.LeftShift) && !isFatigued;

        // Прыжок
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            jumpRequest = true;
        }
    }

    private void HandleMovement()
    {
        // Проверка на землю
        bool prevIsGrounded = isGrounded;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundMask);
        if (!prevIsGrounded && isGrounded)
        {
            jumpAudioSource.PlayOneShot(falledSound);
        }

        // Перемещение
        float speed = isRunning ? runSpeed : walkSpeed;
        Vector3 moveVelocity = movement.normalized * speed;
        rb.MovePosition(rb.position + moveVelocity * Time.fixedDeltaTime);

        // Прыжок
        if (jumpRequest && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpRequest = false;

            // Проигрывание звука прыжка
            jumpAudioSource.PlayOneShot(jumpSound);
        }
    }

    private void HandleStamina()
    {
        if (isRunning)
        {
            stamina -= staminaDrainRate * Time.deltaTime;
            if (stamina < 0) stamina = 0;

            // Если стамина закончилась и игрок не устал, проигрываем звук усталости
            if (stamina <= 0 && !isFatigued)
            {
                isFatigued = true;
                fatigueAudioSource.PlayOneShot(fatigueSound);
            }
        }
        else
        {
            if (isMoving)
            {
                stamina += staminaRegenRateWalk * Time.deltaTime;
            }
            else
            {
                stamina += staminaRegenRateIdle * Time.deltaTime;
            }

            if (stamina > staminaMax) stamina = staminaMax;

            // Сбрасываем состояние усталости, если стамина восстановилась
            if (stamina > staminaMax/5)
            {
                isFatigued = false;
            }
        }
    }

    private void PlayFootstepSound()
    {
        if (isGrounded && isMoving)
        {
            // Проигрывание звука ходьбы или бега
            walkAudioSource.pitch = isRunning ? runPitchMultiplier : 1f;
            walkAudioSource.volume = isRunning ? 1f * runVolumeMultiplier : 1f;
            if (!walkAudioSource.isPlaying)
            {
                walkAudioSource.clip = walkSound;
                walkAudioSource.loop = true;
                walkAudioSource.Play();
            }
        }
        

        // Остановка звука, если игрок не движется
        if (!isMoving || !isGrounded)
        {
            walkAudioSource.Stop();
        }
    }

    private void HandleCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);  // Ограничиваем вращение по оси X

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);  // Вращаем персонажа по оси Y
    }
}
