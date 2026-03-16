using UnityEngine;

[RequireComponent(typeof(CarController))]
public class PlayerDriver : MonoBehaviour
{
    private CarController car;

    void Start() => car = GetComponent<CarController>();

    void Update()
    {
        if (!car) return;

        car.throttleInput = Input.GetAxis("Vertical");
        car.steerInput = Input.GetAxis("Horizontal");
        car.brakeInput = Input.GetKey(KeyCode.Space);
    }
}
