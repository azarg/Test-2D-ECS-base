using Unity.Entities;
using UnityEngine;

public class SpawnerAuthoring : MonoBehaviour
{
    public bool usePooling = true;
    public bool addSorting = true;

    public float spawnAreaRadius = 100f;
    public int maxBullets = 50000;
    public int maxEnemies = 50000;
    public int defaultBulletSpawnCount = 200;
    public int defaultEnemySpawnCount = 200;
    public float defaultBulletSpeed = 100f;
    public float defaultEnemySpeed = 10f;

    public RendererType useRenderer;

    public GameObject SpriteRendererBulletPrefab;
    public GameObject SpriteRendererEnemyPrefab;
    public GameObject Mesh3DBulletPrefab;
    public GameObject Mesh3DEnemyPrefab;
    public GameObject MeshQuadBulletPrefab;
    public GameObject MeshQuadEnemyPrefab;

    class Baker : Baker<SpawnerAuthoring>
    {
        public override void Bake(SpawnerAuthoring authoring) {
            var entity = GetEntity(TransformUsageFlags.None);
            Entity bulletPrefab, enemyPrefab;

            // Select prefab based on renderer choice
            switch (authoring.useRenderer) {
                case RendererType.SpriteRenderer:
                default:
                    bulletPrefab = GetEntity(authoring.SpriteRendererBulletPrefab, TransformUsageFlags.Dynamic);
                    enemyPrefab = GetEntity(authoring.SpriteRendererEnemyPrefab, TransformUsageFlags.Dynamic);
                    break;
                case RendererType.Mesh3D:
                    bulletPrefab = GetEntity(authoring.Mesh3DBulletPrefab, TransformUsageFlags.Dynamic);
                    enemyPrefab = GetEntity(authoring.Mesh3DEnemyPrefab, TransformUsageFlags.Dynamic);
                    break;
                case RendererType.MeshQuad:
                    bulletPrefab = GetEntity(authoring.MeshQuadBulletPrefab, TransformUsageFlags.Dynamic);
                    enemyPrefab = GetEntity(authoring.MeshQuadEnemyPrefab, TransformUsageFlags.Dynamic);
                    break;
            }
            AddComponent(entity, new Config {
                usePooling = authoring.usePooling,
                addSorting = authoring.addSorting,
                bulletPrefab = bulletPrefab,
                enemyPrefab = enemyPrefab,
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
public enum RendererType
{
    SpriteRenderer,
    Mesh3D,
    MeshQuad,
}

public struct Spawner : IComponentData
{
    public int currentBulletCount;
    public int currentEnemyCount;
}

public struct Config : IComponentData
{
    public bool usePooling;
    public bool addSorting;
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