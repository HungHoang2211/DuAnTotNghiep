using UnityEngine;
using System.Collections;

/// <summary>
/// Debug: For our Time Of Day system
/// </summary>
public class ToD_Debug : MonoBehaviour
{
    private ToD_Base _clToDBase;
    private bool _bTodDebugOn;
    private bool _bMoreDebugInfo;

    public GUISkin guiDebugSkin;

    void Start()
    {
        // TỐI ƯU: Thay thế GetComponent kiểu cũ bằng Generic chuẩn, an toàn hiệu năng hơn
        _clToDBase = GetComponent<ToD_Base>();
        _bTodDebugOn = false;
        _bMoreDebugInfo = false;
    }

    void Update()
    {
        BasicDebugControls();
    }

    private void BasicDebugControls()
    {
        if (_clToDBase == null) return;

        // Bật / tắt bảng thông số Debug bằng phím P
        if (Input.GetKeyDown(KeyCode.P))
        {
            _bTodDebugOn = !_bTodDebugOn;
        }

        // Bật / tắt bảng hướng dẫn chi tiết bằng phím H
        if (Input.GetKeyDown(KeyCode.H))
        {
            _bMoreDebugInfo = !_bMoreDebugInfo;
        }

        // Điều khiển tốc độ dòng chảy thời gian
        if (Input.GetKeyDown(KeyCode.Alpha0) && _clToDBase.GetSet_fTimeMultiplier <= 9.5f)
            _clToDBase.GetSet_fTimeMultiplier += 0.5f;
        else if (Input.GetKeyDown(KeyCode.Alpha9) && _clToDBase.GetSet_fTimeMultiplier >= 0.5f)
            _clToDBase.GetSet_fTimeMultiplier -= 0.5f;
        else if (Input.GetKeyDown(KeyCode.Alpha8))
            _clToDBase.GetSet_fTimeMultiplier = 1.0f;
    }

    void OnGUI()
    {
        if (_clToDBase == null) return;

        if (guiDebugSkin != null)
            GUI.skin = guiDebugSkin;

        if (_bTodDebugOn == true)
        {
            // Tiêu đề Debug chính
            GUI.color = Color.yellow;
            GUI.Label(new Rect(Screen.width / 2 - 120, 20, 240, 30), "Debugging: TIME OF DAY");
            GUI.Label(new Rect(Screen.width / 2 - 225, 40, 450, 30), "Press H for more Debug information and controls");

            // Khu vực hiển thị thông số kỹ thuật thời gian
            GUI.color = Color.red;

            // SỬA LỖI HIỂN THỊ: Ép kiểu nguyên (int) để đồng hồ hiển thị chuẩn dạng 14:05 thay vì 14.2:5
            int displayHour = Mathf.FloorToInt(_clToDBase.Get_fCurrentHour);
            int displayMinute = Mathf.FloorToInt(_clToDBase.Get_fCurrentMinute);

            GUI.Label(new Rect(20, 60, 200, 30), "Current time:");
            // Sử dụng định dạng "D2" để tự động thêm số 0 ở trước nếu số chỉ có 1 chữ số (ví dụ: 09 thay vì 9)
            GUI.Label(new Rect(220, 60, 200, 30), displayHour.ToString("D2") + " : " + displayMinute.ToString("D2"));

            // Trạng thái buổi trong ngày (Bình minh, Ngày, Hoàng hôn, Đêm)
            GUI.Label(new Rect(20, 90, 200, 30), "Timeset:");
            GUI.Label(new Rect(220, 90, 200, 30), _clToDBase.enCurrTimeset.ToString());

            // Tốc độ trôi thời gian hiện tại
            GUI.Label(new Rect(20, 150, 200, 30), "Current ToD Speed:");
            GUI.Label(new Rect(220, 150, 200, 30), _clToDBase.GetSet_fTimeMultiplier.ToString("F1") + "x");

            // Số ngày đã trôi qua trong Game
            GUI.Label(new Rect(20, 180, 200, 30), "Days played:");
            GUI.Label(new Rect(220, 180, 200, 30), _clToDBase.Get_iAmountOfDaysPlayed.ToString());

            // Hướng dẫn phím tắt nâng cao (Hiện khi nhấn H)
            if (_bMoreDebugInfo == true)
            {
                GUI.color = Color.cyan;

                GUI.Label(new Rect(20, 240, 600, 30), "Press 0 to add 0.5f to the Time of Day speed");
                GUI.Label(new Rect(20, 270, 600, 30), "Press 9 to substract 0.5f off the Time of Day speed");
                GUI.Label(new Rect(20, 300, 600, 30), "Press 8 to reset Time of Day speed to 1.0f");
                GUI.Label(new Rect(20, 330, 600, 30), "Max speed is 10.0f, and Minimum speed is 0.0f");
            }
        }
    }
}