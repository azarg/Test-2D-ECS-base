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

        // idea for chaning the z to achieve sorting based on y axis value
        // var doSort = true;
        // if (doSort) pos.z = 0;
        pos += deltaTime * enemy.speed * enemy.direction;
        if (math.length(pos) > spawnAreaRadius) {
            pos = enemy.direction * spawnAreaRadius;
            enemy.direction = -enemy.direction;
        }
        //if (doSort) pos.z = pos.y * 0.01f;
        transform.Position = pos;
    }
}
