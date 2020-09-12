
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
//using UnityEngine;
//using UnityEngine.Rendering;
using Unity.Profiling;
//using System;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
//using System.Threading;

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


            EntityManager.AddComponentData<ageComponent>(PrefabEntitiesV2.prefabEntity,
                        new ageComponent { age = 0f });


            // Method that Instantiates all Entities first then sets a random location for them
            // Fastest method of the four
            // Note:  The instantiation is still the most expensive operaton
            if (Settings.getMethodType() == 0 || Settings.getMethodType() > 3)
            {

                marker1.Begin();

                marker3.Begin();

                NativeArray<Entity> entityArray = new NativeArray<Entity>(toSpawn, Allocator.TempJob);


                EntityManager.Instantiate(PrefabEntitiesV2.prefabEntity, entityArray);

                marker3.End();


                marker4.Begin();

                // The following is pretty fast, but still 0.05ms for 5000 new entities
                var nativeRand = rArray;
                Entities.ForEach((int nativeThreadIndex, ref Translation translation) =>
                {

                    // Using some trickery to use the Unity.Mathematics.Random with Burst
                    var rnd = nativeRand[nativeThreadIndex];

                    if (translation.Value.x == 1000 && translation.Value.y == 0 && translation.Value.z == 0)
                    {

                        translation.Value = new float3(rnd.NextFloat(-100f, 100f), rnd.NextFloat(-100f, 100f), rnd.NextFloat(200f, 400f));
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
        Entities.ForEach((ref Translation translation) =>
        {
            translation.Value.y = translation.Value.y + currTime * -50;

        }).ScheduleParallel();


        marker6.End();

        marker7.Begin();

        // this might be bad performing but anyways, add time to each of the entities and if they are over 10 seconds old then destroy them
        // Requires the EntityCommandBuffer to destroy entities.
        Entities.ForEach((Entity entity, int entityInQueryIndex, ref ageComponent ageComp) =>
        {


            ageComp.age += currTime;

            if (ageComp.age > ttl)
            {
                buf.DestroyEntity(entityInQueryIndex, entity);
            }
        }).Schedule();
        marker7.End();

        // Add dependency
        escbs.AddJobHandleForProducer(this.Dependency);

    }


    // Simple Struct to add to the prefab for aging and ttl checks.
    public struct ageComponent : IComponentData
    {
        public float age;
    }

}
