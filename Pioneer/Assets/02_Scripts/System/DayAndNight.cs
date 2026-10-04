using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DayAndNight : MonoBehaviour, IBegin
{
    public Volume volume;
    private ColorAdjustments colorAdjustments;

    public Gradient dayToNight;
    public Gradient nightToDay;
    public AnimationCurve exposureByTime;
    public float dayDuration = 60f; // 하루 시간 (초)

    private float timer;

    void Start()
    {
        if (volume != null && volume.profile != null)
            volume.profile.TryGet(out colorAdjustments);
    }

    void Update()
    {
        if (GameManager.Instance != null || colorAdjustments == null || Time.timeScale <= 0f) return;

        timer += Time.deltaTime;
        float duration = Mathf.Max(0.01f, dayDuration);
        float phase = Mathf.Repeat(timer, duration * 2f) / duration;
        float nightBlend = phase <= 1f ? phase : 2f - phase;

        colorAdjustments.colorFilter.overrideState = true;
        if (dayToNight != null) colorAdjustments.colorFilter.value = dayToNight.Evaluate(nightBlend);
        else if (nightToDay != null) colorAdjustments.colorFilter.value = nightToDay.Evaluate(1f - nightBlend);
        colorAdjustments.postExposure.overrideState = true;
        if (exposureByTime != null) colorAdjustments.postExposure.value = exposureByTime.Evaluate(1f - nightBlend);
    }
}
