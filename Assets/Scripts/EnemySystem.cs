using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

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
        }.ScheduleParallel();
    }
}

[BurstCompile]
public partial struct EnemyMoveJob : IJobEntity
{
    public float deltaTime;
    public float spawnAreaRadius;

    public void Execute(ref LocalTransform transform, ref Enemy enemy) {
        float3 pos = transform.Position;

        pos += deltaTime * enemy.speed * enemy.direction;
        if (math.length(pos) > spawnAreaRadius) {
            pos = enemy.direction * spawnAreaRadius;
            enemy.direction = -enemy.direction;
        }
        transform.Position = pos;
    }
}
