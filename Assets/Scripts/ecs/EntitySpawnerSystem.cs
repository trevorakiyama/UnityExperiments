
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Profiling;
using Unity.Entities;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEditor;
using Unity.Burst;
using UnityEngine;
using System;
using Unity.Entities.CodeGeneratedJobForEach;

namespace EntitySpawnerSystem
{
    [AlwaysUpdateSystem]
    public class EntitySpawnerSystem : SystemBase
    {

        private Unity.Mathematics.Random random;
        private NativeArray<Unity.Mathematics.Random> rArray;
        public float lastSpawnTime;

        Settings settings;



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


            settings = GameObject.FindObjectOfType<Settings>();

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

            EntityManager.AddComponentData<PrefabEntityExtraData>(PrefabEntitiesV2.preFabEntity,
              new PrefabEntityExtraData
              {
                  ttl = 0f,
                  velocity = new float3(0, 0, 0)
              });

            Entity preFabEntity = PrefabEntitiesV2.preFabEntity;

            
            // Command Buffer is needed to destroy entities.
            EndSimulationEntityCommandBufferSystem escbs = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            float gravity = 9.8f;
            float ttl = settings.ttl;
            float spawnRate = settings.spawnRate;
            int mode = settings.method;
            int destroyMode = settings.destroyMode;

            // Spawn entities based on Spawn Rate per second

            float currTime = Time.DeltaTime;
            lastSpawnTime += Time.DeltaTime;
            int toSpawn = (int)(spawnRate * lastSpawnTime);

            if (toSpawn >= 1)
            {
                lastSpawnTime = 0;

                // Method that Instantiates all Entities first then sets a random location for them
                // Fastest method of the four
                // Note:  The instantiation is still the most expensive operaton
                SpawnEntities(mode, preFabEntity, toSpawn, ttl);

            }


            // Apply Gravity
            ApplyGravityParallel(gravity, currTime);


            // Update the TTL and destroy if necessary
            CalculateTTLAndDestroy(destroyMode, escbs, currTime);
        }



        protected void SpawnEntities(int mode, Entity baseEntity, int numberToSpawn, float avgTtl)
        {

            if (mode == 1)
            {
                // Second fastest version that Instantiates entities then uses the returned array to set the translation one by one.
                // This might be faster with smaller numbers as it avoids the ForEach overhead, but it doesn't look like it.
                CreateEntitiesOneManagerSetComponentOutside(PrefabEntitiesV2.preFabEntity, numberToSpawn, avgTtl);
            } else if (mode == 2)
            {
                CreateEntitiesAndSetValueOnMainThread(PrefabEntitiesV2.preFabEntity, numberToSpawn, avgTtl);
            } else
            {
                // The slowest methods to spawn entities one at a time, like what might be done with GameObjects.
                // The instantiation is significantly slower than the other methods.
                CreateRandomEntitiesOneEntityManagerCall(PrefabEntitiesV2.preFabEntity, numberToSpawn, avgTtl);
            }
        }


        protected void CalculateTTLAndDestroy(int destroyMode, EndSimulationEntityCommandBufferSystem escbs, float deltaTime)
        {

            if (destroyMode == 0)
            {
                CalcTTLAndDestryWithECB(escbs, deltaTime);
            }
            else
            {

                CalcAndDestroyWithQueueAndJob(deltaTime);
            }
        }


        #region Spawning Methods

        protected void CreateRandomEntitiesOneEntityManagerCall(Entity baseEntity, int numberToSpawn, float avgTtl)
        {

            NativeArray<Entity> entityArray = EntityManager.Instantiate(baseEntity, numberToSpawn, Allocator.Temp);


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

                    extraData.ttl = avgTtl + rnd.NextFloat(0, 1f);

                }
                nativeRand[nativeThreadIndex] = rnd;

            }).ScheduleParallel();

            entityArray.Dispose();

        }


        protected void CreateEntitiesOneManagerSetComponentOutside(Entity baseEntity, int numberToSpawn, float avgTtl)
        {
            NativeArray<Entity> entityArray = new NativeArray<Entity>(numberToSpawn, Allocator.TempJob);

            EntityManager.Instantiate(baseEntity, entityArray);

            for (int i = 0; i < numberToSpawn; i++)
            {

                EntityManager.SetComponentData(entityArray[i],
                    new Translation { Value = new float3(random.NextFloat(-100f, 100f), random.NextFloat(-100f, 100f), random.NextFloat(200f, 400f)) });

                EntityManager.SetComponentData(entityArray[i],
                    new PrefabEntityExtraData { ttl = avgTtl });
            }

            entityArray.Dispose();
        }


        protected void CreateEntitiesAndSetValueOnMainThread(Entity baseEntity, int numberToSpawn, float avgTtl)
        {
            for (int i = 0; i < numberToSpawn; i++)
            {

                Entity spawnedEntity = EntityManager.Instantiate(baseEntity);

                EntityManager.SetComponentData(spawnedEntity,
                    new Translation { Value = new float3(random.NextFloat(-100f, 100f), random.NextFloat(-100f, 100f), random.NextFloat(200f, 400f)) });
                    
                EntityManager.SetComponentData(spawnedEntity,
                    new PrefabEntityExtraData { ttl = 1, velocity = new float3(0, 0, 0), expired = false });

            }
        }

        #endregion

        protected void ApplyGravityParallel(float gravity, float deltaTime)
        {
            Entities.ForEach((ref Translation translation, ref PrefabEntityExtraData extraData) =>
            {
                extraData.velocity.y = extraData.velocity.y - gravity * 10 * deltaTime;

                translation.Value = translation.Value + deltaTime * extraData.velocity;

            }).ScheduleParallel();

        }



        #region Destroy Methods
        protected void CalcTTLAndDestryWithECB(EndSimulationEntityCommandBufferSystem escbs,  float deltaTime)
        {

            EntityCommandBuffer.Concurrent buf = escbs.CreateCommandBuffer().ToConcurrent();

            // Try the EntityCommandBuffer to destroy entities.
            Entities.ForEach((Entity entity, int entityInQueryIndex, ref PrefabEntityExtraData extraData) =>
            {
                extraData.ttl -= deltaTime;

                if (extraData.ttl < 0)
                {
                    buf.DestroyEntity(entityInQueryIndex, entity);

                }
            }).Schedule();

            // Add dependency

            escbs.AddJobHandleForProducer(this.Dependency);
        }


        protected void CalcAndDestroyWithQueueAndJob(float deltaTime)
        {

            //NativeList<Entity> expired = new NativeList<Entity>(0, Allocator.TempJob);
            NativeQueue<Entity> queue = new NativeQueue<Entity>(Allocator.TempJob);

            NativeQueue<Entity>.ParallelWriter queuep = queue.AsParallelWriter();


            //NativeArray<Entity> expired = new NativeArray<Entity>(0, Allocator.TempJob);

            NativeList<Entity> expired = new NativeList<Entity>(0, Allocator.TempJob);


            JobHandle jobHandle = Entities.ForEach((Entity entity, int entityInQueryIndex, ref PrefabEntityExtraData extraData) =>
            {
                extraData.ttl -= deltaTime;

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


            var output = myJob.output.AsArray();


            EntityManager.DestroyEntity(output);


            expired.Dispose();
            queue.Dispose();
        }

        #endregion



        [BurstCompile]
        public struct FindExpiredEntities : IJob
        {
            public NativeQueue<Entity> input;
            public NativeList<Entity> output;

            void IJob.Execute()
            {
                //NativeList<Entity> tempList = new NativeList<Entity>(100, Allocator.Temp);

                for (int i = 0; i < input.Count; i++)
                {
                    output.Add(input.Dequeue());
                }

                //output.CopyFrom(tempList);
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








}