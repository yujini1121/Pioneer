using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class InGameUiClockOut : MonoBehaviour
{
    [SerializeField] private SurvivalClockFace face;
    [SerializeField] private Text phaseText;
    [SerializeField, Range(0.01f, 0.3f)] private float warningFraction = 0.12f;
    private int warnedDay = -1;
    private int shownDay = -1;
    private bool shownDaytime;
    private bool initialized;
    private Tween feedback;
    private Vector3 faceScale;

    private void Awake() { if (face != null) faceScale = face.transform.localScale; }

    private void LateUpdate()
    {
        var game = GameManager.Instance;
        if (game == null || face == null) return;
        if (game.IsGameResultActive) { StopFeedback(); return; }
        bool changed = !initialized || shownDay != game.currentDay || shownDaytime != game.IsDaytime;
        if (changed)
        {
            bool nightStarted = initialized && shownDaytime && !game.IsDaytime;
            StopFeedback();
            shownDay = game.currentDay;
            shownDaytime = game.IsDaytime;
            initialized = true;
            if (phaseText != null) phaseText.text = $"{shownDay}일 · {(shownDaytime ? "낮" : "밤")}";
            if (nightStarted)
                feedback = face.transform.DOPunchScale(faceScale * 0.07f, 0.25f, 1, 0.3f)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable).OnKill(RestoreScale);
        }
        face.SetPhase(game.IsDaytime, game.CurrentPhaseProgress);
        if (Time.timeScale <= 0f) return;
        if (game.IsDaytime && game.CurrentPhaseRemaining <= game.CurrentPhaseDuration * warningFraction
            && warnedDay != game.currentDay)
        {
            warnedDay = game.currentDay;
            StopFeedback();
            InGameUI.instance?.ShowActionFeedback("곧 밤이 찾아옵니다.", 1);
            feedback = face.transform.DOScale(faceScale * 1.045f, 0.55f).SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo).SetLink(gameObject, LinkBehaviour.KillOnDisable).OnKill(RestoreScale);
        }
    }

    private void RestoreScale() { if (face != null) face.transform.localScale = faceScale; }
    private void StopFeedback() { feedback?.Kill(); feedback = null; RestoreScale(); }
    private void OnDisable() { StopFeedback(); }
    private void OnDestroy() { StopFeedback(); }
}
