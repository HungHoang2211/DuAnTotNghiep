using UnityEngine;

public class WarningFillController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private MeshRenderer meshRenderer;
    [Tooltip("Tên biến trong Shader Graph (thường có dấu gạch dưới phía trước)")]
    [SerializeField] private string fillPropertyName = "_Fill";
    [SerializeField] private float speed = 0.5f;

    public enum Mode { Loop, PingPong, Once }
    [Header("Animation Mode")]
    [SerializeField] private Mode animationMode = Mode.PingPong;

    private Material _material;
    // Đảo ngược: Khởi đầu bằng 1 thay vì 0
    private float _currentFill = 1f;
    // Đảo ngược: Mặc định ban đầu là giảm dần xuống
    private bool _increasing = false;
    private bool _isDone = false;

    void Start()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
        }

        if (meshRenderer != null)
        {
            _material = meshRenderer.material;
            // Đặt giá trị ban đầu trong Shader là 1
            _material.SetFloat(fillPropertyName, _currentFill);
        }
        else
        {
            Debug.LogError("Không tìm thấy MeshRenderer trên " + gameObject.name);
        }
    }

    void Update()
    {
        if (_material == null || _isDone) return;

        switch (animationMode)
        {
            case Mode.Loop:
                // Giảm dần từ 1 về 0, chạm 0 lập tức quay lại 1
                _currentFill -= Time.deltaTime * speed;
                if (_currentFill < 0f) _currentFill = 1f;
                break;

            case Mode.PingPong:
                // Giảm từ 1 về 0, rồi lại tăng từ 0 lên 1
                if (!_increasing)
                {
                    _currentFill -= Time.deltaTime * speed;
                    if (_currentFill <= 0f)
                    {
                        _currentFill = 0f;
                        _increasing = true;
                    }
                }
                else
                {
                    _currentFill += Time.deltaTime * speed;
                    if (_currentFill >= 1f)
                    {
                        _currentFill = 1f;
                        _increasing = false;
                    }
                }
                break;

            case Mode.Once:
                // Giảm dần từ 1 về 0 đúng 1 lần rồi dừng lại (Hiệu ứng biến mất dần)
                _currentFill -= Time.deltaTime * speed;
                if (_currentFill <= 0f)
                {
                    _currentFill = 0f;
                    _isDone = true;
                }
                break;
        }

        // Cập nhật giá trị vào Shader
        _material.SetFloat(fillPropertyName, _currentFill);
    }

    // Hàm reset cũng được cập nhật để đưa về giá trị 1 ban đầu
    public void ResetEffect()
    {
        _currentFill = 1f;
        _isDone = false;
        _increasing = false;
    }
}