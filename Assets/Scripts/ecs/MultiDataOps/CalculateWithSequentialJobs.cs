using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class CalculateWithSequentialJobs : ICalculate
{
    public long Calculate(Dictionary<int, NativeArray<int>> map)
    {

        NativeArray<long> result = new NativeArray<long>(1, Allocator.TempJob);


        long sum = 0;

        // Iterate all items in the map

        foreach (var item in map)
        {
            NativeArray<int> array = item.Value;

            var handle = new CalculateJob()
            {
                arr = array,
                output = result
            }.Schedule();

            handle.Complete();
            

            sum += result[0];
        }


        result.Dispose();

        return sum;
    }

    public long CalculateLateUpdate()
    {
        return -1;
    }

    public string getDescription()
    {
        return "CalculateWithSequentialJobs";
    }
}



[Unity.Burst.BurstCompile]
public struct CalculateJob : IJob
{
    public NativeArray<int> arr;
    public NativeArray<long> output;

    public void Execute()
    {
        long sum = 0;

        for (int i = 0; i < arr.Length; i++)
        {
            sum = sum + arr[i] + 1;
        }

        output[0] = sum;
    }
}