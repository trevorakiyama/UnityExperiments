using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

public class CalculateSumWithSequentialPtrJobs : ICalculate
{
    public unsafe long Calculate(Dictionary<int, NativeArray<int>> map)
    {

        NativeArray<ulong> ptrArr = new NativeArray<ulong>(1, Allocator.TempJob);
        NativeArray<int> ptrArrSize = new NativeArray<int>(1, Allocator.TempJob);
        NativeArray<long> result = new NativeArray<long>(1, Allocator.TempJob);
        


        long sum = 0;

        // Iterate all items in the map

        foreach (var item in map)
        {

            // Convert to void* pointer and cast to ulong so it can be sent to a job
            NativeArray<int> array = item.Value;
            ulong ptr = (ulong)array.GetUnsafePtr();
            

            var handle = new CalculateJobPtr()
            {
                arrPtr = ptrArr,
                arrLen = ptrArrSize,
                output = result
            }.Schedule();

            handle.Complete();


            sum += result[0];
        }

        ptrArr.Dispose();
        ptrArrSize.Dispose();
        result.Dispose();

        return sum;
    }

    public long CalculateLateUpdate()
    {
        return -1;
    }

    public string getDescription()
    {
        return "CalculateWithSequentialPtrJobs";
    }
}



[Unity.Burst.BurstCompile(CompileSynchronously = true)]
public unsafe struct CalculateJobPtr : IJob
{
    public NativeArray<ulong> arrPtr;
    public NativeArray<int> arrLen;
    public NativeArray<long> output;

    public void Execute()
    {
        long sum = 0;

        // reconstruct the native array for the Burst Job
        void* ptr = (void*)arrPtr[0];
        NativeArray<int> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(ptr, arrLen[0], Allocator.None);
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.Create());


        for (int i = 0; i < arrLen[0]; i++)
        {
            sum = sum + array[i] + 1;
        }

        output[0] = sum;
    }
}