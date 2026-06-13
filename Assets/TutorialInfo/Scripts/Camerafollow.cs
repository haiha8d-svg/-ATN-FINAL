using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 18f, 0f);
    public float followSpeed = 6f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        if (GameManager.Instance != null && GameManager.Instance.raceFinished)
        {
            Quaternion targetRot = Quaternion.LookRotation((target.position + Vector3.up) - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, followSpeed * Time.deltaTime);
        }
        else
        {
            transform.rotation = Quaternion.Euler(90f, 180f, 0f);
        }
    }
}