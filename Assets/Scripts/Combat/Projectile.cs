using UnityEngine;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    public enum ProjectileType { Arrow, Magic, Special, SummonedArrow }

    [Header("Projectile Config")]
    public ProjectileType type = ProjectileType.Arrow;
    public float speed = 20f;
    public float damage = 10f;
    public float lifetime = 5f;
    public float gravity = -5f;
    public bool isHoming = false;
    public float homingStrength = 5f;

    [Header("Impact")]
    public GameObject impactEffect;
    public float impactRadius = 1f;

    private Vector3 direction;
    private Vector3 velocity;
    private GameObject owner;
    private Transform target;
    private float spawnTime;
    private float currentGravity;
    private bool isActive = false;
    private HashSet<GameObject> hitTargets = new();

    public static Projectile Spawn(Vector3 position, Vector3 dir, float dmg, ProjectileType projType, GameObject ownerObj = null)
    {
        ProjectileData data = projType switch
        {
            ProjectileType.Arrow => new ProjectileData { speed = 20f, gravity = -2f },
            ProjectileType.Magic => new ProjectileData { speed = 15f, gravity = 0f },
            ProjectileType.Special => new ProjectileData { speed = 25f, gravity = 0f },
            ProjectileType.SummonedArrow => new ProjectileData { speed = 30f, gravity = -2f },
            _ => new ProjectileData { speed = 15f, gravity = 0f }
        };

        GameObject obj = null;
        if (ObjectPool.Instance != null)
        {
            obj = ObjectPool.Instance.Spawn("Projectile", position, Quaternion.identity);
        }

        if (obj == null)
        {
            obj = new GameObject($"Projectile_{projType}");
            obj.transform.position = position;
        }

        Projectile proj = obj.GetComponent<Projectile>();
        if (proj == null) proj = obj.AddComponent<Projectile>();

        proj.direction = dir.normalized;
        proj.damage = dmg;
        proj.type = projType;
        proj.owner = ownerObj;
        proj.speed = data.speed;
        proj.gravity = data.gravity;
        proj.velocity = dir.normalized * data.speed;
        proj.spawnTime = Time.time;
        proj.currentGravity = data.gravity;
        proj.isActive = true;
        proj.hitTargets.Clear();

        obj.SetActive(true);
        return proj;
    }

    void Update()
    {
        if (!isActive) return;

        if (isHoming && target != null)
        {
            Vector3 targetDir = (target.position - transform.position).normalized;
            direction = Vector3.Slerp(direction, targetDir, homingStrength * Time.deltaTime);
        }

        velocity = direction * speed;
        velocity.y += currentGravity * Time.deltaTime;
        currentGravity += gravity * Time.deltaTime;

        transform.position += velocity * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        if (Time.time - spawnTime > lifetime)
        {
            DespawnProjectile();
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, impactRadius);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Enemy") || hit.CompareTag("Boss"))
            {
                if (hitTargets.Contains(hit.gameObject)) continue;
                hitTargets.Add(hit.gameObject);

                IDamageable dmg = hit.GetComponent<IDamageable>();
                dmg?.TakeDamage(damage);

                if (impactEffect != null)
                    Instantiate(impactEffect, transform.position, Quaternion.identity);

                DespawnProjectile();
                break;
            }
            else if (hit.CompareTag("Player") && owner != null && owner.CompareTag("Enemy"))
            {
                if (hitTargets.Contains(hit.gameObject)) continue;
                hitTargets.Add(hit.gameObject);

                IDamageable dmg = hit.GetComponent<IDamageable>();
                dmg?.TakeDamage(damage);
                DespawnProjectile();
                break;
            }
        }
    }

    void DespawnProjectile()
    {
        isActive = false;
        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.Despawn("Projectile", gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    struct ProjectileData { public float speed; public float gravity; }
}
