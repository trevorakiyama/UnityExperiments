using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

public class CalculateSumWithDeferredCompletion : ICalculate
{



    NativeArray<ulong> _pointers;
    NativeArray<int> _lens;
    NativeArray<long> _subSums;

    NativeArray<long> _totalSum;

    private JobHandle handle;
    private bool pendingJob;




    unsafe public long Calculate(Dictionary<int, NativeArray<int>> map)
    {

        // This relies on being called in the main thread

        // trick the job system into using ulong as a holder for void*
        _pointers = new NativeArray<ulong>(map.Count, Allocator.TempJob);
        _subSums = new NativeArray<long>(map.Count, Allocator.TempJob);

        // Scalars seem to need to be in Native Structures
        _lens = new NativeArray<int>(map.Count, Allocator.TempJob);
        _totalSum = new NativeArray<long>(1, Allocator.TempJob);


        int i = 0;
        foreach (var item in map)
        {
            NativeArray<int> array = item.Value;
            _lens[i] = array.Length;
            void* ptr = array.GetUnsafePtr();
            _pointers[i++] = (ulong)ptr;
        }



        // create a job to calculate each Array in parallel
        JobHandle handle1 = new CalculateMultiJob
        {
            longPointers = _pointers,
            arrayLengths = _lens,
            subSums = _subSums  // subsums will be the output passed to next job
        }.Schedule(map.Count, 10);


        // Consolidate all the sub sums into one master sum and schedule
        JobHandle handle2 = new sumUpArrayVals
        {
            subSums = _subSums,
            output = _totalSum
        }.Schedule(handle1);

        pendingJob = true;
        handle = handle2;

        return -1;
    }



    public long CalculateLateUpdate()
    {

        // I know that the handle should be executing every frame
        // because of the way I've set up the tests
        if (pendingJob)
        {
            handle.Complete();
            //System.Threading.Thread.Sleep(1);
            long sum = _totalSum[0];

            if (sum == 0)
            {
                Debug.Log("SUM OF ZERO");
            }


            _pointers.Dispose();
            _lens.Dispose();
            _subSums.Dispose();
            _totalSum.Dispose();

            pendingJob = false;

            return sum;

        } else
        {
            return -1;
        }

    }

    public string getDescription()
    {
        return "CalculateSumWithDeferredCompletion";
    }
}

[BurstCompile]
public unsafe struct CalculateMultiJob : IJobParallelFor
{

    public NativeArray<ulong> longPointers;
    public NativeArray<int> arrayLengths;  //  assume they are all the same for our purposes
    public NativeArray<long> subSums;


    public void Execute(int index)
    {


        // Convert a ulong to a void* since NativeArrays can't hold pointers
        void* ptr = (void*)longPointers[index];

        // Convert the pointer to back to a NativeArray for burst
        NativeArray<int> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(ptr, arrayLengths[index], Allocator.None);
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.Create());
      

        long sum = 0;
        for (int i = 0; i < array.Length; i++)
        {
            sum += array[i] + 1;
        }

        subSums[index] = sum;
    }
}

[BurstCompile]
public struct sumUpArrayVals : IJob
{
    public NativeArray<long> subSums;
    public NativeArray<long> output;
    public void Execute()
    {

        long sum = 0;
        for (int i = 0; i < subSums.Length; i++)
        {
            sum += subSums[i];
        }

        output[0] = sum;
    }
}