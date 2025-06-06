using UnityEngine.AI;
using UnityEngine;
using System.Collections;
using Mirror;

public class MinionMovement : MonoBehaviour
{
    public NavMeshAgent agent;
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    [Header("Patrol")]
    public float patrolRadius;

    [Header("Attack & detection")]
    public float visionRange;
    public float attackRange;
    public float shootCooldown = 1.5f;

    private float lastShootTime;
    private Vector3 patrolPoint;
    private Transform targetPlayer;
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        SetNewPatrolPoint();
    }

    void Update()
    {
        UpdateTargetPlayer();

        if (targetPlayer == null)
        {
            Patrol();
            return;
        }
        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

        if (distanceToPlayer <= attackRange)
        {
            Attack();
        }
        else if (distanceToPlayer <= visionRange)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }
    void UpdateTargetPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float closestDistance = Mathf.Infinity;
        Transform closest = null;

        foreach (GameObject playerObj in players)
        {
            float dist = Vector3.Distance(transform.position, playerObj.transform.position);
            if (dist < closestDistance && dist <= visionRange)
            {
                closestDistance = dist;
                closest = playerObj.transform;
            }
        }

        targetPlayer = closest;
    }

    void Patrol()
    {
        if (!agent.hasPath || agent.remainingDistance < 1f)
        {
            SetNewPatrolPoint();
        }
    }

    void ChasePlayer()
    {
        agent.SetDestination(targetPlayer.position);
    }

    void Attack()
    {
        agent.ResetPath();


        Vector3 direction = (targetPlayer.position - transform.position).normalized;
        direction.y = 0f;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 3f);
        if (Time.time - lastShootTime >= shootCooldown)
        {
            Shoot(direction);
            lastShootTime = Time.time;
        }
    }

    void Shoot(Vector3 direction)
    {
        if (projectilePrefab == null || projectileSpawnPoint == null)
        {
            Debug.LogWarning("Projectile Prefab o Spawn Point no asignados.");
            return;
        }

        GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
        projectile.GetComponent<Projectile>()?.SetDirection(direction);
    }
    void SetNewPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
        {
            patrolPoint = hit.position;
            agent.SetDestination(patrolPoint);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}