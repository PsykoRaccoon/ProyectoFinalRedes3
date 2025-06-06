using UnityEngine;

public class Weapon : MonoBehaviour
{
    public float speed = 15f, damageToEnemy = 3f, cooldown = 1f, magSize= 10,
        bulletsPerShot = 1f, timeBetweenEachShot = 0.05f, spread = 0.5f, range = 20f,
        reloadTime = 1.5f, timeBetweenEachBullet = 0.05f; 
    

    public Transform firePos;

    override public string ToString()
    {
        string info = "";

        info += "speed: " + speed;
        info += "\ndamageToEnemy: " + damageToEnemy;
        info += "\ncooldown" + cooldown;
        info += "\nmagSize" + magSize;
        info += "\nbulletsPerShot: " + bulletsPerShot;
        info += "\ntimeBetweenEachBullet: " + timeBetweenEachBullet;
        info += "\nspread" + spread;
        info += "\nrange" + range;
        info += "\nrange" + range;
        info += "\nreloadTime" + reloadTime;
        info += "\ntimeBetweenEachShot" + timeBetweenEachShot;

        return info;
    }
}
