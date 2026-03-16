using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDriver : MonoBehaviour
{
    private CarController car;

    void Start() => car = GetComponent<CarController>();

    void Update()
    {
        car.throttleInput = Input.GetAxis("Vertical");
        car.steerInput = Input.GetAxis("Horizontal");
        car.brakeInput = Input.GetKey(KeyCode.Space);
    }
}