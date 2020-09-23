using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.Collections;
using System.Diagnostics;
using UnityEngine.Diagnostics;
using Unity.Profiling;

namespace Tests
{
    public class TestArrayAccessOptions
    {


        Dictionary<int, NativeArray<int>> map = new Dictionary<int, NativeArray<int>>();



       [SetUp]
        public void SetupData()
        {

            //Debug.Log("Calling Setup");

            //UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");

            //SceneManager.SetActiveScene(SceneManager.GetSceneAt(0));


            
        }



        private Dictionary<int, NativeArray<int>> CreateMap(int arraySize, int numArrays)
        {

            Dictionary<int, NativeArray<int>> map = new Dictionary<int, NativeArray<int>>();

            for (int i = 0; i < numArrays; i++)
            {
                NativeArray<int> arr = new NativeArray<int>(arraySize, Allocator.Persistent);
                for (int j = 0; j < arraySize; j++)
                {
                    arr[j] = i + j;
                }

                map.Add(i, arr);
            }

            return map;

        }


        private void DisposeMap(Dictionary<int, NativeArray<int>> map)
        {

            foreach (var item in map)
            {
                NativeArray<int> val = item.Value;

                val.Dispose();

            }
        }




        // A Test behaves as an ordinary method
        [Test]
        public void TestMultipleArrayCalculators()
        {


            // 100 Milliion should be good enough
            int arraySize = 100000;
            int arrayNum = 1000;


            ICalculate[] calcs = new ICalculate[4]
            {
                new CalculateSumNoJobs(),  // Not Using Jobs is brutally slow
                new CalculateSumWithSequentialPtrJobs(),  // this is currently broken and needs to be fixed
                new CalculateWithSequentialJobs(),
                new CalculateMultiSumInUpdate()
            };


            Dictionary<int, NativeArray<int>> map = CreateMap(arraySize, arrayNum);



            foreach (ICalculate calc in calcs)
            {
                (long elapsed, long result) = ExecutionTimer(calc, map);
                UnityEngine.Debug.Log($"{calc.getDescription()} : ExecutionTime: {elapsed} : result {result}");
            }


            DisposeMap(map);



            // Use the Assert class to test conditions
        }


        ProfilerMarker marker = new ProfilerMarker("M1");

        private (long, long) ExecutionTimer(ICalculate calc, Dictionary<int, NativeArray<int>> map)
        {

            marker.Begin();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            long elapsed = stopwatch.ElapsedTicks;

            long result = calc.Calculate(map);

            elapsed = stopwatch.ElapsedTicks - elapsed;
            stopwatch.Stop();

            marker.End();

            return (elapsed, result);
        }





        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator TestCalculateSumWithSequentialPtrJobs()
        {

            int arraySize = 1000;
            int arrayNum = 1000;


            Dictionary<int, NativeArray<int>> map = CreateMap(arraySize, arrayNum);


            ICalculate calc = new CalculateSumNoJobs();

            long sum = calc.Calculate(map);
            Assert.AreEqual(sum, 1000000000L);



            DisposeMap(map);


            yield return null;
        }
    }







}
