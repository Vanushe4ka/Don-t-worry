using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

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
    [SerializeField] AudioClip walkSound;
    [SerializeField] AudioClip jumpSound;
    [SerializeField] AudioClip falledSound;
    [SerializeField] AudioClip fatigueSound;
    [SerializeField] AudioSource walkAudioSource;
    [SerializeField] AudioSource jumpAudioSource;
    [SerializeField] AudioSource fatigueAudioSource; // Отдельный источник для звука усталости
    [SerializeField] float runPitchMultiplier = 1.5f; // Ускорение звука бега
    [SerializeField] float runVolumeMultiplier = 1.2f; // Увеличение громкости звука бега
    [SerializeField] float jumpSoundVolume;
    [SerializeField] float fallSoundVolume;

    private Rigidbody rb;
    private Vector3 movement;
    [SerializeField] bool isGrounded = false;
    float timeInFall = 0;

    [SerializeField] float stamina;
    private bool isRunning = false;
    private bool isMoving = false;
    private bool jumpRequest = false;
    private bool isFatigued = false;

    List<(string, int)> gribsInventory = new List<(string, int)>();
    [SerializeField] (string, int)[] targetGribsInventory;
    public float rayDistance = 100f;

    public LayerMask layerMask;

    Grib selectedGrib;

    [SerializeField] Text gribText;
    [SerializeField] Image blackScreen;

    [SerializeField] Text finalText;
    [SerializeField] Graphic[] finalUIGraphics;

    bool isDead = true;
    Coroutine gribTextCoroutine;

    [SerializeField] Text gribsInventoryText;
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }
    public void SetTargetInventory((string, int)[] targetInventory)
    {
        targetGribsInventory = targetInventory;
    } 
    public void StartGame()
    {
        stamina = staminaMax;
        gribsInventory.Clear();
        UpdateInventory();
        StartCoroutine(finalUIChange(false));
        StartCoroutine(ChangeBlackScreen(false));
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        transform.rotation = Quaternion.Euler(0, 0, 0);
        isDead = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void PickUpGrib(Grib grib)
    {
        Debug.Log("PickUpGrib");
        ShowGribMessage(grib.Name);
        if (grib.isPoisonous)
        {
            Dead();
            Destroy(grib.gameObject);
            return;
        }
        bool isContain = false;
        for (int i = 0; i < gribsInventory.Count; i++)
        {
            if (gribsInventory[i].Item1 == grib.Name)
            {
                gribsInventory[i] = (gribsInventory[i].Item1, gribsInventory[i].Item2 + 1);
                isContain = true;
                break;
            }
        }
        if (!isContain) { gribsInventory.Add((grib.Name, 1)); }
        UpdateInventory();
        Destroy(grib.gameObject);

        bool isWin = true;
        for (int i = 0; i < targetGribsInventory.Length; i++)
        {
            if (!gribsInventory.Contains(targetGribsInventory[i])) { isWin = false;break; }
        }
        if (isWin)
        {
            Wictory();
        }
    }
    private void UpdateInventory()
    {
        int maxGribNameLength = 0;
        int maxGribNum = 0;
        for (int i = 0; i < gribsInventory.Count; i++)
        {
            if (gribsInventory[i].Item1.Length > maxGribNameLength) { maxGribNameLength = gribsInventory[i].Item1.Length; }
            if (gribsInventory[i].Item2 > maxGribNum) { maxGribNum = gribsInventory[i].Item2; }
        }
        string text = "";
        for (int i = 0; i < gribsInventory.Count; i++)
        {
            text += gribsInventory[i].Item1 + new string(' ', maxGribNameLength - gribsInventory[i].Item1.Length) + " - " 
                + new string(' ',maxGribNum.ToString().Length - gribsInventory[i].Item2.ToString().Length) + gribsInventory[i].Item2.ToString() + '\n';
        }
        gribsInventoryText.text = text;
    }
    void ShowGribMessage(string name)
    {
        if (gribTextCoroutine != null)
        {
            StopCoroutine(gribTextCoroutine);
        }
        gribTextCoroutine = StartCoroutine(gribTextChange(name));
    }
    IEnumerator finalUIChange(bool isActivated)
    {
        float timer = 0;
        float t;
        while (timer < 1)
        {
            timer += Time.deltaTime;
            if (isActivated) { t = timer; }
            else { t = 1 - timer; }
            for (int i = 0; i < finalUIGraphics.Length; i++)
            {
                finalUIGraphics[i].color = new Color(finalUIGraphics[i].color.r, finalUIGraphics[i].color.g, finalUIGraphics[i].color.b, t);
            }
            blackScreen.color = new Color(0, 0, 0, t);
            yield return null;
        }
        if (isActivated) { t = 1; }
        else { t = 0; }
        for (int i = 0; i < finalUIGraphics.Length; i++)
        {
            finalUIGraphics[i].color = new Color(finalUIGraphics[i].color.r, finalUIGraphics[i].color.g, finalUIGraphics[i].color.b, t);
        }
    }
    IEnumerator finalTextChange(string message)
    {
        finalText.text = "";
        WaitForSeconds wait = new WaitForSeconds(0.05f);
        for (int i = 1; i <= message.Length; i++)
        {
            finalText.text = message.Substring(0, i);
            yield return wait;
        }
        finalText.text = message;
    }
    
    IEnumerator gribTextChange(string message)
    {
        gribText.text = "";
        gribText.color = new Color(1, 1, 1, 1);
        WaitForSeconds wait = new WaitForSeconds(0.05f);
        for (int i = 1; i <= message.Length; i++)
        {
            gribText.text = message.Substring(0, i);
            yield return wait;
        }
        gribText.text = message;
        yield return new WaitForSeconds(1);

        float timer = 0;
        while (timer < 1)
        {
            timer += Time.deltaTime;
            gribText.color = new Color(1, 1, 1, 1 - timer);
            yield return null;
        }
    }
    void Dead()
    {
        rb.constraints = 0;
        isDead = true;
        StartCoroutine(ChangeBlackScreen(true));
        StartCoroutine(finalTextChange("You Died..."));
        StartCoroutine(finalUIChange(true));
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    void Wictory()
    {
        isDead = true;
        StartCoroutine(ChangeBlackScreen(true));
        StartCoroutine(finalTextChange("You Won!!!"));
        StartCoroutine(finalUIChange(true));

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    IEnumerator ChangeBlackScreen(bool isActivated)
    {
        float timer = 0;
        float t;
        while (timer < 1)
        {
            timer += Time.deltaTime;
            if (isActivated) { t = timer; }
            else { t = 1 - timer; }
            blackScreen.color = new Color(0, 0, 0, t);
            yield return null;
        }
        if (isActivated) { t = 1; }
        else { t = 0; }
        blackScreen.color = new Color(0, 0, 0, t);
    }
    private void Update()
    {
        if (!isDead)
        {
            HandleInput();
            HandleStamina();
            PlayFootstepSound();
            HandleCamera();
            SendRaycast();
            InputProcessing();
        }
        
    }
    void InputProcessing()
    {
        if (selectedGrib == null) { return; }
        if (Input.GetMouseButtonDown(0))
        {
            PickUpGrib(selectedGrib);
        }
    }
    
    void SendRaycast()
    {
        // Получение центра экрана
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

        // Преобразование центра экрана в мировые координаты
        Ray ray = Camera.main.ScreenPointToRay(screenCenter);
        
        // Создаем переменную для хранения информации о столкновении
        RaycastHit hit;

        // Если луч пересекает какой-то объект с указанной маской слоёв
        if (Physics.Raycast(ray, out hit, rayDistance, layerMask))
        {
            Grib grib = hit.collider.gameObject.GetComponent<Grib>();
            if (grib != null)
            {
                if (selectedGrib != null && selectedGrib != grib)
                {
                    selectedGrib.ChangeBoard(false);
                    selectedGrib = null;
                }
                selectedGrib = grib;
                selectedGrib.ChangeBoard(true);
            }
            else if (selectedGrib != null)
            {
                selectedGrib.ChangeBoard(false);
                selectedGrib = null;
            }
            // Можно добавить дополнительные действия при попадании луча в объект
        }
        else if (selectedGrib != null)
        {
            selectedGrib.ChangeBoard(false);
            selectedGrib = null;
        }
    }
    private void FixedUpdate()
    {
        if (!isDead)
        {
            HandleMovement();
        }
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
            jumpAudioSource.volume = timeInFall * fallSoundVolume;
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
            jumpAudioSource.volume = jumpSoundVolume;
            jumpAudioSource.PlayOneShot(jumpSound);
        }
        if (isGrounded)
        {
            timeInFall = 0;
        }
        else
        {
            timeInFall += Time.fixedDeltaTime;
        }
        if (transform.position.y < -100) { Dead(); }
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
