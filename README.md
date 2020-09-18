# UnityExperiments
Simple project to create moduels to help learn Unity features and figure out what they are doing


## Experiments:
Creating and destroying 1000s of prefab entities per frame.
Primarily driven by the [EntitySpawnerSystem.cs] (https://github.com/trevorakiyama/UnityExperiments/blob/master/Assets/Scripts/ecs/EntitySpawnerSystem.cs)

Experiments:
* Creating entities using naive one at a time approach in Mainline vs Creating entities by a batch
* Setting values individually vs Setting values with the Entities.ForEach
* Destroying Entities using EntityCommandBuffer vs Searching for Entities to expire and running a Job

Findings:

* Always try to make code Burst compatible and use it.   It speeds things up 10-100 
* Batch whenever possible.  Creating entities one at a time is VERY expensive  (Jumping from Burst to Managed each time)
* ForEach is also very powerful to taking advantage of batching
* Destroying Entities with the EntityCommandBuffer from a burst job seems to be faster than trying to destroy them in a batch from Entity Manager



