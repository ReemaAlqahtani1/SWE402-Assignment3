using TMPro;
using UnityEngine;

// Presentation only. The original HUD continues to own the LIVES number.
public sealed class LivesHearts : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private HeartIconGraphic[] hearts;
    private int shownLives;
    private float changedAt = -10f;
    private readonly Color full = new Color(1f, 0.38f, 0.46f, 1f);
    private readonly Color empty = new Color(0.34f, 0.26f, 0.32f, 0.65f);

    private static int CurrentLives()
    {
        if (AfterRound.Active != null) return AfterRound.Active.RemainingLives;
        return GameManager.Instance != null ? GameManager.Instance.Lives : 3;
    }

    public void Configure(TMP_Text livesLabel)
    {
        label = livesLabel;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = "LIVES";
        if (hearts != null && hearts.Length > 0)
        {
            Refresh(CurrentLives());
            return;
        }
        int capacity = CurrentLives();
        hearts = new HeartIconGraphic[Mathf.Clamp(capacity, 1, 12)];
        for (int i = 0; i < hearts.Length; i++)
        {
            var go = new GameObject("Life heart " + (i + 1), typeof(RectTransform));
            go.transform.SetParent(label.transform, false);
            var heart = go.AddComponent<HeartIconGraphic>();
            heart.raycastTarget = false;
            heart.rectTransform.anchorMin = heart.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            heart.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            heart.rectTransform.sizeDelta = new Vector2(40f, 38f);
            hearts[i] = heart;
        }
        Refresh(CurrentLives());
    }

    private void Start()
    {
        if (label != null) Configure(label);
    }

    private void OnEnable()
    {
        GameEvents.LivesChanged += Refresh;
        if (hearts != null) Refresh(CurrentLives());
    }

    private void OnDisable()
    {
        GameEvents.LivesChanged -= Refresh;
        if (label != null) label.text = "LIVES " + CurrentLives();
        if (hearts == null) return;
        foreach (var heart in hearts)
            if (heart != null) heart.gameObject.SetActive(false);
    }

    private void Refresh(int lives)
    {
        shownLives = lives;
        if (label != null) { label.text = "LIVES"; label.alignment = TextAlignmentOptions.MidlineLeft; }
        changedAt = Time.unscaledTime;
        if (hearts == null) return;
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;
            hearts[i].gameObject.SetActive(true);
            hearts[i].color = i < shownLives ? full : empty;
        }
    }

    private void LateUpdate()
    {
        if (label == null || hearts == null) return;
        // HUD still receives life events; this After-only view displays icons instead of digits.
        label.text = "LIVES";
        float labelWidth = label.GetPreferredValues("LIVES").x;
        float age = Time.unscaledTime - changedAt;
        float pulse = 1f + 0.08f * Mathf.Clamp01(1f - age / 0.2f);
        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null) continue;
            hearts[i].rectTransform.anchorMin = hearts[i].rectTransform.anchorMax = new Vector2(0f, 0.5f);
            hearts[i].rectTransform.anchoredPosition = new Vector2(labelWidth + 26f + i * 46f, 0f);
            hearts[i].transform.localScale = Vector3.one * (i < shownLives ? pulse : 1f);
        }
    }

    // Scene-owned hearts are cleaned up with their parent Canvas on scene unload.
}
