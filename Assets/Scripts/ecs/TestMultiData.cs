using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Profiling;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class TestMultiData : MonoBehaviour
{

    ICalculate calculateMethod1;


    Dictionary<int, NativeArray<int>> map = new Dictionary<int, NativeArray<int>>();

    public int ArrayLength = 1000;
    public int NumberOfArrays = 1000;

    ProfilerMarker marker3 = new ProfilerMarker("m3");

    ProfilerMarker markerCalculate = new ProfilerMarker("CalcCalc");
    ProfilerMarker markerLateUpdate = new ProfilerMarker("CalcLate");



    private void Awake()
    {

        //calculateMethod1 = GetComponent<ICalculate>();
        calculateMethod1 = new CalculateMultiSumInUpdate();
    }



    // Start is called before the first frame update
    unsafe void Start()
    {
    

        // Simple initialzation
        for (int i = 0; i < NumberOfArrays; i++)
        {
            NativeArray<int> arr = new NativeArray<int>(ArrayLength, Allocator.Persistent);
            for (int j = 0; j < ArrayLength; j++)
            {
                arr[j] = i + j;
            }

            map.Add(i, arr);
        }
    }

    void Update()
    {

        Stopwatch stopwatch = Stopwatch.StartNew();

        long sum = 0;

        marker3.Begin();

        markerCalculate.Begin();

        long elapsed = stopwatch.ElapsedTicks;
        sum = calculateMethod1.Calculate(map);
        elapsed = stopwatch.ElapsedTicks - elapsed;

        markerCalculate.End();


        Debug.Log($"{calculateMethod1.getDescription()} : Sum at Update = {sum}");



        marker3.End();
    }



    public void LateUpdate()
    {
        markerLateUpdate.Begin();
        long sum = calculateMethod1.CalculateLateUpdate();
        markerLateUpdate.End();

        Debug.Log($"{calculateMethod1.getDescription()} : Sum at LateUpdate = {sum}");


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



public interface ICalculate
{

    long Calculate(Dictionary<int, NativeArray<int>> map);

    long CalculateLateUpdate();

    string getDescription();

}