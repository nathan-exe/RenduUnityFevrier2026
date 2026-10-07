using UnityEngine;

public class Turntable : MonoBehaviour
{
    [SerializeField] private float speed;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up * (Time.deltaTime * speed));
    }
}
