using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimeOfDayUI : MonoBehaviour
{
    [Header("Refs")]
    public TimeOfDaySystem timeSystem;
    public TextMeshProUGUI timeLabel;
    public TextMeshProUGUI dayLabel; // [เพิ่ม] เผื่ออยากโชว์วันที่

    [Header("Clock Animation")]
    [Tooltip("ลาก GameObject ที่มี Animator ของนาฬิกามาใส่ (ตัว ClockUI)")]
    public Animator clockAnimator;

    [Tooltip("ชื่อ State ใน Animator ที่ทำไว้ (เช่น DayNightCycle)")]
    public string animationStateName = "DayNightCycle";

    void Start()
    {
        if (timeSystem == null) timeSystem = TimeOfDaySystem.Instance;
    }

    void Update()
    {
        if (timeSystem == null) return;

        // 1. อัปเดตตัวเลขเวลา
        if (timeLabel != null)
            timeLabel.text = $"{timeSystem.Hour:00}:{timeSystem.Minute:00}";

        // (แถม) อัปเดตวันที่
        // if (dayLabel != null) dayLabel.text = $"Day {GameManager.Instance.calendar.day}";

        // 2. อัปเดต Animation ตามเวลาจริง (หัวใจสำคัญ!) ??
        if (clockAnimator != null)
        {
            // สั่งให้ Animator กระโดดไปที่เฟรมตามเวลา Time01 (0.0 - 1.0) ทันที
            // playSpeed = 0 (หยุดเล่นอัตโนมัติ เพราะเราคุมเองทุกเฟรม)
            clockAnimator.Play(animationStateName, 0, timeSystem.Time01);
            clockAnimator.speed = 0f;
        }
    }
}