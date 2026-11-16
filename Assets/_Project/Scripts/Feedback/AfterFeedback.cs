using UnityEngine;
using TMPro;

// Presentation only: observes starter events; never writes physics or game state.
public sealed class AfterFeedback : MonoBehaviour
{
    [SerializeField] private Transform paddleVisual;
    [SerializeField] private Transform scoreVisual;
    [SerializeField] private Transform ball;
    [SerializeField] private Shader effectShader;
    [Header("Tuning")]
    [SerializeField, Range(0.05f, 0.4f)] private float squashAmount = 0.22f;
    [SerializeField, Range(0.08f, 0.5f)] private float squashDuration = 0.18f;
    [SerializeField, Range(0.05f, 0.3f)] private float scorePulse = 0.16f;
    [SerializeField, Range(0.1f, 0.5f)] private float pulseDuration = 0.24f;
    [SerializeField, Range(4, 24)] private int particleCount = 12;
    [SerializeField, Range(0.05f, 0.3f)] private float trailDuration = 0.14f;
    [SerializeField, Range(0f, 1f)] private float audioVolume = 0.5f;

    [Header("Polish / accessibility")]
    [Tooltip("Visual intensity only. Zero disables animated feedback; game rules are unchanged.")]
    [SerializeField, Range(0f, 1f)] private float effectIntensity = 0.85f;
    [SerializeField] private bool showLossMessage = true;
    [SerializeField] private Color accentColor = new Color(0.35f, 0.9f, 1f, 1f);

    private TMP_Text scoreLabel, lossLabel;
    [SerializeField] private LivesHearts heartsHUD;
    private Color scoreColor;
    private float lossTime;
    private AudioSource secondBrickAudio;
    private bool alternateBrickVoice;
    private readonly float[] melody = { 1f, 1.12246f, 1.25992f, 1.49831f, 1.68179f, 2f };
    private readonly LineRenderer[] rings = new LineRenderer[8];
    private readonly float[] ringTimes = new float[8];
    private readonly Color[] ringColors = new Color[8];
    private int nextRing;
    private bool initialized;

    private Vector3 paddleScale, scoreScale;
    private float squashTime, pulseTime;
    private int previousScore;
    private Material effectMaterial;
    private ParticleSystem particles;
    private TrailRenderer trail;
    private AudioSource paddleAudio, brickAudio, lossAudio;
    private AudioClip paddleClip, brickClip, lossClip, gameOverClip, victoryClip;
    private bool resetTrail;

    private void Awake()
    {
        if (paddleVisual == null || scoreVisual == null || ball == null || effectShader == null)
        {
            Debug.LogError("After Feedback needs its four scene references assigned.", this);
            enabled = false;
            return;
        }
        paddleScale = paddleVisual.localScale;
        scoreScale = scoreVisual.localScale;
        effectMaterial = new Material(effectShader);

        var particleObject = new GameObject("Brick particles (reused)");
        particleObject.transform.SetParent(transform, false);
        particles = particleObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 256;
        main.startLifetime = 0.38f;
        main.startSize = 0.1f;
        main.gravityModifier = 0.18f;
        particles.useAutoRandomSeed = false;
        particles.randomSeed = 402;
        var rotation = particles.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = 2.2f;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = false;
        var color = particles.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = effectMaterial;
        renderer.sortingOrder = 3;

        var trailObject = new GameObject("Ball trail");
        trailObject.transform.SetParent(ball, false);
        trail = trailObject.AddComponent<TrailRenderer>();
        trail.sharedMaterial = effectMaterial;
        trail.time = trailDuration;
        trail.minVertexDistance = 0.04f;
        trail.startWidth = 0.22f;
        trail.endWidth = 0f;
        trail.startColor = new Color(0.4f, 0.85f, 1f, 0.65f);
        trail.endColor = new Color(0.4f, 0.85f, 1f, 0f);
        trail.numCapVertices = 4;
        trail.numCornerVertices = 3;
        trail.sortingOrder = -1;
        trail.emitting = false;

        paddleAudio = MakeAudio("Paddle sound");
        brickAudio = MakeAudio("Brick sound");
        lossAudio = MakeAudio("Loss sound");
        secondBrickAudio = MakeAudio("Brick sound alternate");
        paddleClip = MakeTone("Paddle pop", 420f, 220f, 0.10f);
        brickClip = MakeTone("Brick chime", 660f, 660f, 0.18f);
        lossClip = MakeTone("Loss fall", 250f, 80f, 0.38f);
        gameOverClip = MakeGameOverTone();
        victoryClip = MakeVictoryTone();
        PreparePolish();
        initialized = true;
    }

    private void Start()
    {
        if (!initialized) return;
        if (heartsHUD != null) return;
        Transform lives = scoreVisual.parent.Find("LivesText");
        if (lives != null && lives.TryGetComponent<TMP_Text>(out var label))
        {
            heartsHUD = gameObject.AddComponent<LivesHearts>();
            heartsHUD.Configure(label);
        }
    }

    private void OnEnable()
    {
        if (!initialized) return;
        if (heartsHUD != null) heartsHUD.enabled = true;
        previousScore = GameManager.Instance != null ? GameManager.Instance.Score : 0;
        GameEvents.BallHitPaddle += OnPaddle;
        GameEvents.BrickDestroyed += OnBrick;
        GameEvents.ScoreChanged += OnScore;
        GameEvents.BallLost += OnLost;
        particles.Play();
        resetTrail = true;
    }

    private void OnDisable()
    {
        if (heartsHUD != null) heartsHUD.enabled = false;
        GameEvents.BallHitPaddle -= OnPaddle;
        GameEvents.BrickDestroyed -= OnBrick;
        GameEvents.ScoreChanged -= OnScore;
        GameEvents.BallLost -= OnLost;
        // Scene teardown may destroy these objects before this component is disabled.
        if (paddleVisual != null) paddleVisual.localScale = paddleScale;
        if (scoreVisual != null) scoreVisual.localScale = scoreScale;
        squashTime = pulseTime = lossTime = 0f;
        if (scoreLabel != null) scoreLabel.color = scoreColor;
        if (lossLabel != null) lossLabel.gameObject.SetActive(false);
        for (int i = 0; i < rings.Length; i++)
        {
            ringTimes[i] = 0f;
            if (rings[i] != null) rings[i].enabled = false;
        }
        if (secondBrickAudio != null) secondBrickAudio.Stop();
        if (trail != null) { trail.emitting = false; trail.Clear(); }
        if (particles != null) particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (paddleAudio != null) paddleAudio.Stop();
        if (brickAudio != null) brickAudio.Stop();
        if (lossAudio != null) lossAudio.Stop();
    }

    private void OnPaddle(ImpactInfo info)
    {
        squashTime = squashDuration;
        Ring(info.Point, accentColor);
        Play(paddleAudio, paddleClip, 0.65f);
    }

    private void OnBrick(ImpactInfo info)
    {
        // Deterministic radial burst: does not consume gameplay's Unity Random stream.
        int count = Mathf.RoundToInt(particleCount * effectIntensity);
        for (int i = 0; i < count; i++)
        {
            float angle = i * 2.399963f;
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            var emit = new ParticleSystem.EmitParams {
                position = info.Center,
                velocity = direction * (1.4f + (i % 3) * 0.5f) + (Vector3)info.Normal * 0.7f,
                rotation = i * 47f,
                startColor = info.Color,
                startSize = 0.08f + (i % 3) * 0.025f,
                startLifetime = 0.28f + (i % 3) * 0.06f
            };
            particles.Emit(emit, 1);
        }
        Ring(info.Center, info.Color);
        AudioSource voice = alternateBrickVoice ? secondBrickAudio : brickAudio;
        alternateBrickVoice = !alternateBrickVoice;
        voice.pitch = melody[Mathf.Clamp(info.Combo - 1, 0, melody.Length - 1)];
        Play(voice, brickClip, 0.45f);
    }

    private void OnScore(int score)
    {
        if (score > previousScore) pulseTime = pulseDuration;
        else { pulseTime = 0f; if (scoreVisual != null) scoreVisual.localScale = scoreScale; }
        previousScore = score;
        // HUD retains sole ownership of the displayed number.
    }

    public void PlayVictoryFeedback()
    {
        if (!initialized || !isActiveAndEnabled) return;
        brickAudio.Stop();
        secondBrickAudio.Stop();
        paddleAudio.Stop();
        Play(lossAudio, victoryClip, 0.8f);
        resetTrail = false;
        trail.emitting = false;
        trail.Clear();
        lossTime = 0f;
        if (lossLabel != null) lossLabel.gameObject.SetActive(false);
    }

    private void OnLost()
    {
        bool finalLife = AfterRound.Active != null && AfterRound.Active.IsGameOver;
        Play(lossAudio, finalLife ? gameOverClip : lossClip, 0.8f);
        lossTime = 0.85f;
        if (lossLabel != null)
        {
            lossLabel.text = finalLife ? "GAME OVER" : "BALL LOST";
            lossLabel.gameObject.SetActive(showLossMessage);
        }
        trail.emitting = false;
        trail.Clear();
        resetTrail = true;
    }

    private void LateUpdate()
    {
        squashTime = Mathf.Max(0f, squashTime - Time.deltaTime);
        pulseTime = Mathf.Max(0f, pulseTime - Time.deltaTime);
        float progress = 1f - squashTime / Mathf.Max(0.01f, squashDuration);
        // Fast compression followed by a small elastic rebound, ending exactly at rest.
        float squash = squashTime > 0f
            ? Mathf.Cos(progress * Mathf.PI * 1.5f) * Mathf.Pow(1f - progress, 2f) * effectIntensity : 0f;
        if (paddleVisual != null) paddleVisual.localScale = Vector3.Scale(paddleScale,
            new Vector3(1f + squashAmount * squash, 1f - squashAmount * squash, 1f));
        float pulse = Mathf.Clamp01(pulseTime / Mathf.Max(0.01f, pulseDuration));
        float easedPulse = pulse * pulse * (3f - 2f * pulse) * effectIntensity;
        if (scoreVisual != null) scoreVisual.localScale = scoreScale * (1f + scorePulse * easedPulse);
        if (scoreLabel != null) scoreLabel.color = Color.Lerp(scoreColor, accentColor, easedPulse);
        UpdatePolish();
        trail.startWidth = 0.22f * effectIntensity;
        trail.startColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.65f * effectIntensity);
        trail.time = trailDuration;
        // BallLost fires before Launch; wait until LateUpdate to clear at the new position.
        if (resetTrail) { trail.Clear(); trail.emitting = true; resetTrail = false; }
    }

    private AudioSource MakeAudio(string label)
    {
        var child = new GameObject(label);
        child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    private void Play(AudioSource source, AudioClip clip, float gain)
    {
        // Four bounded voices: paddle, two alternating chimes, loss. No unbounded PlayOneShot overlap.
        source.clip = clip;
        source.volume = audioVolume * gain;
        source.Play();
    }

    private static AudioClip MakeTone(string label, float from, float to, float duration)
    {
        const int rate = 44100;
        var samples = new float[Mathf.CeilToInt(rate * duration)];
        float phase = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / samples.Length;
            phase += 2f * Mathf.PI * Mathf.Lerp(from, to, t) / rate;
            float envelope = Mathf.Min(1f, i / (rate * 0.005f)) * Mathf.Pow(1f - t, 2f);
            // A soft second partial adds body without a harsh square-wave edge.
            samples[i] = (Mathf.Sin(phase) + 0.18f * Mathf.Sin(2f * phase)) * envelope * 0.36f;
        }
        var clip = AudioClip.Create(label, samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip MakeGameOverTone()
    {
        const int rate = 44100;
        const float slot = 0.28f;
        float[] notes = { 392f, 311.13f, 196f };
        var samples = new float[Mathf.CeilToInt(rate * slot * notes.Length)];
        for (int n = 0; n < notes.Length; n++)
        {
            int start = Mathf.RoundToInt(n * slot * rate);
            int count = Mathf.RoundToInt((slot - 0.025f) * rate);
            for (int i = 0; i < count && start + i < samples.Length; i++)
            {
                float progress = (float)i / count;
                float phase = 2f * Mathf.PI * notes[n] * i / rate;
                float envelope = Mathf.Min(1f, i / (rate * 0.006f)) * Mathf.Pow(1f - progress, 1.4f);
                samples[start + i] = (Mathf.Sin(phase) + 0.15f * Mathf.Sin(2f * phase)) * envelope * 0.36f;
            }
        }
        var clip = AudioClip.Create("Game over - three descending notes", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip MakeVictoryTone()
    {
        const int rate = 44100;
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
        var samples = new float[rate];
        for (int n = 0; n < notes.Length; n++)
        {
            int start = Mathf.RoundToInt(n * 0.17f * rate);
            int count = Mathf.RoundToInt((n == 3 ? 0.46f : 0.15f) * rate);
            for (int i = 0; i < count; i++)
            {
                float phase = 2f * Mathf.PI * notes[n] * i / rate;
                float envelope = Mathf.Min(1f, i / (rate * 0.006f))
                    * Mathf.Pow(1f - (float)i / count, 1.5f);
                samples[start + i] = (Mathf.Sin(phase) + 0.12f * Mathf.Sin(2f * phase)) * envelope * 0.36f;
            }
        }
        var clip = AudioClip.Create("Victory - ascending fanfare", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void PreparePolish()
    {
        scoreLabel = scoreVisual.GetComponent<TMP_Text>();
        if (scoreLabel != null) scoreColor = scoreLabel.color;
        for (int i = 0; i < rings.Length; i++)
        {
            var child = new GameObject("Impact ring " + i);
            child.transform.SetParent(transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = effectMaterial;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 32;
            line.sortingOrder = 2;
            line.enabled = false;
            rings[i] = line;
        }
        // Reuse the existing HUD font, without changing the original text or its owner.
        if (scoreLabel != null && scoreLabel.font != null)
        {
            var message = new GameObject("Ball loss cue", typeof(RectTransform));
            message.transform.SetParent(scoreVisual.parent, false);
            lossLabel = message.AddComponent<TextMeshProUGUI>();
            lossLabel.font = scoreLabel.font;
            lossLabel.fontSize = 24f;
            lossLabel.characterSpacing = 5f;
            lossLabel.alignment = TextAlignmentOptions.Center;
            lossLabel.raycastTarget = false;
            var rect = lossLabel.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 16f);
            rect.sizeDelta = new Vector2(500f, 40f);
            message.SetActive(false);
        }
    }

    private void Ring(Vector2 position, Color color)
    {
        if (effectIntensity <= 0f) return;
        int i = nextRing++ % rings.Length;
        rings[i].transform.position = position;
        ringColors[i] = color;
        ringTimes[i] = 0.20f;
        rings[i].enabled = true;
    }

    private void UpdatePolish()
    {
        for (int i = 0; i < rings.Length; i++)
        {
            ringTimes[i] = Mathf.Max(0f, ringTimes[i] - Time.deltaTime);
            if (ringTimes[i] <= 0f || effectIntensity <= 0f) { rings[i].enabled = false; continue; }
            float t = 1f - ringTimes[i] / 0.20f;
            float radius = Mathf.Lerp(0.07f, 0.4f, 1f - (1f - t) * (1f - t));
            for (int j = 0; j < 32; j++)
            {
                float a = j * Mathf.PI * 2f / 32;
                rings[i].SetPosition(j, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius);
            }
            Color c = ringColors[i];
            c.a = (1f - t) * 0.65f * effectIntensity;
            rings[i].startColor = rings[i].endColor = c;
            rings[i].widthMultiplier = 0.035f * (1f - t);
        }
        lossTime = Mathf.Max(0f, lossTime - Time.deltaTime);
        if (lossLabel != null)
        {
            lossLabel.gameObject.SetActive(showLossMessage && lossTime > 0f);
            lossLabel.color = new Color(1f, 0.65f, 0.42f, Mathf.Clamp01(lossTime / 0.25f));
        }
    }

    private void OnDestroy()
    {
        if (lossLabel != null) Destroy(lossLabel.gameObject);
        if (trail != null) Destroy(trail.gameObject);
        if (effectMaterial != null) Destroy(effectMaterial);
        if (paddleClip != null) Destroy(paddleClip);
        if (brickClip != null) Destroy(brickClip);
        if (lossClip != null) Destroy(lossClip);
        if (gameOverClip != null) Destroy(gameOverClip);
        if (victoryClip != null) Destroy(victoryClip);
    }
}
