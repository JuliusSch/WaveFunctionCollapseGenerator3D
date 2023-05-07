using System.Collections;
using Unity.Jobs;
using UnityEngine;

public class CoroutineTest : MonoBehaviour
{
    public GameObject model;

    void Start()
    {
        //StartCoroutine(Test());
    }

    IEnumerator Test()
    {
        var job = new TestJob {
            i = 0
        };
        var handle = job.Schedule();

        while (!handle.IsCompleted)
            yield return null;
        handle.Complete();
    }
    public struct TestJob : IJob
    {
        public int i;

        public void Execute()
        {
            while (i < 2000000)
            {
                //Instantiate(model, new Vector3(i * 10, 0, 0), Quaternion.identity);
                Debug.Log(i);
                i++;
            }
        }
    }
}
