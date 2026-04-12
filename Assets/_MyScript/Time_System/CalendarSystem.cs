using System;
using UnityEngine;

[Serializable]
public struct Date
{
    public int year, month, day;
    public Date(int y, int m, int d) { year = y; month = m; day = d; }
}

public class CalendarSystem : MonoBehaviour
{
    public static CalendarSystem Instance { get; private set; }

    [Header("Clock / Link")]
    [Tooltip("ลิงก์ไป TimeOfDaySystem เพื่อให้ปฏิทินเดินตามเวลาในเกม (ไม่ใส่ก็ได้)")]
    public TimeOfDaySystem timeOfDay;

    [Header("Date Config")]
    [Tooltip("จำนวนวันต่อเดือนแบบคงที่")]
    [Min(1)] public int daysPerMonth = 30;

    [Header("Initial Date")]
    public int startYear = 1;
    public int startMonth = 1;
    public int startDay = 1;

    // Runtime
    [SerializeField, Range(0f, 1f)] private float time01; // 0..1 ของแต่ละวัน
    [SerializeField] private int year, month, day;

    public Date date => new Date(year, month, day);
    public float Time01 => time01;

    // Events
    public event Action<Date> OnDateChanged;  // ยิงทุกครั้งที่ SetDate/SetTime01
    public event Action<Date> OnDayEnded;     // ยิงเมื่อวันจบ (เที่ยงคืน/ข้ามวัน)

    float lastTod01 = -1f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        year = Mathf.Max(1, startYear);
        month = Mathf.Clamp(startMonth, 1, 12);
        day = Mathf.Clamp(startDay, 1, daysPerMonth);
        time01 = 0f; // เริ่ม 00:00 (ให้ TimeOfDaySystem เป็นคนกำหนดเวลาเริ่ม)
    }

    void Update()
    {
        // ถ้ามี TOD ให้ sync และตรวจจับ cross-midnight
        if (timeOfDay != null)
        {
            TickFromTOD(timeOfDay.Time01);
        }
    }

    /// <summary>เรียกทุกเฟรมเมื่อมี TOD: ตรวจจับวันใหม่และ sync time01</summary>
    public void TickFromTOD(float tod01)
    {
        if (lastTod01 < 0f) lastTod01 = tod01;

        // ตรวจจับ wrap: ตัวก่อน > ตัวใหม่ => ข้ามวัน
        bool crossedMidnight = lastTod01 > tod01;
        lastTod01 = tod01;

        SetTime01(tod01, raiseChanged: false);

        if (crossedMidnight)
        {
            NextDay();
            OnDayEnded?.Invoke(date);
            // ยิง OnDateChanged อีกรอบหลังจบวัน
            OnDateChanged?.Invoke(date);
        }
        else
        {
            // ระหว่างวัน ถ้าคุณอยากอัปเดต UI เวลา ให้ยิงเฉพาะเปลี่ยนเล็กๆ ได้
            OnDateChanged?.Invoke(date);
        }
    }

    public void SetDate(int y, int m, int d)
    {
        year = Mathf.Max(1, y);
        month = Mathf.Clamp(m, 1, 12);
        day = Mathf.Clamp(d, 1, daysPerMonth);
        OnDateChanged?.Invoke(date);
    }

    public void SetTime01(float t, bool raiseChanged = true)
    {
        time01 = Mathf.Repeat(t, 1f);
        if (raiseChanged) OnDateChanged?.Invoke(date);
    }
    

    /// <summary>เลื่อนไปวันถัดไป (public เพื่อให้ระบบอื่นเรียกได้)</summary>
    public void NextDay()
    {
        day++;
        if (day > daysPerMonth)
        {
            day = 1;
            month++;
            if (month > 12) { month = 1; year++; }
        }
    }

    public bool IsLastDayOfMonth(int d) => d >= daysPerMonth;

    /// <summary>ข้ามวันจนกว่า Date จะเท่ากับเป้าหมาย (ใช้ตอนโหลดเก็บ state จากไฟล์)</summary>
    public void FastForwardTo(Date target)
    {
        year = target.year;
        month = Mathf.Clamp(target.month, 1, 12);
        day = Mathf.Clamp(target.day, 1, daysPerMonth);
        OnDateChanged?.Invoke(date);
    }
}
