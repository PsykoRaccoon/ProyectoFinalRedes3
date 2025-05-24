using UnityEngine;
using TMPro;

public class GunSystem : MonoBehaviour
{
    [Header("GunStats")]
    public int damage, magSize, bulletsPerTap;
    public float timeBetweenShooting, spread, range, reloadTime, timeBetweenShots;
    public bool allowButtonHold;
    int bulletsLeft, bulletsShot;

    bool shooting, readyToShoot, reloading;

    [Header("References")]
    public Camera fpsCam;
    public Transform attackPoint;
    public RaycastHit rayHit;
    public LayerMask enemy;


    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip hitSound;

    private AudioSource audioSource;

    [Header("Graphics")]
    public GameObject muzzleFlash, bulletHole;
    public TextMeshProUGUI text;

    private void Start()
    {
        bulletsLeft = magSize;
        readyToShoot = true;

        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        MyInput();
        text.SetText(bulletsLeft + "/" + magSize);
    }

    private void MyInput()
    {
        if (allowButtonHold)
        {
            shooting = Input.GetKeyDown(KeyCode.Mouse0);
        }
        else
        {
            shooting = Input.GetKeyDown(KeyCode.Mouse0);
        }

        if(Input.GetKeyDown(KeyCode.R) && bulletsLeft < magSize && !reloading)
        {
            Reload();
        }

        if(readyToShoot && shooting && !reloading && bulletsLeft > 0)
        {
            bulletsShot = bulletsPerTap;
            Shoot();
        }
    }

    private void Shoot()
    {
        float x = Random.Range(-spread, spread);
        float y = Random.Range(-spread, spread);
        Vector3 direction = fpsCam.transform.forward + new Vector3(x, y, 0);

        readyToShoot = false;
        audioSource.PlayOneShot(shootSound);

        if (Physics.Raycast(fpsCam.transform.position, direction, out rayHit, range, enemy))
        {
            Quaternion hitRotation = Quaternion.LookRotation(rayHit.normal);
            GameObject hole = Instantiate(bulletHole, rayHit.point, hitRotation);
            AudioSource.PlayClipAtPoint(hitSound, rayHit.point);
            Destroy(hole, 10f);

            PlayerHealth targetHealth = rayHit.collider.GetComponent<PlayerHealth>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
            }
        }

        Instantiate(muzzleFlash, attackPoint.position, Quaternion.identity);

        bulletsLeft--;
        bulletsShot--;

        Invoke("ResetShot", timeBetweenShooting);

        if (bulletsShot > 0 && bulletsLeft > 0)
        {
            Invoke("Shoot", timeBetweenShots);
        }
    }


    private void ResetShot()
    {
        readyToShoot = true;
    }

    private void Reload()
    {
        reloading = true;
        audioSource.PlayOneShot(reloadSound);
        Invoke("ReloadFinished", reloadTime);
    }

    private void ReloadFinished()
    {
        bulletsLeft = magSize;
        reloading = false;
    }

    
}
