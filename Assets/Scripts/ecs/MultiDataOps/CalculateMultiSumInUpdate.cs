using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

public class CalculateMultiSumInUpdate : ICalculate
{
    unsafe public long Calculate(Dictionary<int, NativeArray<int>> map)
    {
        NativeArray<ulong> pointers = new NativeArray<ulong>(map.Count, Allocator.TempJob);
        NativeArray<int> len = new NativeArray<int>(map.Count, Allocator.TempJob);
        NativeArray<long> subSums = new NativeArray<long>(map.Count, Allocator.TempJob);
        NativeArray<long> totalSum = new NativeArray<long>(1, Allocator.TempJob);


        // hopefully this is fast for what I need
        int i = 0;
        foreach (var item in map)
        {
            NativeArray<int> array = item.Value;
            len[i] = array.Length;
            void* ptr = array.GetUnsafePtr();
            pointers[i++] = (ulong)ptr;

        }


        
        JobHandle handle1 = new CalculateMultiJob
        {
            longPointers = pointers,
            arrayLengths = len,
            subSums = subSums
        }.Schedule(map.Count, 1);


        
        JobHandle handle2 = new sumUpArrayVals
        {
            subSums = subSums,
            output = totalSum
        }.Schedule(handle1);


        // If possible this should either go into a yeild pattern or complete in lateUpdate

        
        handle2.Complete();


        long sum = totalSum[0];


        pointers.Dispose();
        len.Dispose();
        subSums.Dispose();
        totalSum.Dispose();

        return sum;
    }

    public long CalculateLateUpdate()
    {
        return -1;
    }

    public string getDescription()
    {
        return "CalculateMultiSumInUpdate";
    }
}
