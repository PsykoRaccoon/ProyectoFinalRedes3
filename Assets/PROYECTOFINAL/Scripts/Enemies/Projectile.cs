using UnityEngine;
using Mirror;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 5f;
    public int damage = 10;

    private Vector3 direction;

    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;
    }

    void Start()
    {
        Destroy(gameObject, lifeTime); // Auto-destruir
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    [ServerCallback]
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Aquí puedes aplicar daño con tu sistema
            // Ej: other.GetComponent<PlayerHealth>()?.TakeDamage(damage);

            //NetworkServer.Destroy(gameObject);
        }
    }
}
