using UnityEngine;

/// <summary>
/// Gắn lên GameObject Canvas (World Space) của prompt "E".
/// Canvas sẽ luôn quay về phía camera mỗi frame.
///
/// SETUP:
///   Gắn script này lên Canvas của prompt E trên Deer/Wolf/Zombie.
/// </summary>
public class FaceCamera : MonoBehaviour
{
    private Camera _cam;

    private void Awake()
    {
        _cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (_cam == null)
        {
            _cam = Camera.main;
            return;
        }

        // Quay về hướng camera — dùng LookAt rồi đảo ngược
        transform.LookAt(transform.position + _cam.transform.rotation * Vector3.forward,
                         _cam.transform.rotation * Vector3.up);
    }
}