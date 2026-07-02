using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        Vector3 targetPosition = new Vector3(
            target.position.x,
            target.position.y,
            -10
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}