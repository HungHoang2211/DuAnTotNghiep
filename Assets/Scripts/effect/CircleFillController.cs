using UnityEngine;

public class CircleFillController : MonoBehaviour
{
    [Header("Cài đặt hiệu ứng")]
    [Tooltip("Tốc độ fill (giá trị càng cao fill càng nhanh)")]
    public float fillSpeed = 1.0f;

    [Tooltip("Chọn cách lặp lại hiệu ứng")]
    public LoopType loopType = LoopType.Restart;

    public enum LoopType
    {
        Restart, // Chạy từ 0 -> 1 rồi reset về 0 chạy lại
        PingPong // Chạy từ 0 -> 1 rồi chạy lùi từ 1 -> 0
    }

    private Material targetMaterial;
    private float currentProgress = 0f;
    private bool isFilling = true; // Dùng riêng cho kiểu PingPong

    void Start()
    {
        // Tự động lấy Material từ MeshRenderer (nếu dùng cho Object 3D/Quad)
        if (TryGetComponent<Renderer>(out var renderer))
        {
            targetMaterial = renderer.material;
        }
        // Hoặc lấy từ Image (nếu bạn dùng cho giao diện UI)
        else if (TryGetComponent<UnityEngine.UI.Image>(out var uiImage))
        {
            targetMaterial = uiImage.material;
        }

        if (targetMaterial == null)
        {
            Debug.LogError("Không tìm thấy Material phù hợp trên Object này!");
        }
    }

    void Update()
    {
        if (targetMaterial == null) return;

        if (loopType == LoopType.Restart)
        {
            // Tăng progress theo thời gian
            currentProgress += Time.deltaTime * fillSpeed;

            // Nếu vượt quá 1 thì reset về 0 (Sử dụng toán tử chia lấy dư)
            currentProgress %= 1.0f;
        }
        else if (loopType == LoopType.PingPong)
        {
            // Tự động tăng giảm mượt mà giữa 0 và 1 bằng hàm PingPong của Unity
            currentProgress = Mathf.PingPong(Time.time * fillSpeed, 1.0f);
        }

        // Đẩy giá trị vào biến _Progress của Shader code HLSL ban nãy
        targetMaterial.SetFloat("_Progress", currentProgress);
    }
}