using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class CutsceneController : MonoBehaviour
{
    [Header("--- CẤU HÌNH XUẤT/NHẬP SCENE ---")]
    [Tooltip("Kéo GameObject chứa VideoPlayer vào đây (nếu để trống, script sẽ tự tìm).")]
    public VideoPlayer videoPlayer;

    [Tooltip("Nhập chính xác tên Scene Gameplay muốn chuyển tới sau khi hết video.")]
    public string nextSceneName = "GameplayScene";

    [Header("--- TÙY CHỌN BỎ QUA (SKIP) ---")]
    [Tooltip("Tích chọn nếu muốn cho phép người chơi bấm phím để Skip video.")]
    public bool allowSkip = true;

    // Cờ đánh dấu tránh bị gọi LoadScene 2 lần cùng lúc
    private bool isSceneLoading = false;

    void Awake()
    {
        // Tự động tìm component VideoPlayer trên cùng GameObject nếu chưa gán
        if (videoPlayer == null)
        {
            videoPlayer = GetComponent<VideoPlayer>();
        }
    }

    void Start()
    {
        if (videoPlayer != null)
        {
            // Đăng ký sự kiện: Khi video phát xong hoàn toàn sẽ tự động gọi hàm OnVideoFinished
            videoPlayer.loopPointReached += OnVideoFinished;

            // Đăng ký sự kiện: Khi video đã chuẩn bị xong (Prepare)
            videoPlayer.prepareCompleted += OnVideoPrepared;

            // Bắt đầu chuẩn bị (Preload) video vào bộ nhớ RAM/GPU trước khi phát
            videoPlayer.Prepare();
        }
        else
        {
            Debug.LogError("[CutsceneController] Chưa gắn VideoPlayer component!");
        }
    }

    void Update()
    {
        // Kiểm tra phím tắt Skip (Space, ESC, hoặc Click chuột trái)
        if (allowSkip && !isSceneLoading)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
            {
                Debug.Log("[CutsceneController] Người chơi đã bấm Skip video.");
                GoToNextScene();
            }
        }
    }

    // Hàm gọi khi Video đã Preload xong hoàn toàn
    private void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("[CutsceneController] Video đã chuẩn bị xong, bắt đầu phát!");
        vp.Play();
    }

    // Hàm gọi khi Video chạy hết thời lượng tự nhiên
    private void OnVideoFinished(VideoPlayer vp)
    {
        Debug.Log("[CutsceneController] Video đã chạy hết.");
        GoToNextScene();
    }

    // Hàm thực hiện chuyển Scene sang Game chính
    public void GoToNextScene()
    {
        if (isSceneLoading) return; // Tránh gọi trùng lặp
        isSceneLoading = true;

        // Hủy đăng ký các sự kiện để tránh rò rỉ bộ nhớ (Memory Leak)
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.Stop(); // Dừng video
        }

        // Chuyển sang Scene Gameplay
        Debug.Log($"[CutsceneController] Đang chuyển sang Scene: {nextSceneName}");
        SceneManager.LoadScene(nextSceneName);
    }

    void OnDestroy()
    {
        // Đảm bảo hủy đăng ký sự kiện khi GameObject bị hủy
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
    }
}