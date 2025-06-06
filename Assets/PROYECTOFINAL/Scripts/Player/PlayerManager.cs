using Mirror;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using static UnityEngine.UI.Image;
using System;
using Mirror.Examples.Common;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using System.Collections;
[RequireComponent(typeof(CharacterController))]
public class PlayerManager : NetworkBehaviour, IDamageable
{

    private SceneScript scScript;

    [Header("Health Settings")]
    public float maxHealth;
    public float currentHealth;
    

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


        if (!isLocalPlayer)
            return;


        if (isLocalPlayer)
        {
            if (bulletsLeftPerWeapon == null || bulletsLeftPerWeapon.Length <= _new)
            {
                Debug.LogError("bulletsLeftPerWeapon array is null or too short.");
                Debug.LogWarning("bulletsLeftPerWeapon not initialized yet, skipping OnWeaponChanged for local player.");
                return;
            }
            activeWeapon = weapons[_new].GetComponent<Weapon>();
            if (activeWeapon == null)
            {
                Debug.LogError("Weapon component not found on selected weapon.");
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
        Debug.Log("esta mamada que");

        if (isLocalPlayer && canvasHealthText != null)
            UpdateHealthUI(_new);
        //RpcUpdateHealth(_new);
        if (txtHealthTag != null)
        {
            txtHealthTag.text = _new.ToString();

            Color color = Color.green;
            if (_new <= 70 && _new > 35) color = Color.yellow;
            else if (_new <= 35 && _new > 10) color = Color.red;
            else if (_new <= 10) color = Color.black;

            txtHealthTag.color = color;
        }
        if (_new <= 0 && isLocalPlayer)
        {
            Debug.LogError("You're dead...");
        }

        Debug.Log($"OnHealthChanged fired: {_old} -> {_new}");


        //Debug.Log($"OnHealthChanged fired: {_old} -> {_new}");
        //canvasHealthText.text = _new.ToString();
        //txtHealthTag.text = _new.ToString();

    }


    #endregion

    #region Funciones de red
    [ClientRpc]
    void RpcUpdateHealth(int _new)
    {
        if (!isLocalPlayer) return;
        Debug.Log($"[CLIENT] Vida actualizada: {_new}");
        UpdateHealthUI(_new);
        //if (canvasHealthText != null)
        //    canvasHealthText.text = _new.ToString();

        //if (txtHealthTag != null)
        //    txtHealthTag.text = _new.ToString();
    }
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
        //Debug.Log($"Changing weapon to index {newIndex}, total weapons: {weapons.Length}");
        activeWeaponSync = newIndex;
    }
    [Command]
    public void CmdUpdateHealth(int _health)
    {
        healthSync = _health;
    }
    [Command]
    public void CmdShootRay(uint targetNetId, Vector3 origin, Vector3 direction, Vector3 muzzlePos)
    {
        if (activeWeapon == null)
        {
            Debug.LogWarning("[SERVER] activeWeapon es null en CmdShootRay");
            return;
        }
        if (targetNetId == 0)
        {
            Debug.Log($"[SERVER] Disparo al vacío. Hay {NetworkServer.spawned.Count} objetos en escena.");

            if (Physics.Raycast(origin, direction, out RaycastHit missHit, activeWeapon.range))
            {
                RpcPlayShootEffect(missHit.point, missHit.normal, muzzlePos);
            }
            else
            {
                // Si ni siquiera golpea algo (por ejemplo, espacio vacío), aún puedes mostrar el flash del arma
                RpcPlayShootEffect(origin + direction * activeWeapon.range, -direction, muzzlePos);
            }
            return;
        }
        if (NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
        {
            IDamageable damageable = targetIdentity.GetComponentInParent<IDamageable>();
            Debug.Log($"[SERVER] Encontrado targetNetId={targetNetId}, damageable={damageable}");

            if (damageable != null)
            {
                damageable.ApplyDamage(activeWeapon.damageToEnemy);
            }

            if (Physics.Raycast(origin, direction, out RaycastHit hit, activeWeapon.range))
            {
               // RpcPlayShootEffect(hit.point, hit.normal, muzzlePos);
            }
        }
        else
        {
            Debug.LogWarning($"[SERVER] No se encontró el objeto con netId {targetNetId} en NetworkServer.spawned");

            // Aún así mostramos efecto visual
            if (Physics.Raycast(origin, direction, out RaycastHit fallbackHit, activeWeapon.range))
            {
                //RpcPlayShootEffect(fallbackHit.point, fallbackHit.normal, muzzlePos);
            }
            else
            {
                //RpcPlayShootEffect(origin + direction * activeWeapon.range, -direction, muzzlePos);
            }
        }
        //NetworkIdentity targetIdentity;
        //if (NetworkServer.spawned.TryGetValue(targetNetId, out targetIdentity))
        //{
        //    IDamageable damageable = targetIdentity.GetComponentInParent<IDamageable>();
        //    Debug.Log("Damageable gotten: " + damageable);
        //    if (damageable != null)
        //    {
        //        damageable.ApplyDamage(activeWeapon.damageToEnemy);
        //    }

        //    if (Physics.Raycast(origin, direction, out RaycastHit hit, activeWeapon.range))
        //    {
        //        RpcPlayShootEffect(hit.point, hit.normal, muzzlePos);
        //    }
        //}
        //else
        //{
        //    Debug.LogWarning($"[SERVER] No se encontró el objeto con netId {targetNetId}");
        //}
        //IDamageable damageable = target.GetComponentInParent<IDamageable>();
        //Debug.Log("Damageable gotten " + damageable);
        //Debug.Log($"[SERVER] Raycast hit: {target.name}, damageable: {target.GetComponent<IDamageable>() != null}");

        //if (damageable != null)
        //{
        //    damageable.ApplyDamage(activeWeapon.damageToEnemy);
        //}
        //if (Physics.Raycast(origin, direction, out RaycastHit hit, activeWeapon.range))
        //{
        //    RpcPlayShootEffect(hit.point, hit.normal, muzzlePos);
        //}

    }
   

    [ClientRpc]
    void RpcPlayShootEffect(Vector3 hitPoint, Vector3 hitNormal, Vector3 muzzlePos)
    {
        if (bulletHole == null)
        {
            Debug.LogWarning("[CLIENT] bulletHole es null en RpcPlayShootEffect");
            return;
        }

        if (bulletHole != null)
        {
            GameObject impact = Instantiate(bulletHole, hitPoint, Quaternion.LookRotation(hitNormal));
            Instantiate(muzzleFlash, activeWeapon.firePos.position, Quaternion.identity);
            Destroy(impact, 2f);
        }

        if (muzzleFlash == null)
        {
            Debug.LogWarning("[CLIENT] muzzleFlash es null en RpcPlayShootEffect");
            return;
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

        //// 🔧 Fuerza actualización de UI remota
        //OnNameChange("", playerName);
        //OnColorChange(Color.black, playerColor);
        ////OnHealthChanged(0, healthSync);
        ////OnWeaponChanged(-1, activeWeaponSync);
    }
   
    public override void OnStartServer()
    {
        name = $"Player[{netId}|server]";
        Debug.Log("OnStartServer: " + name);

        currentHealth = maxHealth;
        healthSync = (int)currentHealth;
        scScript = GameObject.FindFirstObjectByType<SceneScript>();
         base.OnStartServer();

    if (activeWeapon == null && activeWeaponSync >= 0 && activeWeaponSync < weapons.Length)
        {
            // Aquí deberías asignar correctamente la referencia del arma
            activeWeapon = weapons[activeWeaponSync].GetComponent<Weapon>();
            Debug.Log($"[SERVER] {gameObject.name} asignó su arma en OnStartServer: {activeWeapon}");
    }

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
        healthSync = (int)maxHealth;

        UpdateHealthUI((int)currentHealth);
        txtHealthTag.text = ((int)currentHealth).ToString();

        readyToShoot = true;

        string _name = "player_" + UnityEngine.Random.Range(100, 999);
        Color _color = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));

        CmdSetupPlayer(_name, _color);
        CmdSendPlayerMessage(_name);

        foreach (GameObject weapon in weapons)
        {
            weapon.SetActive(false);
        }
        //if (isLocalPlayer)
        //{
        bulletsLeftPerWeapon = new float[weapons.Length];
        for (int i = 0; i < weapons.Length; i++)
        {
            Weapon w = weapons[i].GetComponent<Weapon>();
            bulletsLeftPerWeapon[i] = w.magSize;
        }
        //}

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
    private void LateUpdate()
    {
        if (floating != null && cam != null)
            floating.transform.LookAt(cam.transform);
    }

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

        Vector3 rayOrigin = cam.transform.position;
        Vector3 rayDirection = cam.transform.forward + new Vector3(x, y, 0);

        if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit rayHit, activeWeapon.range))
        {
            Debug.DrawRay(rayOrigin, rayDirection * rayHit.distance, Color.green, 1f);

            NetworkIdentity targetIdentity = rayHit.transform.GetComponentInParent<NetworkIdentity>();
            uint targetNetId = targetIdentity != null ? targetIdentity.netId : 0;

            CmdShootRay(targetNetId, rayOrigin, rayDirection, muzzlePos);
            //if (ni != null)
            //{
            //    CmdShootRay(ni.netId, rayOrigin, rayDirection, muzzlePos);
            //}
            //Debug.Log("Oyeme, esto ocurre? " + rayHit.collider.gameObject.name);
            //CmdShootRay(rayHit.collider.gameObject, rayOrigin, rayDirection, muzzlePos);

            //Quaternion hitRotation = Quaternion.LookRotation(rayHit.normal);
            //GameObject hole = Instantiate(bulletHole, rayHit.point, hitRotation);
            //AudioSource.PlayClipAtPoint(hitSound, rayHit.point);
            //Destroy(hole, 10f);

        }
        else
        {
            Debug.DrawLine(rayOrigin, rayDirection * activeWeapon.range, Color.red, 1f);
            //Vector3 fallbackHit = cam.transform.position + cam.transform.forward * activeWeapon.range;
            CmdShootRay(0,rayOrigin, rayDirection, muzzlePos);
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

    #region VIDA

    public void ApplyDamage(float amount)
    {
        if (!isServer) return;
    
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        healthSync = (int)currentHealth;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            //CmdUpdateHealth((int)currentHealth);
            Die();
        }
        //healthSync = (int)currentHealth;


        //CmdUpdateHealth((int)currentHealth);
       
        Debug.Log($"[SERVER] {playerName} recibió daño. Vida: {currentHealth}");
        RpcUpdateHealth((int)currentHealth);
        //ShowHitEffects();
        //UpdateHealthUI(currentHealth);
        
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
    #endregion 
    #region Death
    void Die()
    {
        RpcPlayDeathEffect();
        print("Morido");
        //LOGICA RESPAWN
        RpcHandleDeath();
        StartCoroutine(RespawnRoutine());
    }
    [Server]
    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(1f); // Tiempo de espera antes del respawn

        Vector3 spawnPos = NetworkManager.singleton.GetStartPosition().position;
        transform.position = spawnPos;
        controller.enabled = false; // desactivar para poder mover el transform
        transform.position = spawnPos;
        controller.enabled = true;

        currentHealth = maxHealth;
        healthSync = (int)maxHealth;
        capsula.SetActive(true);

        RpcHandleRespawn(spawnPos);
    }
    [ClientRpc]
    void RpcHandleDeath()
    {
        if (isLocalPlayer)
        {
            Debug.Log("¡Has muerto!");
            // Aquí puedes desactivar el control del jugador, mostrar una pantalla de muerte, etc.
            controller.enabled = false;
            cam.gameObject.SetActive(false);
            enabled = false;
        }
        // Desactiva el render del cuerpo o muestra un efecto de muerte
        capsula.SetActive(false);
    }
    [ClientRpc]
    void RpcHandleRespawn(Vector3 spawnPosition)
    {
        if (isLocalPlayer)
        {
            Debug.Log("¡Has respawneado!");
            controller.enabled = false;
            transform.position = spawnPosition;
            controller.enabled = true;
            cam.gameObject.SetActive(true);
            enabled = true;
        }

        capsula.SetActive(true);
    }
    [ClientRpc]
    void RpcPlayDeathEffect()
    {
        //if (deathEffect != null)
          //  Instantiate(deathEffect, transform.position, Quaternion.identity);
    }

    void ShowHitEffects()
    {
        // Aquí puedes poner sonido, pantalla roja, etc.
        if (isLocalPlayer)
        {
            Debug.Log("Recibiste daño");
        }
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
    
    #endregion

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

    #endregion
}
