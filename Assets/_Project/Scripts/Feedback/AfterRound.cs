using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// After-only round rules. Replaces DeathZone in After; shared starter code stays intact.
// GameManager retains score/combo; this component owns the After life count.
public sealed class AfterRound : MonoBehaviour
{
    public static AfterRound Active { get; private set; }
    private static bool beginAfterReload;
    public int RemainingLives { get; private set; } = 3;
    public bool IsGameOver { get; private set; }
    public bool IsWon { get; private set; }
    public bool IsRunning { get; private set; }

    private Ball ball;
    private Paddle paddle;
    private Rigidbody2D body;
    private GameObject overlay;
    private RectTransform menuCard;
    private CanvasGroup menuFade;
    private Image accentLine, buttonImage;
    private TMP_Text kicker;
    private float revealTime;
    private TMP_Text heading, detail, buttonText;
    private Button button;
    private bool reloading;
    private int lastLossFrame = -1;
    private int brickEventFrame = -1;
    private bool checkVictory;

    private void OnEnable() { GameEvents.BrickDestroyed += OnBrickDestroyed; }
    private void OnDisable() { GameEvents.BrickDestroyed -= OnBrickDestroyed; }

    private void OnBrickDestroyed(ImpactInfo info)
    {
        if (!IsRunning || reloading) return;
        // Brick sends the event before Destroy. Count on the next frame, not in the callback.
        checkVictory = true;
        brickEventFrame = Time.frameCount;
    }

    private void LateUpdate()
    {
        if (!checkVictory || Time.frameCount <= brickEventFrame || !IsRunning || reloading) return;
        checkVictory = false;
        foreach (var brick in FindObjectsByType<Brick>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (brick.gameObject.scene == gameObject.scene) return;
        Win();
    }

    private void Win()
    {
        if (!IsRunning || IsGameOver || IsWon || reloading) return;
        IsWon = true;
        Freeze();
        Color gold = new Color(1f, 0.79f, 0.32f);
        heading.text = "YOU WIN!";
        heading.color = gold;
        kicker.text = "HOORAY!  ALL BRICKS CLEARED";
        kicker.color = gold;
        detail.text = "<size=24>FINAL SCORE</size>\n<size=72><color=#FFFFFF>"
            + GameManager.Instance.Score + "</color></size>";
        accentLine.color = buttonImage.color = gold;
        buttonText.text = "PLAY AGAIN";
        var feedback = FindFirstObjectByType<AfterFeedback>();
        if (feedback != null) feedback.PlayVictoryFeedback();
        ShowMenu();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Active = null; beginAfterReload = false; }

    private void Awake()
    {
        Active = this;
        ball = FindFirstObjectByType<Ball>();
        paddle = FindFirstObjectByType<Paddle>();
        if (ball == null || paddle == null)
        {
            Debug.LogError("After round requires a Ball and Paddle in this scene.", this);
            enabled = false;
            return;
        }
        body = ball.GetComponent<Rigidbody2D>();
        // Prevent the starter's automatic Ball.Start launch until Start is pressed.
        Freeze();
    }

    private void Start()
    {
        RemainingLives = GameManager.Instance != null ? GameManager.Instance.Lives : 3;
        CreateMenu();
        gameObject.AddComponent<AfterBrickLook>();
        gameObject.AddComponent<AfterPaddleLook>();
        bool resume = beginAfterReload;
        beginAfterReload = false;
        if (resume) Begin();
    }

    private void Freeze()
    {
        IsRunning = false;
        ball.enabled = false;
        body.simulated = false;
        body.linearVelocity = Vector2.zero;
        paddle.enabled = false;
    }

    public void Begin()
    {
        if (IsRunning || IsGameOver || IsWon || reloading) return;
        overlay.SetActive(false);
        body.simulated = true;
        paddle.enabled = true;
        ball.enabled = true; // Its original Start launches once on the first frame.
        IsRunning = true;
        GameEvents.RaiseLivesChanged(RemainingLives);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsRunning || reloading || !other.TryGetComponent<Ball>(out var lost) || lost != ball) return;
        if (lastLossFrame == Time.frameCount) return;
        lastLossFrame = Time.frameCount;
        RemainingLives = Mathf.Max(0, RemainingLives - 1);
        GameManager.Instance.ResetCombo();
        IsGameOver = RemainingLives == 0;
        if (IsGameOver) Freeze();
        GameEvents.RaiseLivesChanged(RemainingLives);
        GameEvents.RaiseBallLost();
        if (!IsGameOver)
        {
            ball.Launch();
            return;
        }
        heading.text = "GAME OVER";
        heading.color = new Color(1f, 0.48f, 0.5f);
        kicker.text = "ROUND COMPLETE";
        detail.text = "<size=24>FINAL SCORE</size>\n<size=72><color=#FFFFFF>" + GameManager.Instance.Score + "</color></size>";
        accentLine.color = buttonImage.color = new Color(1f, 0.48f, 0.5f);
        buttonText.text = "RESTART";
        ShowMenu();
    }

    public void Restart()
    {
        if ((!IsGameOver && !IsWon) || reloading) return;
        reloading = true;
        button.interactable = false;
        beginAfterReload = true;
        // Reload rebuilds the original board and resets score, lives, and feedback pools.
        SceneManager.LoadScene(gameObject.scene.name);
    }

    private void CreateMenu()
    {
        var hud = FindFirstObjectByType<HUD>();
        var canvas = hud.GetComponent<Canvas>();
        TMP_FontAsset font = hud.GetComponentInChildren<TMP_Text>().font;
        overlay = new GameObject("After start and game-over screen", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(canvas.transform, false);
        var rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0.018f, 0.025f, 0.055f, 0.96f);

        // A framed arcade card, with depth and a small brick motif.
        Panel("Card shadow", overlay.transform, new Vector2(10, -12), new Vector2(970, 630), new Color(0f, 0f, 0f, 0.45f));
        var card = Panel("Arcade card", overlay.transform, Vector2.zero, new Vector2(960, 620), new Color(0.055f, 0.075f, 0.12f));
        menuCard = card.rectTransform;
        var border = card.gameObject.AddComponent<Outline>();
        border.effectColor = new Color(0.22f, 0.34f, 0.44f, 0.8f);
        border.effectDistance = new Vector2(1, -1);
        menuFade = card.gameObject.AddComponent<CanvasGroup>();
        accentLine = Panel("Accent", menuCard, new Vector2(0, 309), new Vector2(960, 3), new Color(0.35f, 0.9f, 1f));
        Color[] motif = { new Color(1f, 0.48f, 0.5f), new Color(1f, 0.73f, 0.35f), new Color(0.35f, 0.9f, 1f), new Color(0.58f, 0.48f, 1f), new Color(0.35f, 0.9f, 1f) };
        for (int i = 0; i < 5; i++)
        {
            var pos = new Vector2((i - 2) * 53, 245);
            Panel("Brick motif shadow", menuCard, pos + new Vector2(0, -4), new Vector2(44, 18), new Color(0f, 0f, 0f, 0.4f));
            Panel("Brick motif", menuCard, pos, new Vector2(44, 18), motif[i]);
            Panel("Brick motif shine", menuCard, pos + new Vector2(0, 7), new Vector2(40, 2), new Color(1f, 1f, 1f, 0.38f));
        }
        kicker = Text("Kicker", "THE ARCADE EDITION", 24, 194, new Color(0.35f, 0.9f, 1f), font);
        kicker.characterSpacing = 3;
        heading = Text("Heading", "BRICK <color=#59E6FF>BURST</color>", 74, 120, Color.white, font);
        heading.fontStyle = FontStyles.Bold;
        detail = Text("Description", "Break the bricks. Keep the rhythm.\nThree hearts. One more try.", 32, 18,
            new Color(0.72f, 0.78f, 0.87f), font);
        detail.rectTransform.sizeDelta = new Vector2(840, 140);
        Panel("Button shadow", menuCard, new Vector2(0, -140), new Vector2(340, 76), new Color(0.08f, 0.35f, 0.43f));

        var buttonObject = new GameObject("Start or restart", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(menuCard, false);
        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(340f, 76f);
        buttonRect.anchoredPosition = new Vector2(0, -130);
        var image = buttonObject.GetComponent<Image>();
        buttonImage = image;
        image.color = new Color(0.35f, 0.9f, 1f);
        button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.highlightedColor = new Color(0.8f, 1f, 1f);
        colors.pressedColor = new Color(0.55f, 0.75f, 0.85f);
        button.colors = colors;
        button.onClick.AddListener(() => { if (IsGameOver || IsWon) Restart(); else Begin(); });
        var label = new GameObject("Button label", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(buttonObject.transform, false);
        buttonText = label.GetComponent<TextMeshProUGUI>();
        buttonText.font = font;
        buttonText.text = "START  >";
        buttonText.fontSize = 34;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = new Color(0.025f, 0.035f, 0.075f);
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.raycastTarget = false;
        buttonText.rectTransform.anchorMin = Vector2.zero;
        buttonText.rectTransform.anchorMax = Vector2.one;
        buttonText.rectTransform.offsetMin = buttonText.rectTransform.offsetMax = Vector2.zero;
        Text("Controls", "MOUSE   /   A-D   /   ARROW KEYS", 24, -220, new Color(0.76f, 0.83f, 0.92f), font);
        Text("Footer", "MOVE. BOUNCE. BREAK.", 20, -268, new Color(0.60f, 0.70f, 0.80f), font);
        ShowMenu();
    }

    private TMP_Text Text(string name, string content, float size, float y, Color color, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(menuCard, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        text.rectTransform.sizeDelta = new Vector2(1000, 90);
        text.rectTransform.anchoredPosition = new Vector2(0, y);
        return text;
    }

    private Image Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        image.rectTransform.anchoredPosition = position;
        image.rectTransform.sizeDelta = size;
        return image;
    }

    private void ShowMenu()
    {
        overlay.SetActive(true);
        revealTime = 0f;
        menuFade.alpha = 0f;
        menuCard.localScale = Vector3.one * 0.96f;
    }

    private void Update()
    {
        if (overlay == null || !overlay.activeSelf) return;
        revealTime = Mathf.Min(1f, revealTime + Time.unscaledDeltaTime / 0.24f);
        float ease = 1f - Mathf.Pow(1f - revealTime, 3f);
        menuFade.alpha = ease;
        menuCard.localScale = Vector3.one * Mathf.Lerp(0.96f, 1f, ease);
    }

    private void OnDestroy()
    {
        if (Active == this) Active = null;
        if (overlay != null) Destroy(overlay);
    }
}
