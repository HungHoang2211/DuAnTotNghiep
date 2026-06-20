using UnityEngine;

public class Weather_Rain : MonoBehaviour
{
    private ToD_Base _clToDBase;
    private float _fTargetIntensity = 1f;
    private float _fLerpSpeed = 2f;
    private bool _bIsFadingOut = false;

    [Header("Rain Visual & Audio")]
    public ParticleSystem rainParticles; // Kéo thả Particle System của mưa vào đây
    public AudioSource rainAudio;        // Kéo thả AudioSource tiếng mưa vào đây

    [SerializeField] private float _fMaxRainSoundVolume = 0.8f;

    void Start()
    {
        _clToDBase = FindFirstObjectByType<ToD_Base>();

        // Đảm bảo lúc đầu nếu đang kích hoạt thì Particle phải chạy
        if (enabled && rainParticles != null && !rainParticles.isPlaying)
        {
            rainParticles.Play();
        }
    }

    void Update()
    {
        if (_clToDBase == null) return;

        // TRƯỜNG HỢP 1: Thời tiết đang chuyển giao tắt TRỜI MƯA (Fade Out)
        if (_bIsFadingOut)
        {
            // Giảm dần âm lượng tiếng mưa về 0
            if (rainAudio != null)
                rainAudio.volume = Mathf.MoveTowards(rainAudio.volume, 0f, Time.deltaTime * _fLerpSpeed);

            // Khi âm lượng đã về 0, tắt hẳn hiệu ứng hạt mưa
            if (rainAudio == null || rainAudio.volume <= 0.01f)
            {
                if (rainParticles != null && rainParticles.isPlaying) rainParticles.Stop();
            }
            return;
        }

        // TRƯỜNG HỢP 2: Đang trong trạng thái TRỜI MƯA bình thường
        // Nếu hạt mưa đang tắt thì bật lại (dành cho lúc vừa giao thoa xong)
        if (rainParticles != null && !rainParticles.isPlaying)
        {
            rainParticles.Play();
        }

        // Điều tiết ánh sáng và âm thanh dựa theo buổi (Mưa ban ngày sẽ tối hơn ngày thường)
        float currentTargetVolume = _fMaxRainSoundVolume;

        switch (_clToDBase.enCurrTimeset)
        {
            case ToD_Base.Timeset.SUNRISE:
                _fTargetIntensity = 0.2f;
                break;
            case ToD_Base.Timeset.DAY:
                _fTargetIntensity = 0.5f; // Giảm sáng xuống một nửa vì mây mưa che phủ
                break;
            case ToD_Base.Timeset.SUNSET:
                _fTargetIntensity = 0.2f;
                break;
            case ToD_Base.Timeset.NIGHT:
                _fTargetIntensity = 0.0f;
                currentTargetVolume = _fMaxRainSoundVolume * 0.6f; // Đêm khuya tiếng mưa nhỏ hơn cho dễ ngủ
                break;
        }

        // Lerp mượt mà cường độ sáng của đèn mặt trời
        if (_clToDBase.lSun != null)
            _clToDBase.lSun.intensity = Mathf.Lerp(_clToDBase.lSun.intensity, _fTargetIntensity, Time.deltaTime * _fLerpSpeed);

        // Lerp mượt mà âm lượng tiếng mưa
        if (rainAudio != null)
            rainAudio.volume = Mathf.Lerp(rainAudio.volume, currentTargetVolume, Time.deltaTime * _fLerpSpeed);
    }

    // Hàm nhận lệnh từ Weather_Controller gửi sang
    public void StartWeatherTransition(Weather_Controller.WeatherType targetWeather)
    {
        _bIsFadingOut = (targetWeather != Weather_Controller.WeatherType.RAIN);

        // Nếu bắt đầu chuyển sang trời mưa, kích hoạt Audio phát để chuẩn bị Lerp Volume lên
        if (!_bIsFadingOut && rainAudio != null && !rainAudio.isPlaying)
        {
            rainAudio.volume = 0f;
            rainAudio.Play();
        }
    }
}