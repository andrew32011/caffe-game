using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlamesController : MonoBehaviour
{
    [SerializeField] private GameObject Flame1;
    [SerializeField] private GameObject Flame2;
    [SerializeField] private GameObject Flame3;

    [SerializeField] private float Delay1 = 1.0f;
    [SerializeField] private float Delay2 = 1.0f;
    // Start is called before the first frame update
    void Start()
    {
        Flame1.SetActive(false);
        Flame2.SetActive(false);
        Flame3.SetActive(false);
        Invoke(nameof(EnableFirst), Delay1);
        Invoke(nameof(EnableSecond), Delay2);
    }

    // Update is called once per frame
    void EnableFirst()
    {
        Flame1.SetActive(true);
    }

    void EnableSecond()
    {
        Flame2.SetActive(true);
        Flame3.SetActive(true);
    }
}
