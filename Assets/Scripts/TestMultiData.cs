using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

public class TestMultiData : MonoBehaviour
{

    Dictionary<int, NativeArray<int>> map = new Dictionary<int, NativeArray<int>>();

    public int ArrayLength = 4;


    // Start is called before the first frame update
    unsafe void Start()
    {
    
        // Simple initialzation
        
        for (int i = 0; i < 4; i++)
        {
            

            NativeArray<int> arr = new NativeArray<int>(4, Allocator.Persistent);
            for (int j = 0; j < ArrayLength; j++)
            {
                arr[j] = i;


            }

            map.Add(i, arr);
        }
    }

    [ExecuteAlways]
    // Update is called once per frame
    unsafe void Update()
    {

        Debug.Log("HELLO");




        NativeArray<int> val;
        if (map.TryGetValue(2, out val))
        {
            testPtrs(val.GetUnsafePtr<int>());
        }

    }




    public unsafe void testPtrs( void* ptr)
    {


        NativeArray<int> array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(ptr, 4, Allocator.None);
        NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.Create());

        Debug.LogFormat("VALUE of Native Array {0}", array[1]);

        //array.Dispose();

    }




    private void OnDestroy()
    {
        for (int i = 0; i < 4; i++)
        {
            NativeArray<int> arr; 
                
            if (map.TryGetValue(i, out arr))
            {
                arr.Dispose();
            }
            
        }
    }
}



public struct myJob : IJob
{
    [NativeDisableUnsafePtrRestriction]
    readonly unsafe void* ptr;

    public unsafe void Execute()
    {
        
        throw new System.NotImplementedException();
    }
}


