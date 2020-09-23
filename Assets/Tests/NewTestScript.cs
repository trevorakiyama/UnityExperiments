using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;
using UnityEngine.XR.WSA;

namespace Tests
{
    public class NewTestScript
    {

       [SetUp]
        public void ResetSchene()
        {

            Debug.Log("Calling Setup");

            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");

            SceneManager.SetActiveScene(SceneManager.GetSceneAt(0));
            
        }


        // A Test behaves as an ordinary method
        [Test]
        public void NewTestScriptSimplePasses()
        {
            // Use the Assert class to test conditions
        }

        // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // `yield return null;` to skip a frame.
        [UnityTest]
        public IEnumerator NewTestScriptWithEnumeratorPasses()
        {


            Assert.IsTrue(true);
            

            // Use the Assert class to test conditions.
            // Use yield to skip a frame.


            //Settings settings = new Settings();
            
            

            //TestMultiData multiData = new TestMultiData();
            





            yield return null;




        }
    }
}
