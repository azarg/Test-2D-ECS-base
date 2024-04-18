using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
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
        
        // move bullets (and check collisions) in a parallel job

        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var job = new BulletMoveJob {
            usePooling = config.usePooling,
            addSorting = config.addSorting,
            physics = physics,
            deltaTime = SystemAPI.Time.DeltaTime,
            spawnAreaRadius = config.spawnAreaRadius,
            ecb = ecb.AsParallelWriter(),
        };
        job.ScheduleParallel();
        state.Dependency.Complete();

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
    [ReadOnly] public bool addSorting;

    public EntityCommandBuffer.ParallelWriter ecb;
    

    public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref LocalTransform transform, ref Bullet bullet) {
        float3 pos = transform.Position;

        // TODO: get radius from bullet component
        float radius = 0.5f;
        float dist = deltaTime * bullet.speed;

        // TODO: add collision filter so that only collisions with Enemies layer is checked
        // currently only enemies have a collider, therefore there is no need to add a collision filter

        var collision = physics.SphereCast(pos, radius, bullet.direction, dist, out ColliderCastHit hitInfo, CollisionFilter.Default);

        // If there is a collision destroy / disable bullet AND enemy,
        // otherwise just move the bullet

        // BIG TODO: instead of destroying / disabling bullet and enemy on collision check,
        // create a "HitContext" either as a new entity or in some array (struct, buffer).
        // Then query HitContext to decide if enemy and / or bullet should be deleted.

        if (collision) {
            if (usePooling) {
                ecb.SetEnabled(chunkIndex, entity, false);
                ecb.SetEnabled(chunkIndex, hitInfo.Entity, false);
            }
            else {
                ecb.DestroyEntity(chunkIndex, hitInfo.Entity);
                ecb.DestroyEntity(chunkIndex, entity);
            }
        }
        else {
            pos += dist * bullet.direction;
            if (math.length(pos) > spawnAreaRadius) {
                pos = bullet.direction * spawnAreaRadius;
                bullet.direction = -bullet.direction;
            }
            // set z position to a multiple of y to achieve sort order on y axis
            if (addSorting) {
                pos.z = pos.y * 0.01f;
            }
            transform.Position = pos;
        }
    }
}


