using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;

public partial struct BulletSystem : ISystem
{
    EntityQuery bulletCountQuery;
    EntityQuery enemyCountQuery;
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<Spawner>();

        // queries for getting the count of enemies & bullets
        // TODO: there is probably a better way of doing this
        // than counting on every frame
        bulletCountQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Bullet>()
            .Build(ref state);
        enemyCountQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Enemy>()
            .Build(ref state);
    }

    public void OnUpdate(ref SystemState state) {
        
        var physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var config = SystemAPI.GetSingleton<Config>();
        
        // move bullets in a parallel job
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var job = new BulletMoveJob {
            usePooling = config.usePooling,
            physics = physics,
            deltaTime = SystemAPI.Time.DeltaTime,
            spawnAreaRadius = config.spawnAreaRadius,
            ecb = ecb.AsParallelWriter(),
        };
        job.ScheduleParallel();
        state.Dependency.Complete();
        

        // TODO: if we dont wait for the job to complete, does it mean
        // the entity command buffer is not played back, which in turn
        // means the forllowing count queries will not be accurate??
        //state.Dependency.Complete();

        // Update enemy and bullet counts, so that spawner can keep track of counts
        var spawner = SystemAPI.GetSingletonRW<Spawner>();
        spawner.ValueRW.currentBulletCount = bulletCountQuery.CalculateEntityCount(); 
        spawner.ValueRW.currentEnemyCount = enemyCountQuery.CalculateEntityCount();

        // Update UI
        var uiData = SystemAPI.ManagedAPI.GetSingleton<UIData>();
        uiData.infoText.text = $"Enemy count = {spawner.ValueRO.currentBulletCount} " +
            $"\nBullet count = {spawner.ValueRO.currentBulletCount}";
    }
}

[BurstCompile]
public partial struct BulletMoveJob : IJobEntity
{
    [ReadOnly] public PhysicsWorldSingleton physics;
    [ReadOnly] public float deltaTime;
    [ReadOnly] public float spawnAreaRadius;
    [ReadOnly] public bool usePooling;

    public EntityCommandBuffer.ParallelWriter ecb;
    

    public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref LocalTransform transform, ref Bullet bullet) {
        float3 pos = transform.Position;

        // sphere cast to find enemies on the bullet's path
        // currently only enemies have a collider, therefore there is no need to add a collision filter

        // TODO: calculate max distance from bullet's speed
        // TODO: get sphere radius from bullet component
        // TODO: add collision filter so that only collisions with Enemies layer is checked
        // BIG TODO: instead of destroying bullet and enemy on collision check, create a "HitContext"
        // either as a new entity or in some array (struct, buffer). Then query HitContext
        // to decide if enemy and / or bullet should be deleted.
        float maxDistance = 1f;
        float radius = 0.5f;
        if (physics.SphereCast(pos, radius, bullet.direction, maxDistance, out ColliderCastHit hitInfo, CollisionFilter.Default)) {
            if (usePooling) {
                ecb.SetEnabled(chunkIndex, entity, false);
                ecb.SetEnabled(chunkIndex, hitInfo.Entity, false);
            }
            else {
                ecb.DestroyEntity(chunkIndex, hitInfo.Entity);
                ecb.DestroyEntity(chunkIndex, entity);
            }
        }

        pos += deltaTime * bullet.speed * bullet.direction;
        if (math.length(pos) > spawnAreaRadius) {
            pos = bullet.direction * spawnAreaRadius;
            bullet.direction = -bullet.direction;
        }
        transform.Position = pos;
    }
}


