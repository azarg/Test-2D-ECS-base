using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Burst;


public partial struct SpawnerSystem : ISystem
{
    NativeArray<Random> rngs;
    private int rngArraySize;

    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<Spawner>();

        rngArraySize = 200;
        rngs = new NativeArray<Random>(rngArraySize, Allocator.Persistent);

        for (int i = 0; i < rngs.Length; i++) {
            rngs[i] = new Random((uint)UnityEngine.Random.Range(1, int.MaxValue));
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {

        var spawner = SystemAPI.GetSingletonRW<Spawner>();
        var config = SystemAPI.GetSingleton<Config>();
        var random = new Random((uint)UnityEngine.Random.Range(1, int.MaxValue));

        //var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        //var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        if (spawner.ValueRO.currentBulletCount < config.maxBullets) {
            // HACK: sometimes two or more bullets can hit the same enemy at the same time
            // in which case all bullets and the single enemy get destroyed
            // whcih results in bullet numbers reducing faster than enemy numbers.
            // This happens because I dont yet mark enemies are "hit" to avoide multiple bullets
            // hitting the same enemy.  
            if (spawner.ValueRO.currentBulletCount <= spawner.ValueRO.currentEnemyCount) {
                spawner.ValueRW.currentBulletCount += config.defaultBulletSpawnCount;
                SpawnBullets(state.EntityManager, config, ref random);

                // TODO: look into using a parallel job for spawning
                // For now, it doesn't seem to provide performance benefit
                //new BulletSpawnJob {
                //    ecb = ecb.AsParallelWriter(),
                //    bulletPrefab = config.bulletPrefab,
                //    rngs = rngs,
                //    speed = config.defaultBulletSpeed,
                //}.Schedule(config.defaultBulletSpawnCount, 16).Complete();

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

/// <summary>
/// NOT USED. Didn't seem to provide performance benefit
/// </summary>
[BurstCompile]
public partial struct BulletSpawnJob : IJobParallelFor
{
    [ReadOnly] public Entity bulletPrefab;
    [ReadOnly] public float speed;
    public NativeArray<Random> rngs;
    public EntityCommandBuffer.ParallelWriter ecb;


    public void Execute(int index) {
        var entity = ecb.Instantiate(index, bulletPrefab);

        int rng_idx = index % rngs.Length;
        var rng= rngs[rng_idx];
        var dir = new float3(rng.NextFloat2Direction(), 0f);
        rngs[rng_idx] = rng;

        ecb.SetComponent<Bullet>(index, entity, new Bullet {
            direction = dir,
            speed = speed,
        });
    }
}