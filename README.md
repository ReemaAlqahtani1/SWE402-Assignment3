# SWE402 Assignment 3 — Improving Game Feel

**Status: implemented, awaiting Unity validation and human playtest. Not submission-ready yet.**

- Student name: **TODO**
- Student ID: **TODO**
- Required editor: **Unity 6000.3.2f1** (unchanged ProjectVersion.txt).
- Intended feel: **playful and bouncy, with brief, readable feedback**.
- Playable Web version: [Play Brick Burst on Unity Play](https://play.unity.com/api/v1/games/game/e0c3ada3-5d45-466d-890a-6514e348b511/build/latest/frame).
- Narrated demonstration video: **TODO — add an accessible 2–3-minute video link**.
- Due: November 16, 2026, 2:00 PM Riyadh (UTC+3), per assignment brief.

## Run

1. Add this folder (containing Assets, Packages and ProjectSettings) through Unity Hub → Add project from disk.
2. Open with 6000.3.2f1 and allow package download/import to finish. Resolve any Console errors before recording.
3. Open `Assets/_Project/Scenes/Before.unity`, select 16:9, press Play, and focus Game view.
4. Move with mouse, A/D, or left/right arrows. Before launches automatically.
5. Stop Play, open `Assets/_Project/Scenes/After.unity`, press Play, then click **START**. After now waits for this button before launching.
6. In After, three losses show **GAME OVER**, the final score and **RESTART**. Restart starts a fresh board with score zero and three lives. Before retains the original automatic lives/score reset without rebuilding bricks.

Both scenes are included in Build Settings. No standalone build is required. Use the editor to switch scenes.

## Five improvements

All code is in `Assets/_Project/Scripts/Feedback/AfterFeedback.cs`, attached only to the **After Feedback** object in After. The shader is `Assets/_Project/Feedback/FeedbackUnlit.shader`.

| Improvement | Trigger | Implementation file or object | Purpose |
|---|---|---|---|
| Brick particles | BrickDestroyed | AfterFeedback.OnBrick; reusable ParticleSystem | A short, brick-colored burst confirms destruction at the brick center. |
| Paddle visual squash | BallHitPaddle | AfterFeedback.OnPaddle/LateUpdate; Paddle/Visual | Brief width stretch and height squash communicates contact; the collider and root scale stay unchanged. |
| Three event sounds | BallHitPaddle, BrickDestroyed, BallLost | AfterFeedback.MakeTone/Play; three AudioSources | Distinct pop, rising chime, and falling tone identify each event. |
| Score pulse | ScoreChanged, only when score increases | AfterFeedback.OnScore/LateUpdate; Canvas/ScoreText | Scale pulse emphasizes points while the original HUD owns the displayed score. |
| Ball trail | Ball movement; reset on BallLost | AfterFeedback.Awake/LateUpdate; runtime Ball trail | Short tapered trail communicates direction without obscuring the ball. |

Brick destruction combines sound and particles, plus the accompanying score pulse.

## Initial tuning and implementation decisions

These are initial design choices, **not playtest-derived revisions**:

- Squash: 22%, returning over 0.18 seconds; visual child only.
- Score pulse: +16%, returning over 0.24 seconds; repeated hits restart a bounded pulse.
- Particles: 12 per brick; 0.28–0.40 second lifetime; one reused emitter capped at 256 particles.
- Trail: 0.14 seconds, 0.22 world-unit starting width. Cleared after the starter relocates the ball to prevent a teleport streak.
- Master audio volume: 0.5, with per-event gains. Four bounded voices (paddle, two alternating brick voices, loss) keep overlap controlled. Brick tones follow a capped pentatonic progression using the starter combo value, without changing score or gameplay.
- Synthesized mono sounds use short fades and no external audio assets. Shader is directly referenced by the scene.
- All subscriptions are paired with unsubscriptions; disabling the component restores visual scales, clears effects, and stops audio.
- The feedback effects do not consume `UnityEngine.Random` or modify gameplay state. The separate AfterRound controller deliberately changes round start/end behavior: it waits for START, owns the After life count, stops ball/paddle at zero lives, and reloads After on RESTART. Movement, bounce, brick scoring and layout during an active round remain from the starter. No camera shake is used.

## Verification status

See `VERIFICATION.md` for actual static-check results. **The polished C# code compiles against Unity 6000.3.2f1 libraries.** The user reported that the previous version’s five effects worked and that the teardown error was gone. The new polish has not yet been visually or audibly tested in Play mode. An isolated headless Unity test could not run because local service connections were blocked. No external human playtest has been documented.

Complete this checklist in Unity and replace Pending with an observed result (including any fix and retest):

| Test | Expected result | Actual result |
|---|---|---|
| Import using 6000.3.2f1 | No compile, missing-reference, or shader errors | Pending |
| Before baseline | Silent; no added effects; paddle bounce, brick removal, points and lives work | Pending |
| After paddle contact, repeated | Sound and squash; normal scale returns; collider unchanged | Pending |
| After brick destruction, repeated | Colored burst at brick; chime; particles expire and remain bounded | Pending |
| Rapid score additions | Correct points; pulse returns to normal without growing | Pending |
| Ball movement and deliberate loss | Trail readable; loss tone; no trail line across respawn | Pending |
| Three lives lost in After | Hearts and LIVES reach zero; ball/paddle stop; final score remains; three-note sound and RESTART appear | Pending |
| START / RESTART | No movement before START; RESTART restores all bricks, score zero, three lives, then launches a new round | Pending |
| Several minutes of play | Ball readable; effects bounded; audio comfortable without harsh clicks | Pending |
| Mute audio (audioVolume = 0) | Brick removal/particles and life counter/ball reset still communicate events | Pending |
| Disable/re-enable After Feedback in Play | Original scales restored; no duplicate callbacks or lingering audio | Pending |
| Stop/restart Play three times, reopen Before | No duplicate effects; Before remains unpolished | Pending |

## Required external playtest and revision

**Not conducted. Do not replace these fields with invented observations.** Ask one other person to play both scenes; omit their name.

- Date: TODO
- Which version felt more satisfying, and why: TODO
- Which feedback made events clearer: TODO
- Anything distracting or hard to notice: TODO
- Revision made because of that feedback (parameter/file, old → new): TODO
- What was observed after retesting the revision: TODO

## AI use and credits

OpenAI Codex assisted with inspecting the official course starter, implementing the feedback C# script and shader, wiring the After scene, synthesizing three event tones in code, writing documentation, and static integrity checks. The polished C# output was compiled against the required Unity libraries; visual/audio verification of the polish and an external human playtest are still pending. Student review, actual testing, and final tuning must be recorded above before submission.

The project is based on the course's [JuiceLab-Dead-Starter](https://gamedevkfupm.github.io/swe402/individual%20assignments/game-feel/JuiceLab-Dead-Starter.zip), adapted from Serkan Ceyhan's MIT-licensed JuiceLab. Original `SOURCE.md` and `LICENSE` are retained. Existing starter fonts/assets and their license files are retained. No completed upstream feedback modules were imported. New feedback code, shader and procedural tones were generated with AI assistance; no extra downloaded art/audio assets were used.

## Submission

Finish the checklist, real playtest and revision, student details, and video link. Follow `VIDEO-PLAN.md`. Upload one GitHub repository containing Assets, Packages, ProjectSettings and this README. Retain attribution/license files and `.gitignore`; exclude Library, Temp, Logs and generated caches. Verify repository/video access, then submit the repository link on Blackboard. This folder has not been published to GitHub or submitted.

## Additional polish — November 15

Intended feel remains playful and bouncy, with a consistent cyan accent and short effects.

- Paddle response now includes a small, damped rebound, returning exactly to the cached base scale. Only the visual child is animated.
- Brick fragments include directional movement, gentle gravity and rotation. Existing particles are reused, not instantiated per hit.
- An eight-slot impact-ring pool gives paddle and brick contact a brief expanding outline (0.20 seconds). It complements the particles and does not count as a replacement for any required improvement.
- Brick audio rises through a capped pentatonic sequence based on the existing combo. Two alternating voices avoid immediately cutting off every preceding brick sound. Synthesized tones include a soft second harmonic.
- Score pulse uses smooth easing and a brief cyan tint; HUD still owns the actual score.
- Trail corners and caps are rounded, and clearing on respawn is retained.
- A brief BALL LOST message beneath the play area supports muted play. It uses the existing score font, when available, and never intercepts pointer input.
- **Effect Intensity** (default 0.85) controls the animated visual feedback. Set to zero for a reduced-effects comparison; use **Audio Volume** to mute sound and **Show Loss Message** to toggle the cue. No camera shake or hit-stop is used.

These are AI-assisted design revisions, not changes claimed to come from another person's feedback. See POLISH-CHECKLIST.md for the remaining review.

## Lives hearts

After now displays vector hearts beside the original LIVES number. Full hearts are coral; spent hearts are dim. LivesHearts subscribes to LivesChanged without changing the number owned by HUD. HeartIconGraphic draws the icons without requiring a font glyph. All required references and existing scenes are preserved.

After now follows 3 → 2 → 1 → 0, with dim hearts indicating spent lives. The AfterRound controller is the source of the After life count; the existing HUD displays LivesChanged events. GameManager continues to manage scoring and combo. Before still uses its original GameManager/DeathZone life loop.

## Start / Game Over / Restart — After only

The user explicitly requested these features in After rather than a third scene. `AfterRound.cs` replaces the DeathZone component only in After. Before and all shared core/gameplay script files remain unchanged.

- START holds ball physics and paddle input until clicked; it then enables the starter ball launch.
- First and second losses decrement hearts/LIVES and relaunch the ball.
- Third loss stops physics and paddle input, preserves final score, displays GAME OVER and plays three descending notes through the existing loss AudioSource (respects Audio Volume).
- RESTART reloads After, rebuilding all bricks and resetting score, lives and effect objects. It starts the fresh round immediately without another START click. Repeated clicks are guarded.
- No global time-scale change is used, so audio, UI and effect fades continue normally while the round is stopped.

**Assignment deviation:** waiting for START, stopping at zero lives, and rebuilding the board on RESTART differ from the course starter's rules. These are intentional user-requested changes, not merely presentation feedback. They may affect the unchanged-gameplay criterion and bonus eligibility; this README does not claim instructor approval.

The code compiles against the required editor libraries; actual UI rendering, input, audio and round transitions still require Play-mode testing. No external tester feedback has been fabricated.

The three heart icons are now saved directly in After under Canvas/LivesText (Life Heart 1–3), with explicit CanvasRenderer components and direct controller references. They are visible scene UI objects, rather than only created during Play. The LIVES number remains. Runtime appearance still needs confirmation.

## Visual direction — BRICK BURST

After's life display now uses the label LIVES with three larger coral vector hearts to its right, with no numeric life count. Filled hearts show remaining lives and dim hearts show spent lives. LivesHearts owns this After-only presentation after HUD events; the underlying round count remains unchanged.

Start and Game Over share a navy arcade card, colored brick motif, cyan/coral accents, a dimensional action button, controls hint, and a restrained 0.24-second reveal. Game Over emphasizes the final score. This is generated Unity UI, with no downloaded artwork.

AfterBrickLook generates a reusable bevelled, lightly grained brick sprite after LevelBuilder has populated the board. Original row colors, transforms, shared prefab and collision boxes remain unchanged. Only After's instantiated brick sprites are restyled; Before is untouched. Runtime texture and sprite are released on unload.

C# compilation passed. The new menu, hearts and brick surfaces still require visual Play-mode review; no human playtest or visual verification is claimed.

## Victory — After only

After listens to BrickDestroyed and checks the board on the following frame, after the starter destroys the brick. Clearing the final brick freezes ball/paddle, keeps final score and remaining hearts, and displays a gold YOU WIN! / HOORAY! ALL BRICKS CLEARED screen with PLAY AGAIN. A four-note ascending fanfare uses the existing audio-volume setting and bounded loss voice. PLAY AGAIN reloads a fresh board with zero score and three lives. No victory is triggered just by starting an empty scene. This is an additional intentional round-rule change in After; Before remains unchanged. Compilation passed; actual full-board clear, audio and replay verification remain pending.

## Paddle and menu readability polish

AfterPaddleLook adds a generated beveled paddle surface with cyan endcaps, a bright upper edge and a shaded underside. Original sprite bounds, transforms and colliders are preserved. Menu subtitles, controls, button labels and final-score text are larger and secondary text has higher contrast. No imported artwork is used.
