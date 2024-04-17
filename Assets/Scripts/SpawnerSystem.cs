using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Unity.Transforms;
using Unity.Burst;


public partial struct SpawnerSystem : ISystem
{
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<Spawner>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {

        var spawner = SystemAPI.GetSingletonRW<Spawner>();
        var config = SystemAPI.GetSingleton<Config>();
        var random = new Random((uint)UnityEngine.Random.Range(1, int.MaxValue));

        if (spawner.ValueRO.currentBulletCount < config.maxBullets) {
            // HACK: sometimes two or more bullets can hit the same enemy at the same time
            // in which case all bullets and the single enemy get destroyed
            // whcih results in bullet numbers reducing faster than enemy numbers.
            // This happens because I dont yet mark enemies are "hit" to avoide multiple bullets
            // hitting the same enemy.  
            if (spawner.ValueRO.currentBulletCount <= spawner.ValueRO.currentEnemyCount) {
                spawner.ValueRW.currentBulletCount += config.defaultBulletSpawnCount;
                SpawnBullets(state.EntityManager, config, ref random);
            }
        }

        if (spawner.ValueRO.currentEnemyCount < config.maxEnemies) {
            // just in case... to keep the enemy and bullet counts in sync
            if (spawner.ValueRO.currentEnemyCount <= spawner.ValueRO.currentBulletCount) {
                spawner.ValueRW.currentEnemyCount += config.defaultEnemySpawnCount;
                SpawnEnemies(state.EntityManager, config, ref random);
            }
        }
    }

    [BurstCompile]
    private void SpawnBullets(EntityManager em, Config config, ref Random random) {
        var bullets = em.Instantiate(config.bulletPrefab, config.defaultBulletSpawnCount, Allocator.Temp);

        foreach (var bullet in bullets) {
            var dir = new float3(random.NextFloat2Direction(), 0f);

            em.SetComponentData(bullet, new Bullet {
                direction = dir,
                speed = config.defaultBulletSpeed,
            });
        }
    }

    [BurstCompile]
    private void SpawnEnemies(EntityManager em, Config config, ref Random random) {
        var enemies = em.Instantiate(config.enemyPrefab, config.defaultEnemySpawnCount, Allocator.Temp);
        foreach (var enemy in enemies) {
            var transform = em.GetComponentData<LocalTransform>(enemy);

            var dir = new float3(random.NextFloat2Direction(), 0f);
            var pos = dir * config.spawnAreaRadius;

            transform.Position = pos;
            em.SetComponentData(enemy, transform);
            em.SetComponentData(enemy, new Enemy {
                direction = -dir,
                speed = config.defaultEnemySpeed,
            });
        }
    }
}