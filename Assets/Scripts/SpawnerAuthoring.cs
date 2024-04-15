using Unity.Entities;
using UnityEngine;

public class SpawnerAuthoring : MonoBehaviour
{
    public static int frameCounter;

    public float spawnAreaRadius = 100f;

    public int maxBullets = 1000;
    public int maxEnemies = 1000;
    public int defaultBulletSpawnCount = 30;
    public int defaultEnemySpawnCount = 30;
    public float defaultBulletSpeed = 30f;
    public float defaultEnemySpeed = 10f;

    public GameObject bulletPrefab;
    public GameObject enemyPrefab;

    class Baker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new Config {
                bulletPrefab = GetEntity(authoring.bulletPrefab, TransformUsageFlags.Dynamic),
                enemyPrefab = GetEntity(authoring.enemyPrefab, TransformUsageFlags.Dynamic),
                maxBullets = authoring.maxBullets,
                maxEnemies = authoring.maxEnemies,
                defaultBulletSpawnCount = authoring.defaultBulletSpawnCount,
                defaultEnemySpawnCount = authoring.defaultEnemySpawnCount,
                defaultBulletSpeed = authoring.defaultBulletSpeed,
                defaultEnemySpeed = authoring.defaultEnemySpeed,
                spawnAreaRadius = authoring.spawnAreaRadius,
            });
            AddComponent(entity, new Spawner { });
        }
    }
}

public struct Spawner : IComponentData
{
    public int currentBulletCount;
    public int currentEnemyCount;
}

public struct Config : IComponentData
{
    public Entity bulletPrefab;
    public Entity enemyPrefab;
    public int maxBullets;
    public int maxEnemies;
    public int defaultBulletSpawnCount;
    public int defaultEnemySpawnCount;
    public float defaultBulletSpeed;
    public float defaultEnemySpeed;
    public float spawnAreaRadius;
}