using UnityEngine;
using System;

public enum DayPhase { Dawn, Day, Dusk, Night }

public class TimeOfDaySystem : MonoBehaviour
{
    public static TimeOfDaySystem Instance { get; private set; }

    [Header("Clock")]
    public float dayLengthInMinutes = 10f;
    [Range(0, 23)] public int startHour = 6;
    [Range(0, 59)] public int startMinute = 0;

    [Header("Sun (Day)")]
    public Light directionalLight;
    public Gradient sunColor;
    public AnimationCurve sunIntensity;

    [Header("Moon (Night)")]
    public Light moonLight;
    public Gradient moonColor;
    public AnimationCurve moonIntensity;

    [Header("Environment")]
    public Material skyboxMaterial; // อันนี้คือตัวที่จะถูกแก้ค่าสี (ปกติคือท้องฟ้าตอนเช้า)

    // [เพิ่มใหม่] สำหรับสลับ Skybox
    [Header("Skybox Swapping")]
    public Material daySkybox;     // ลาก Material ท้องฟ้า "ตอนเช้า" ใส่ที่นี่
    public Material nightSkybox;   // ลาก Material ท้องฟ้า "ตอนคืน" (ที่มีดาว) ใส่ที่นี่

    public Gradient skyTint;
    public Gradient groundTint;
    public Gradient ambientColor;
    public Gradient fogColor;
    public AnimationCurve fogDensity;

    [Header("Exposure")]
    [Range(0, 8)] public float skyExposureDay = 1.2f;
    [Range(0, 8)] public float skyExposureNight = 0.5f;

    [Header("Phase Thresholds")]
    [Range(0f, 1f)] public float dawnStart = 0.20f;
    [Range(0f, 1f)] public float dayStart = 0.30f;
    [Range(0f, 1f)] public float duskStart = 0.70f;
    [Range(0f, 1f)] public float nightStart = 0.80f;

    public event Action<DayPhase> OnPhaseChanged;

    [SerializeField, Range(0f, 1f)] private float time01;
    private DayPhase currentPhase;
    public float Time01 => time01;

    public int Hour => Mathf.FloorToInt(time01 * 24f);
    public int Minute => Mathf.FloorToInt(((time01 * 24f) % 1) * 60f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        time01 = ((startHour % 24) + startMinute / 60f) / 24f;
        currentPhase = GetPhase(time01);
    }

    void Update()
    {
        AdvanceTime();
        UpdateLighting();
        CheckPhaseChange();
    }

    void AdvanceTime()
    {
        float secondsPerDay = Mathf.Max(1f, dayLengthInMinutes * 60f);
        time01 += Time.deltaTime / secondsPerDay;
        if (time01 >= 1f) time01 -= 1f;
    }

    void UpdateLighting()
    {
        // 1. หมุนดวงอาทิตย์
        float sunAngle = (time01 * 360f) - 90f;
        if (directionalLight != null)
        {
            directionalLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
            if (sunColor != null) directionalLight.color = sunColor.Evaluate(time01);
            if (sunIntensity != null) directionalLight.intensity = sunIntensity.Evaluate(time01);

            if (directionalLight.intensity <= 0.01f && directionalLight.shadows != LightShadows.None)
                directionalLight.shadows = LightShadows.None;
            else if (directionalLight.intensity > 0.01f && directionalLight.shadows == LightShadows.None)
                directionalLight.shadows = LightShadows.Soft;
        }

        // 2. หมุนดวงจันทร์
        if (moonLight != null)
        {
            moonLight.transform.rotation = Quaternion.Euler(sunAngle - 180f, 170f, 0f);
            if (moonColor != null) moonLight.color = moonColor.Evaluate(time01);
            if (moonIntensity != null) moonLight.intensity = moonIntensity.Evaluate(time01);
        }

        // 3. [แก้ใหม่] สลับ Skybox และปรับสี
        bool isNight = (time01 >= nightStart || time01 < dawnStart);

        // ถ้ามีทั้ง 2 แบบ ให้สลับตามเวลา
        if (daySkybox != null && nightSkybox != null)
        {
            Material targetSky = isNight ? nightSkybox : daySkybox;
            if (RenderSettings.skybox != targetSky)
            {
                RenderSettings.skybox = targetSky;
                DynamicGI.UpdateEnvironment(); // สั่ง Unity อัปเดตแสงสะท้อนโลกใหม่
            }
        }

        // 4. การปรับสี Skybox (Tint) 
        // เราจะปรับสีเฉพาะตอนเป็น "กลางวัน" หรือถ้าใช้ Skybox เดียว
        // เพราะ Skybox กลางคืนมักจะเป็นรูปดาวสวยอยู่แล้ว ไม่ต้องไปย้อมสีทับ
        if (skyboxMaterial != null && !isNight)
        {
            if (skyTint != null) skyboxMaterial.SetColor("_SkyTint", skyTint.Evaluate(time01));
            if (groundTint != null) skyboxMaterial.SetColor("_GroundColor", groundTint.Evaluate(time01));

            // Exposure ปรับได้ทั้งวันทั้งคืน เพื่อความสว่างที่เหมาะสม
            float exposureTarget = isNight ? skyExposureNight : skyExposureDay;
            // skyboxMaterial.SetFloat("_Exposure", exposureTarget); // บรรทัดนี้ถ้าใช้คนละ Material อาจจะไม่ต้องปรับ
        }
        else if (skyboxMaterial != null && isNight)
        {
            // ถ้าอยากคุมความสว่างตอนกลางคืนด้วย ให้แก้ตรงนี้
            // skyboxMaterial.SetFloat("_Exposure", skyExposureNight);
        }

        if (ambientColor != null) RenderSettings.ambientLight = ambientColor.Evaluate(time01);
        if (fogColor != null) RenderSettings.fogColor = fogColor.Evaluate(time01);
        if (fogDensity != null && fogDensity.length > 0) RenderSettings.fogDensity = fogDensity.Evaluate(time01);
    }

    void CheckPhaseChange()
    {
        var p = GetPhase(time01);
        if (p != currentPhase)
        {
            currentPhase = p;
            OnPhaseChanged?.Invoke(currentPhase);
        }
    }

    DayPhase GetPhase(float t)
    {
        if (t >= nightStart || t < dawnStart) return DayPhase.Night;
        if (t >= duskStart) return DayPhase.Dusk;
        if (t >= dayStart) return DayPhase.Day;
        return DayPhase.Dawn;
    }

    public void SetTime(int hour, int minute)
    {
        time01 = ((hour % 24) + minute / 60f) / 24f;
        UpdateLighting();
        CheckPhaseChange();
    }
}