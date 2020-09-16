using JetBrains.Annotations;
using System.Collections.Generic;
using System.Diagnostics;
using System.Timers;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Profiling;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class TestMultiData : MonoBehaviour
{

    Dictionary<int, NativeArray<int>> map = new Dictionary<int, NativeArray<int>>();

    readonly public static int ArrayLength = 10000;
    readonly public static int NumberOfArrays = 200;



    NativeArray<ulong> _pointers;
    NativeArray<int> _len;
    NativeArray<long> _subSums;

    NativeArray<long> _totalSum;

    private JobHandle handle;
    private bool pendingJob;


    ProfilerMarker marker1 = new ProfilerMarker("m1");
    ProfilerMarker marker2 = new ProfilerMarker("m2");
    ProfilerMarker marker3 = new ProfilerMarker("m3");

    // Start is called before the first frame update
    unsafe void Start()
    {
    

        // Simple initialzation
        
        for (int i = 0; i < NumberOfArrays; i++)
        {
            

            NativeArray<int> arr = new NativeArray<int>(ArrayLength, Allocator.Persistent);
            for (int j = 0; j < ArrayLength; j++)
            {
                arr[j] = i;
            }

            map.Add(i, arr);
        }

        
    }

    [ExecuteAlways]
    // Update is called once per frame
    void Update()
    {


        bool high = Stopwatch.IsHighResolution;
        long freq = Stopwatch.Frequency;
        Stopwatch stopwatch = Stopwatch.StartNew();

        long sum = 0;

        long elapsed = stopwatch.ElapsedTicks;

        //marker1.Begin();
        //sum = TestNoJobMulti(map);
        //marker1.End();

        //elapsed = stopwatch.ElapsedTicks - elapsed;

        //Debug.Log($"No Job Time {elapsed} {sum} {high} {freq}");

        elapsed = stopwatch.ElapsedTicks;
        marker2.Begin();
        //sum = TestJobWithMultiNativeArray(map);
        marker2.End();

        elapsed = stopwatch.ElapsedTicks - elapsed;
        //Debug.Log($"Job Time {elapsed} {sum} {high} {freq}");

        marker3.Begin();

        handle = TestJobWithMultiNativeArrayDeferredCompletion(map);

        marker3.End();

        NativeArray<int> val;
        if (map.TryGetValue(2, out val))
        {


            //testPtrs(val.GetUnsafePtr<int>());

            //testPtrInJob(val.GetUnsafePtr<int>());





            //void* a = val.GetUnsafePtr<int>();

            //ulong lp = (ulong)a;

            //NativeArray<ulong> ptrs = new NativeArray<ulong>(1, Allocator.TempJob);
            //ptrs[0] = lp;
            //testPtrInJob2(ptrs);

            //ptrs.Dispose();

            
        }
    }






    public long TestNoJobMulti(Dictionary<int, NativeArray<int>> map)
    {
        // Run Through a loop and add up all the values for all the items

        long sum = 0;

        for (int i = 0; i < NumberOfArrays; i++)
        {
            NativeArray<int> array;
            map.TryGetValue(i, out array);

            for (int j = 0; j < array.Length; j++)
            {
                sum = sum + array[j] + 1;


            }
        }
        return sum;
    }



    public void TestNoJob(NativeArray<int> vals)
    {

        bool high = Stopwatch.IsHighResolution;
        long freq = Stopwatch.Frequency;
        Stopwatch stopwatch = Stopwatch.StartNew();
        
        
        long elapsed = stopwatch.ElapsedTicks;
        
        long sum = CalculateIncAndSum(vals);

        elapsed = stopwatch.ElapsedTicks - elapsed;

        //Debug.Log($"No Job Time {elapsed} {sum} {high} {freq}");

    }


    unsafe public void TestWithJob(NativeArray<int> vals)
    {
        bool high = Stopwatch.IsHighResolution;
        long freq = Stopwatch.Frequency;
        Stopwatch stopwatch = Stopwatch.StartNew();


        long elapsed = stopwatch.ElapsedTicks;

        long sum = CalculateIncAndSumWithJob(vals);


        elapsed = stopwatch.ElapsedTicks - elapsed;

        //Debug.Log($"Job Time {elapsed} {sum} {high} {freq}");
    }


    unsafe public long TestJobWithMultiNativeArray(Dictionary<int, NativeArray<int>> map)
    {

        NativeArray<ulong> pointers = new NativeArray<ulong>(map.Count, Allocator.TempJob);
        NativeArray<int> len = new NativeArray<int>(ArrayLength, Allocator.TempJob);
        NativeArray<long> subSums = new NativeArray<long>(map.Count, Allocator.TempJob);

        NativeArray<long> totalSum = new NativeArray<long>(1, Allocator.TempJob);


        // hopefully this is fast for what I need
        int i = 0;
        foreach(var item in map)
        {
            NativeArray<int> array = item.Value;
            void* ptr = array.GetUnsafePtr();
            pointers[i++] = (ulong)ptr;
           
        }

        JobHandle handle1 = new calculateMultiJob
        {
            longPointers = pointers,
            arrayLength = len,
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


    unsafe public JobHandle TestJobWithMultiNativeArrayDeferredCompletion(Dictionary<int, NativeArray<int>> map)
    {

        _pointers = new NativeArray<ulong>(map.Count, Allocator.TempJob);
        _len = new NativeArray<int>(ArrayLength, Allocator.TempJob);
        _subSums = new NativeArray<long>(map.Count, Allocator.TempJob);
        _totalSum = new NativeArray<long>(1, Allocator.TempJob);


        // hopefully this is fast for what I need
        int i = 0;
        foreach (var item in map)
        {
            NativeArray<int> array = item.Value;
            void* ptr = array.GetUnsafePtr();
            _pointers[i++] = (ulong)ptr;

        }

        JobHandle handle1 = new calculateMultiJob
        {
            longPointers = _pointers,
            arrayLength = _len,
            subSums = _subSums
        }.Schedule(map.Count, 1);


        JobHandle handle2 = new sumUpArrayVals
        {
            subSums = _subSums,
            output = _totalSum
        }.Schedule(handle1);

        pendingJob = true;
        return handle2;

    }


    public void LateUpdate()
    {

        //if (pendingJob)
        //{

        // If possible this should either go into a yeild pattern or complete in lateUpdate


        // I know that the handle should be executing every frame

        //while (handle.IsCompleted() == false)
        //{
        //    yield

        //}
        //{

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
            _len.Dispose();
            _subSums.Dispose();
            _totalSum.Dispose();

            Debug.Log($"LateComplete Sum {sum}");




            pendingJob = false;
            //}
            //}


            // return sum;
        }

    }




    public long CalculateIncAndSum(NativeArray<int> vals)
    {
        long sum = 0;

        for (int i = 0; i < vals.Length; i++)
        {
            sum = sum + vals[i] + 1;
        }

        return sum;
    }





    public long CalculateIncAndSumWithJob(NativeArray<int> vals)
    {
        NativeArray<long> summArr = new NativeArray<long>(1, Allocator.TempJob);

        CalculateJob job = new CalculateJob
        {
            arr = vals,
            output = summArr
        };

        JobHandle handle = job.Schedule();
        handle.Complete();

        long result = job.output[0];

        summArr.Dispose();

        return result;
    }








    public unsafe void testPtrInJob2(NativeArray<ulong> pointers)
    {

        NativeArray<int> arr = new NativeArray<int>(6, Allocator.TempJob);

        MyJob2 job = new MyJob2
        {
            ptrs = pointers,
            output = arr

        };

        JobHandle handle = job.Schedule();
        handle.Complete();


        Debug.LogFormat("VALUE of Native Array3 {0}", job.output[1]);


        arr.Dispose();
    }




    public unsafe void testPtrInJob( void* pointer)
    {

        NativeArray<int> arr = new NativeArray<int>(6, Allocator.TempJob);

        MyJob job = new MyJob
        {
            ptr = pointer,
            output = arr

        };

        JobHandle handle = job.Schedule();
        handle.Complete();


        Debug.LogFormat("VALUE of Native Array2 {0}", job.output[1]);


        arr.Dispose();
    }


    public unsafe void testPtrs( void* ptr)
    {


        NativeArray<int> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(ptr, 4, Allocator.None);
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.Create());

        Debug.LogFormat("VALUE of Native Array {0}", array[1]);

        //array.Dispose();

    }



    void OnDestroy()
    {

        for (int i = 0; i < NumberOfArrays; i++)
        {
            NativeArray<int> arr;
            if (map.TryGetValue(i, out arr))
            {

                arr.Dispose();
            }
        }

    }

}


[Unity.Burst.BurstCompile]
public unsafe struct calculateMultiJob : IJobParallelFor
{

    public NativeArray<ulong> longPointers;
    public NativeArray<int> arrayLength;  //  assume they are all the same for our purposes
    public NativeArray<long> subSums;


    public void Execute(int index)
    {
        

        // Yes this is unsafe but I can't pass in an Array of Native Arrays otherwise
        void* ptr = (void*)longPointers[index];

        NativeArray<int> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(ptr, arrayLength.Length, Allocator.None);
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.Create());

        long sum = 0;
        for (int i = 0; i < array.Length; i++)
        {
            sum = sum + array[i] + 1;
        }

        subSums[index] = sum;
    }
}

[Unity.Burst.BurstCompile]
public struct sumUpArrayVals : IJob
{
    public NativeArray<long> subSums;
    public NativeArray<long> output;
    public void Execute()
    {

        long sum = 0;
        for(int i = 0; i < subSums.Length; i++ )
        {
            sum += subSums[i];
        }

        output[0] = sum;
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





public struct MyJob2 : IJob
{
    [NativeDisableUnsafePtrRestriction]
    public unsafe NativeArray<ulong> ptrs;

    public NativeArray<int> output;



    public unsafe void Execute()
    {

        void* ptr = (void*)ptrs[0];

        NativeArray<int> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(ptr, 4, Allocator.None);
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.Create());

        output.CopyFrom(array);
    }
}




public struct MyJob : IJob
{
    [NativeDisableUnsafePtrRestriction]
    public unsafe void* ptr;

    public NativeArray<int> output;

    

    public unsafe void Execute()
    {
        NativeArray<int> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(ptr, 4, Allocator.None);
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.Create());

        output.CopyFrom(array);
    }
}


