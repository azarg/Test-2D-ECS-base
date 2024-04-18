using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateBefore(typeof(BulletSystem))]
public partial struct EnemySystem : ISystem
{
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<Spawner>();
    }
    
    public void OnUpdate(ref SystemState state) {
        var config = SystemAPI.GetSingleton<Config>();
        new EnemyMoveJob {
            deltaTime = SystemAPI.Time.DeltaTime,
            spawnAreaRadius = config.spawnAreaRadius,
            addSorting = config.addSorting,
        }.ScheduleParallel();
        state.Dependency.Complete();
    }
}

[BurstCompile]
public partial struct EnemyMoveJob : IJobEntity
{
    [ReadOnly] public float deltaTime;
    [ReadOnly] public float spawnAreaRadius;
    [ReadOnly] public bool addSorting;

    public void Execute(ref LocalTransform transform, ref Enemy enemy) {
        float3 pos = transform.Position;

        pos += deltaTime * enemy.speed * enemy.direction;
        if (math.length(pos) > spawnAreaRadius) {
            pos = enemy.direction * spawnAreaRadius;
            enemy.direction = -enemy.direction;
        }

        // set z position to a multiple of y to achieve sort order on y axis
        if (addSorting) {
            pos.z = pos.y * 0.01f;
        }
        transform.Position = pos;
    }
}
