
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Profiling;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEditor;
using Boo.Lang;
using UnityEditor.Build.Pipeline;

[AlwaysUpdateSystem]
public class EntitySpawnerSystem : SystemBase
{

    float spawnTimer;
    private Unity.Mathematics.Random random;
    public NativeArray<Unity.Mathematics.Random> rArray;
    public float lastSpawnTime;


    ProfilerMarker marker01 = new ProfilerMarker("m01");
    ProfilerMarker marker02 = new ProfilerMarker("m02");
    ProfilerMarker marker03 = new ProfilerMarker("m03");


    ProfilerMarker marker1 = new ProfilerMarker("m1");
    ProfilerMarker marker2 = new ProfilerMarker("m2");
    ProfilerMarker marker3 = new ProfilerMarker("m3");
    ProfilerMarker marker4 = new ProfilerMarker("m4");
    ProfilerMarker marker5 = new ProfilerMarker("m5");
    ProfilerMarker marker6 = new ProfilerMarker("m6");
    ProfilerMarker marker7 = new ProfilerMarker("m7");
    ProfilerMarker marker8 = new ProfilerMarker("m8");
    ProfilerMarker marker9 = new ProfilerMarker("m9");

    ProfilerMarker marker21 = new ProfilerMarker("m21");
    ProfilerMarker marker22 = new ProfilerMarker("m22");
    ProfilerMarker marker23 = new ProfilerMarker("m23");
    ProfilerMarker marker24 = new ProfilerMarker("m24");



    protected override void OnCreate()
    {
        base.OnCreate();

        random = new Unity.Mathematics.Random(56);

        var seed = new System.Random();
        var rArrayM = new Unity.Mathematics.Random[JobsUtility.MaxJobThreadCount];
        for (int i = 0; i < JobsUtility.MaxJobThreadCount; ++i)
            rArrayM[i] = new Unity.Mathematics.Random((uint)seed.Next());
        rArray = new NativeArray<Unity.Mathematics.Random>(rArrayM, Allocator.Persistent);


        lastSpawnTime = 0;


        
    }

    protected override void OnDestroy()
    {
        rArray.Dispose();
        base.OnDestroy();
    }



    // Run some tests to see what kind of differences there are from entity spawning techniques
    // As expected Burst operations in parallel get the biggest gains especially when manipulating
    // 1000s or more entities each frame

    protected override void OnUpdate()
    {

        float gravity = 98f;
        float terminal = 60;


        // Command Buffer is needed to destroy entities.
        EndSimulationEntityCommandBufferSystem escbs = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        EntityCommandBuffer.Concurrent buf = escbs.CreateCommandBuffer().ToConcurrent();

        float ttl = Settings.getTTL();
        float spawnRate = Settings.getSpawnRate();
        float count = Settings.getItemCount();
        
        // Spawn entities based on Spawn Rate per second


        float currTime = Time.DeltaTime;
        spawnTimer -= Time.DeltaTime;

        lastSpawnTime += Time.DeltaTime;

        int toSpawn = (int)(spawnRate * lastSpawnTime);

        if (toSpawn < 1 )
        {
            
        } 
        else
        {
            lastSpawnTime = 0;
         
            spawnTimer = .001f;

            EntityManager.AddComponentData<PrefabEntityExtraData>(PrefabEntitiesV2.prefabEntity,
                        new PrefabEntityExtraData { ttl = 0f,
                        velocity = new float3(0,0,0)});
            



            // Method that Instantiates all Entities first then sets a random location for them
            // Fastest method of the four
            // Note:  The instantiation is still the most expensive operaton
            if (Settings.getMethodType() == 0 || Settings.getMethodType() > 3)
            {

                marker1.Begin();

                marker3.Begin();

                //NativeArray<Entity> entityArray = new NativeArray<Entity>(toSpawn, Allocator.TempJob);


                NativeArray<Entity> entityArray = EntityManager.Instantiate(PrefabEntitiesV2.prefabEntity, toSpawn, Allocator.Temp);
                

                marker3.End();


                marker4.Begin();

                // The following is pretty fast, but still 0.05ms for 5000 new entities
                var nativeRand = rArray;
                Entities.ForEach((int nativeThreadIndex, ref Translation translation, ref PrefabEntityExtraData extraData) =>
                {

                    // Using some trickery to use the Unity.Mathematics.Random with Burst
                    var rnd = nativeRand[nativeThreadIndex];

                    if (translation.Value.x == 1000 && translation.Value.y == 0 && translation.Value.z == 0)
                    {

                        float x = rnd.NextFloat(-100, 100);
                        float y = rnd.NextFloat(50f, 100f);
                        float z = rnd.NextFloat(-100, 100);

                        extraData.velocity.x = x * 0.1f;
                        extraData.velocity.y = (y - 50f) * 0.5f;
                        extraData.velocity.z = z * 0.1f;

                        translation.Value.x = x;
                        translation.Value.y = y;
                        translation.Value.z = z + 300;

                        extraData.ttl = ttl + rnd.NextFloat(0, 1f);

                    }
                    nativeRand[nativeThreadIndex] = rnd;

                }).ScheduleParallel();

                entityArray.Dispose();
                marker4.End();

                marker1.End();




            }
            else if (Settings.getMethodType() == 1)
            {
                // Second fastest version that Instantiates entities then uses the returned array to set the translation one by one.
                // This might be faster with smaller numbers as it avoids the ForEach overhead, but it doesn't look like it.


                marker1.Begin();
                marker3.Begin();
                NativeArray<Entity> entityArray = new NativeArray<Entity>(toSpawn, Allocator.TempJob);

                EntityManager.Instantiate(PrefabEntitiesV2.prefabEntity, entityArray);

                marker3.End();
                marker4.Begin();



                for (int i = 0; i < toSpawn; i++)
                {

                    marker5.Begin();
                    EntityManager.SetComponentData(entityArray[i],
                        new Translation { Value = new float3(random.NextFloat(-100f, 100f), random.NextFloat(-100f, 100f), random.NextFloat(200f, 400f)) });

                    EntityManager.SetComponentData(entityArray[i],
                        new PrefabEntityExtraData { ttl = ttl });


                    marker5.End();



                }

                marker4.End();

                entityArray.Dispose();
                marker1.End();

            }
            else
            {
                // The slowest methods to spawn entities one at a time, like what might be done with GameObjects.
                // The instantiation is significantly slower than the other methods.

                marker01.Begin();
                for (int i = 0; i < toSpawn; i++)
                {

                    marker02.Begin();
                    Entity spawnedEntity = EntityManager.Instantiate(PrefabEntitiesV2.prefabEntity);
                    marker02.End();

                    marker03.Begin();

                    EntityManager.SetComponentData(spawnedEntity,
                        new Translation { Value = new float3(random.NextFloat(-100f, 100f), random.NextFloat(-100f, 100f), random.NextFloat(200f, 400f)) });

                    
                    marker03.End();

                }

                marker01.End();
            } 

        }

        marker6.Begin();


        // this might be bad performing but anyways, add time to each of the entities and if they are over 10 seconds old then destroy them
        Entities.ForEach((ref Translation translation, ref PrefabEntityExtraData extraData) =>
        {
            extraData.velocity.y = extraData.velocity.y - gravity * currTime; 

            translation.Value = translation.Value + currTime * extraData.velocity;

        }).ScheduleParallel();


        marker6.End();

        marker7.Begin();



        if (Settings.getMethodType() != 4)
        {
            // this might be bad performing but anyways, add time to each of the entities and if they are over 10 seconds old then destroy them
            // Requires the EntityCommandBuffer to destroy entities.
            Entities.ForEach((Entity entity, int entityInQueryIndex, ref PrefabEntityExtraData extraData) =>
            {
                extraData.ttl -= currTime;

                if (extraData.ttl < 0)
                {
                    buf.DestroyEntity(entityInQueryIndex, entity);

                }
            }).Schedule();




            // Add dependency

            escbs.AddJobHandleForProducer(this.Dependency);

        }
        else
        {
            // get the old Entities with a job
            // Parallel does not like Lists
            // This performs MUCH worse than the EntityCommandBuffer 100ms vs 5 ms for destroying thousands of entitis


            //NativeList<Entity> expired = new NativeList<Entity>(0, Allocator.TempJob);
            NativeQueue<Entity> queue = new NativeQueue<Entity>(Allocator.TempJob);

            NativeQueue<Entity>.ParallelWriter queuep = queue.AsParallelWriter();


            NativeList<Entity> expired = new NativeList<Entity>(0, Allocator.TempJob);





            JobHandle jobHandle = Entities.ForEach((Entity entity, int entityInQueryIndex, ref PrefabEntityExtraData extraData) =>
            {
                extraData.ttl -= currTime;

                if (extraData.ttl < 0)
                {
                    queuep.Enqueue(entity);
                }
            }).ScheduleParallel(Dependency);


            //jobHandle.Complete();

            FindExpiredEntities myJob = new FindExpiredEntities()
            {
                input = queue,
                output = expired
            };

            myJob.Schedule(jobHandle).Complete();


            marker8.Begin();

            EntityManager.DestroyEntity(expired.AsArray());

            expired.Dispose();
            queue.Dispose();


            marker8.End();



        }
        marker7.End();
        
    }

    public struct FindExpiredEntities : IJob
    {
        public NativeQueue<Entity> input;
        public NativeList<Entity> output;

        void IJob.Execute()
        {
            //NativeList<Entity> tempList = new NativeList<Entity>(256, Allocator.Temp);

            for (int i = 0; i < input.Count; i++)
            {
                output.Add(input.Dequeue());
            }
        }
    }




    // Simple Struct to add to the prefab for aging and ttl checks.
    public struct PrefabEntityExtraData : IComponentData
    {
        public float ttl;
        public float3 velocity;
        public bool expired;
    }

}
