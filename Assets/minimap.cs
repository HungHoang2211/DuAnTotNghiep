using UnityEngine;

public class MinimapFollow : MonoBehaviour
{
    public Transform target;
    public float height = 25f;

    void LateUpdate()
    {
        transform.position = new Vector3(
            target.position.x,
            height,
            target.position.z);

        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}