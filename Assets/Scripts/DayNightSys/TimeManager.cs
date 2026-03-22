using TMPro;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timeText;

    [SerializeField] Light sun;
    [SerializeField] Light moon;

    [SerializeField] TimeSettings timeSettings;

    TimeService service;

    void Start()
    {
        service = new TimeService(timeSettings);
    }

    void Update()
    {
        UpdateTimeOfDay();
        RotateSun();


        if (Input.GetKeyDown(KeyCode.Space))
        {
            timeSettings.timeMultiplier *= 2;
        }
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            timeSettings.timeMultiplier /= 2;
        }
    }

    void RotateSun()
    {
        float rotation = service.CalculateSunAngle();
        sun.transform.rotation = Quaternion.AngleAxis(rotation, Vector3.right);

    }

    void UpdateTimeOfDay()
    {
        service.UpdateTime(Time.deltaTime);
        if(timeText != null)
        {
            timeText.text = service.CurrentTime.ToString("HH:mm");
        }
    }
}
