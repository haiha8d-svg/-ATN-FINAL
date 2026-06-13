using UnityEngine;
using Debug = UnityEngine.Debug;

public class SlopeTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        BikeController bike = other.GetComponent<BikeController>();

        if (bike != null)
        {
            float slopeAngle = transform.eulerAngles.x;

            if (slopeAngle > 180) slopeAngle = (slopeAngle - 360);
            bike.SetSlopeAngle(slopeAngle);
            Debug.Log("Trigger! Angle: " + slopeAngle);
        }
    }

    void OnTriggerExit(Collider other)
    {
        BikeController bike = other.GetComponent<BikeController>();

        if (bike != null)
        {
            bike.ResetSlopeAngle();
            Debug.Log("Angle Reset!");
        }
    }
}
