using UnityEngine;

public class PedalRotate : MonoBehaviour
{
    public float speed = 200f;

    void Update()
    {
        transform.Rotate(Vector3.left * speed * Time.deltaTime);
    }
}