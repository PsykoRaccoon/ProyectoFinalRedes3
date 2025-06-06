using Mirror;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using static UnityEngine.UI.Image;
using System;
using Mirror.Examples.Common;
using static UnityEngine.UIElements.UxmlAttributeDescription;
[RequireComponent(typeof(CharacterController))]
public class PlayerManager : NetworkBehaviour, IDamageable
{

    private SceneScript scScript;

    [Header("Health Settings")]
    public float maxHealth;
    private float currentHealth;
    

    [Header("Weapon Settings")]
    public GameObject[] weapons;
    public Camera cam;
    
    
    private RaycastHit rayHit;
    private Weapon activeWeapon;
    private int selectedWeaponLocal = 0;
    private float bulletsLeft, bulletsShot;
    private float[] bulletsLeftPerWeapon;
    private bool shooting, readyToShoot, reloading, allowButtonHold;
    

    [Header("Movement Settings")]
    public float speed;
    public float jumpHeight;
    public float gravity;

    private CharacterController controller;
    private Vector3 velocity;

    [Header("Sprint Settings")]
    public float sprintMultiplier;

    private bool isSprinting;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance;
    public LayerMask groundMask;

    private bool isGrounded;

    [Header("Camera Settings")]
    public float sensitivity;
    public Transform playerBody;
    public GameObject thirdPersonCameraPoint;
    public GameObject firstPersonCameraPoint;


    private float xRotation = 0f;
    private bool isThirdPerson = false;

    [Header("NameTag Settings")]
    public GameObject floating;
    public TextMesh txtNameTag;
    public TextMesh txtHealthTag;


    [Header("User Interface")]
    public TextMeshProUGUI canvasAmmoText;
    public TextMeshProUGUI canvasHealthText;

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip hitSound;

    private AudioSource audioSource;

    [Header("Graphics")]
    public GameObject muzzleFlash, bulletHole;


    [Header("Player Appearance ")]
    public GameObject capsula;
    public Material myMaterial;





    #region Variables de red


    [SyncVar(hook = nameof(OnWeaponChanged))]
    public int activeWeaponSync = 0;

    void OnWeaponChanged(int _old, int _new)
    {
        if (_new < 0 || _new >= weapons.Length)
        {
            Debug.LogError($"Weapon index {_new} is out of bounds!");
            return;
        }
        foreach (GameObject weapon in weapons)
            weapon.SetActive(false);


        weapons[_new].SetActive(true);

        
        

        if (isLocalPlayer)
        {
            activeWeapon = weapons[_new].GetComponent<Weapon>();
            if (activeWeapon == null)
            {
                Debug.LogError("Weapon component not found on selected weapon.");
            }
            if (bulletsLeftPerWeapon == null || bulletsLeftPerWeapon.Length <= _new)
            {
                Debug.LogError("bulletsLeftPerWeapon array is null or too short.");
                return;
            }

            bulletsLeft = bulletsLeftPerWeapon[_new];
            UpdateAmmoUI(bulletsLeft);
        }
       
    }

    [SyncVar(hook = nameof(OnNameChange))]
    public string playerName;

    void OnNameChange(string _old, string _new)
    {
        txtNameTag.text = playerName;
    }

    [SyncVar(hook = nameof(OnColorChange))]
    public Color playerColor;
    
    void OnColorChange(Color _old, Color _new)
    {
        myMaterial = new Material(capsula.GetComponent<Renderer>().material);
        myMaterial.color = _new;
        capsula.GetComponent<Renderer>().material = myMaterial;

        txtNameTag.color = _new;
        txtHealthTag.color = _new;
    }

    [SyncVar(hook = nameof(OnHealthChanged))]
    public int healthSync;

    void OnHealthChanged(int _old, int _new)
    {
        canvasHealthText.text = healthSync.ToString();
        txtHealthTag.text = healthSync.ToString();

        if (isLocalPlayer)
            UpdateHealthUI(_new);

        if (healthSync > 70)
        {
            txtHealthTag.color = Color.green;
        }
        else if (healthSync > 35)
        {
            txtHealthTag.color = Color.yellow;
        }
        else if (healthSync > 10)
        {
            txtHealthTag.color = Color.red;
        }
        else if (healthSync <= 0)
        {
            Debug.LogError("You're dead...");
        }
    }


    #endregion

    #region Funciones de red

    [Command]
    public void CmdSetupPlayer(string newName, Color newColor)
    {
        playerName = newName;
        playerColor = newColor;

        scScript.statusText = $"{playerName} joined.";
    }
    [Command]
    public void CmdSendPlayerMessage(string newName)
    {
        if (scScript)
            scScript.statusText = $"{playerName} says: new name: {newName}";
    }
    [Command]
    public void CmdChangeActiveWeapon(int newIndex)
    {
        Debug.Log($"Changing weapon to index {newIndex}, total weapons: {weapons.Length}");
        activeWeaponSync = newIndex;
    }
    [Command]
    public void CmdUpdateHealth(int _health)
    {
        healthSync = _health;
    }
    [Command]
    public void CmdShootRay(GameObject target, Vector3 origin, Vector3 direction, Vector3 muzzlePos)
    {
        if (target == null) return;

        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.ApplyDamage(activeWeapon.damageToEnemy);
        }
        if (Physics.Raycast(origin, direction, out RaycastHit hit, activeWeapon.range))
        {
            RpcPlayShootEffect(hit.point, hit.normal, muzzlePos);
        }
        //RpcFireWeaponEffects(origin, direction);

        //if (Physics.Raycast(origin, direction, out RaycastHit hit, activeWeapon.range))
        //{
        //    if (hit.collider.gameObject == target)
        //    {
        //        IDamageable damageable = target.GetComponent<IDamageable>();
        //        if (damageable != null)
        //        {
        //            damageable.ApplyDamage(activeWeapon.damageToEnemy);
        //        }
        //    }
        //}
        //RcpFireWeapon(target, origin, direction);
    }
   

    [ClientRpc]
    void RpcPlayShootEffect(Vector3 hitPoint, Vector3 hitNormal, Vector3 muzzlePos)
    {
        // Solo instanciamos efectos visuales
        if (bulletHole != null)
        {
            GameObject impact = Instantiate(bulletHole, hitPoint, Quaternion.LookRotation(hitNormal));
            Instantiate(muzzleFlash, activeWeapon.firePos.position, Quaternion.identity);
            Destroy(impact, 2f);
        }
        if (muzzleFlash != null)
        {
            Instantiate(muzzleFlash, muzzlePos, Quaternion.identity);
        }

        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }

    //[ClientRpc]
    //void RpcFireWeaponEffects( Vector3 origin, Vector3 direction)
    //{

    //    if (!isLocalPlayer)
    //    {
    //        if (activeWeapon != null && muzzleFlash != null && activeWeapon.firePos != null)
    //        {
    //            Instantiate(muzzleFlash, activeWeapon.firePos.position, Quaternion.identity);
    //            audioSource.PlayOneShot(shootSound);
    //        }
    //    }
    //    //GameObject bullet = Instantiate(activeWeapon.bullet, activeWeapon.firePos.position, activeWeapon.firePos.rotation);
    //    //bullet.GetComponent<Rigidbody>().linearVelocity = bullet.transform.forward * activeWeapon.speed;
    //    //Destroy(bullet, activeWeapon.life);

    //}


    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        scScript = GameObject.FindFirstObjectByType<SceneScript>();
        audioSource = GetComponent<AudioSource>();

        if (!isLocalPlayer)
        {
            cam.gameObject.SetActive(false); // Seguridad extra
            if (cam != null)
                cam.gameObject.SetActive(false);

            // 🔴 DESACTIVA LOS CANVAS E INTERFACES QUE SOLO DEBE VER EL JUGADOR LOCAL
            if (canvasAmmoText != null)
                canvasAmmoText.transform.parent.gameObject.SetActive(false);

            // 🔴 DESACTIVA LAS ARMAS EN PRIMERA PERSONA
            foreach (GameObject weapon in weapons)
            {
                weapon.SetActive(false);
            }
        }
    }
   
    public override void OnStartClient()
    {
        name = $"Player[{netId}|{(isLocalPlayer ? "local" : "remote")}]";
        Debug.Log("OnStartClient: " + name);

        if (!isLocalPlayer)
        {
            DisableRemoteVisuals();
        }

    }
    private void DisableRemoteVisuals()
    {
        if (cam != null)
            cam.gameObject.SetActive(false);

        if (canvasAmmoText != null)
            canvasAmmoText.transform.parent.gameObject.SetActive(false);

        foreach (GameObject weapon in weapons)
        {
            weapon.SetActive(false);
        }
    }
    public override void OnStartServer()
    {
        name = $"Player[{netId}|server]";
        Debug.Log("OnStartServer: " + name);
    }

    public override void OnStartLocalPlayer()
    {
        
        ToggleLocalComponents(true);
        scScript.plMove = this;

        cam.gameObject.SetActive(true);
        cam.transform.SetParent(transform);
        cam.transform.position = firstPersonCameraPoint.transform.position;
        cam.transform.rotation = firstPersonCameraPoint.transform.rotation;
        
        //// Camera.main.transform.localPosition = new Vector3(0, 0, 0);
        //cam.transform.SetPositionAndRotation(firstPersonCameraPoint.transform.position, firstPersonCameraPoint.transform.rotation);

      
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentHealth = maxHealth;
        readyToShoot = true;

        string _name = "player_" + UnityEngine.Random.Range(100, 999);
        Color _color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));

        CmdSetupPlayer(_name, _color);
        CmdSendPlayerMessage(_name);

        foreach (GameObject weapon in weapons)
        {
            weapon.SetActive(false);
        }
        if (isLocalPlayer)
        {
            bulletsLeftPerWeapon = new float[weapons.Length];
            for (int i = 0; i < weapons.Length; i++)
            {
                Weapon w = weapons[i].GetComponent<Weapon>();
                bulletsLeftPerWeapon[i] = w.magSize;
            }
        }

        selectedWeaponLocal = 0;
        activeWeaponSync = selectedWeaponLocal;


        SetActiveWeapon(selectedWeaponLocal); // <-- fuerza activación local inmediata
        CmdChangeActiveWeapon(selectedWeaponLocal);
        // OnWeaponChanged(-1, selectedWeaponLocal);
        //CmdChangeActiveWeapon(selectedWeaponLocal);

        //activeWeapon = weapons[activeWeaponSync].GetComponent<Weapon>();
        if (activeWeapon != null)
        {
            bulletsLeft = activeWeapon.magSize;
        }
        else
        {
            Debug.LogError("activeWeapon es null en OnStartLocalPlayer");
        }
    }
    
    #endregion

    #region Funciones locales
    private void ToggleLocalComponents(bool enable)
    {
       
        if (!isLocalPlayer)
        {
         
            if (cam != null)
                cam.gameObject.SetActive(false);

            if (canvasAmmoText != null)
                canvasAmmoText.transform.parent.gameObject.SetActive(false);

            foreach (GameObject weapon in weapons)
            {
                weapon.SetActive(false);
            }

        }
        if (cam != null)
            cam.gameObject.SetActive(enable);


        if (canvasAmmoText != null)
            canvasAmmoText.transform.parent.gameObject.SetActive(enable);
    }
    private void Update()
    {
        if (!isLocalPlayer)
        {
            floating.transform.LookAt(cam.transform);
            return;
        }


        MyInput();
        GroundCheck();
        Movement();
        Jump();
        controller.Move(velocity * Time.deltaTime);
        HandleViewToggle();
        HandleLook();
        
    }
    #region Movimiento
    private void MyInput()
    {
        if (!isLocalPlayer) return;
        if (allowButtonHold)
        {
            shooting = Input.GetKeyDown(KeyCode.Mouse0);
        }
        else
        {
            shooting = Input.GetKeyDown(KeyCode.Mouse0);
        }

        if (Input.GetKeyDown(KeyCode.R) && bulletsLeft < activeWeapon.magSize && !reloading)
        {
            Reload();
        }

        if (readyToShoot && shooting && !reloading && bulletsLeft > 0)
        {
            bulletsShot = activeWeapon.bulletsPerShot;

            bulletsLeft -= bulletsShot;
            bulletsLeftPerWeapon[selectedWeaponLocal] = bulletsLeft;

            if (isLocalPlayer)
                UpdateAmmoUI(bulletsLeft);

            Shoot();
        }
        //PARA CAMBIAR DE ARMA
        if (Input.GetButtonDown("Fire2"))
        {
            if (!isLocalPlayer) return;

            selectedWeaponLocal += 1;
            if (selectedWeaponLocal >= weapons.Length)
            {
                selectedWeaponLocal = 0;
            }

            CmdChangeActiveWeapon(selectedWeaponLocal);
        }
        for (int i = 0; i < weapons.Length; i++)
        {
            // Teclas numéricas: 1 = index 0, 2 = index 1, etc.
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                if (i != selectedWeaponLocal)
                {
                    selectedWeaponLocal = i;
                    CmdChangeActiveWeapon(selectedWeaponLocal);
                }
            }
        }
    }
    private void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }

    private void Movement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        isSprinting = Input.GetKey(KeyCode.LeftShift);

        float currentSpeed = isSprinting ? speed * sprintMultiplier : speed;


        //Vector3 move = transform.right * x + transform.forward * z;
        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;

        // Elimina el componente vertical para evitar moverse hacia arriba/abajo
        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = camRight * x + camForward * z;
        move.y = 0;
        controller.Move(move.normalized * currentSpeed * Time.deltaTime);

        

    }

    private void Jump()
    {
        velocity.y += gravity * Time.deltaTime;

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
    #region Cámara
 
    void HandleViewToggle()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            //isThirdPerson = !isThirdPerson;
            //        //transform.localPosition = isThirdPerson ? thirdPersonPosition : firstPersonPosition;
            //        
            isThirdPerson = !isThirdPerson;

            if (isThirdPerson)
            {
                cam.transform.SetPositionAndRotation(thirdPersonCameraPoint.transform.position, thirdPersonCameraPoint.transform.rotation);
            }
            else
            {
                cam.transform.SetPositionAndRotation(firstPersonCameraPoint.transform.position, firstPersonCameraPoint.transform.rotation);
            }
        }
    }
    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
    #endregion

    #region DISPARO
    private void SetActiveWeapon(int index)
    {
        for (int i = 0; i<weapons.Length; i++)
        {
            weapons[i].SetActive(i == index);
        }

        activeWeapon = weapons[index].GetComponent<Weapon>();
        selectedWeaponLocal = index;
        bulletsLeft = bulletsLeftPerWeapon[index];

        if (isLocalPlayer)
        {
            UpdateAmmoUI(bulletsLeft);
        }
    }
    private void Shoot()
    {
        if (!isLocalPlayer) return;

        float x = UnityEngine.Random.Range(-activeWeapon.spread, activeWeapon.spread);
        float y = UnityEngine.Random.Range(-activeWeapon.spread, activeWeapon.spread);


        readyToShoot = false;
        Vector3 muzzlePos = activeWeapon.firePos.position;
        audioSource.PlayOneShot(shootSound);

        Vector3 rayOrigin = activeWeapon.firePos.position;
        Vector3 rayDirection = cam.transform.forward + new Vector3(x, y, 0);

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit rayHit, activeWeapon.range))
        {
            Debug.DrawRay(rayOrigin, rayDirection * rayHit.distance, Color.green, 1f);
            if(rayHit.collider.TryGetComponent<NetworkIdentity>(out NetworkIdentity netID))
            {
                CmdShootRay(rayHit.collider.gameObject, rayOrigin, rayDirection, muzzlePos);
                
            }


            //Quaternion hitRotation = Quaternion.LookRotation(rayHit.normal);
            //GameObject hole = Instantiate(bulletHole, rayHit.point, hitRotation);
            //AudioSource.PlayClipAtPoint(hitSound, rayHit.point);
            //Destroy(hole, 10f);

        }
        else
        {
            Debug.DrawLine(rayOrigin, rayDirection * activeWeapon.range, Color.red, 1f);
            Vector3 fallbackHit = cam.transform.position + cam.transform.forward * activeWeapon.range;
            CmdShootRay(null, cam.transform.position, cam.transform.forward, muzzlePos);
        }

       //Instantiate(muzzleFlash, activeWeapon.firePos.position, Quaternion.identity);

        //bulletsLeftPerWeapon[selectedWeaponLocal] = bulletsLeft;

        bulletsShot--;

        if (isLocalPlayer)
        {
            UpdateAmmoUI(bulletsLeft);
        }
        Invoke("ResetShot", activeWeapon.timeBetweenEachShot);

        if (bulletsShot > 0 && bulletsLeft > 0)
        {
            Invoke("Shoot", activeWeapon.timeBetweenEachBullet);
        }
    }
    private void ResetShot()
    {
        readyToShoot = true;
    }
    #region RELOAD
    private void Reload()
    {
        reloading = true;
        audioSource.PlayOneShot(reloadSound);
        Invoke("ReloadFinished", activeWeapon.reloadTime);
    }

    private void ReloadFinished()
    {
        bulletsLeft = activeWeapon.magSize;
        bulletsLeftPerWeapon[selectedWeaponLocal] = bulletsLeft;
        reloading = false;


        if (isLocalPlayer)
        {
            UpdateAmmoUI(bulletsLeft);
        }
    }
    #endregion
    #endregion
    #endregion

    #region VIDA

    public void ApplyDamage(float amount)
    {
        if (!isServer) return;
        TakeDamage(amount);
        
    }
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        UpdateHealthUI(currentHealth);
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    public bool Heal(float amount)
    {
        if (currentHealth >= maxHealth)
            return false;


        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        UpdateHealthUI(currentHealth);
        return true;
    }

    void Die()
    {
        print("Morido");
        //LOGICA RESPAWN
        gameObject.SetActive(false);
    }
    #endregion

    #region UI
    public void UpdateAmmoUI (float ammo)
    {
        if (!isLocalPlayer) return;
        canvasAmmoText.text = ammo.ToString() + "/" + activeWeapon.magSize;
    }
    void UpdateHealthUI(float health)
    {
        if (!isLocalPlayer) return;
        if (canvasHealthText != null)
        {
            canvasHealthText.text = $"Health: {health}/{maxHealth}";
        }
    }
    #endregion
    #endregion
}
