using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class CalculateSumNoJobs : ICalculate
{
    public long Calculate(Dictionary<int, NativeArray<int>> map)
    {


        long sum = 0;

        // Iterate all items in the map

        foreach(var item in map)
        {
            NativeArray<int> array = item.Value;

            for (int i = 0; i < array.Length; i++)
            {
                sum += array[i] + 1;
            }

        }

        return sum;
    }

    public long CalculateLateUpdate()
    {
        return -1;
    }

    public string getDescription()
    {
        return "CalculateSumNoJobs";
    }
}
