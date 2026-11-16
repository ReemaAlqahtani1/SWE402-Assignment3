# Brick Burst — SWE402 Assignment 3: Improving Game Feel

- **Student:** Reema AlQahtani
- **Student ID:** 202280860
- **Unity version:** 6000.3.2f1 (Unity 6.3 LTS)
- **Demonstration video:** **ADD YOUR ACCESSIBLE 2–3-MINUTE NARRATED VIDEO LINK HERE**
- **Playable Web version:** [Play Brick Burst](https://play.unity.com/api/v1/games/game/e0c3ada3-5d45-466d-890a-6514e348b511/build/latest/frame)

## Overview

Brick Burst improves the supplied JuiceLab-Dead-Starter with visual, audio, and interface feedback. The intended feel is playful, bouncy, and responsive, with short effects that make collisions, scoring, and life loss easier to notice.

## Run instructions and controls

1. Download or clone this repository. In Unity Hub, select **Add project from disk** and choose the project folder containing `Assets`, `Packages`, and `ProjectSettings`.
2. Open it with **Unity 6000.3.2f1** and allow package installation and asset import to finish.
3. Open `Assets/_Project/Scenes/Before.unity` for the original baseline, or `Assets/_Project/Scenes/After.unity` for the improved version.
4. Set the Game view to **16:9**, press Play, and focus the Game view.
5. Move the paddle with the **mouse**, **A/D**, or **left/right arrow keys**.
6. Before launches automatically. In After, click **START**. Losing all three lives shows **GAME OVER**; clearing the bricks shows **YOU WIN!**. **RESTART** or **PLAY AGAIN** begins a fresh round.

Stop Play mode before switching scenes. The Web version is configured to start in After; both scenes are available in the Unity project.

## Five improvements

Paths below are relative to `Assets/_Project/`.

| Improvement | Trigger | Implementation file or object | Purpose |
|---|---|---|---|
| Brick impact particles | `BrickDestroyed` | `Scripts/Feedback/AfterFeedback.cs`, `OnBrick`; reusable ParticleSystem | A brief burst at the destroyed brick confirms the impact and fades away. |
| Reactive paddle animation | `BallHitPaddle` | `AfterFeedback.cs`, `OnPaddle` and `LateUpdate`; `Paddle/Visual` | Squash/stretch makes contact feel responsive, then restores the normal appearance without modifying the collider. |
| Event-based audio | Paddle contact, brick destruction, and ball loss | `AfterFeedback.cs`; generated clips and AudioSources | Distinguishable sounds communicate the three events. |
| Score feedback | `ScoreChanged` when points increase | `AfterFeedback.cs`, `OnScore` and `LateUpdate`; `Canvas/ScoreText` | A brief scale pulse and color emphasis draw attention to the updated score. The original HUD owns the score value. |
| Ball trail | Ball movement, cleared after respawn | `AfterFeedback.cs`; runtime TrailRenderer | A short tapered trail makes the ball's direction easier to follow. |

## Tuning and implementation

The **After Feedback** Inspector exposes squash amount and duration, score-pulse strength and duration, particle count, trail duration, audio volume, and visual-effect intensity.

The effects use short durations to keep the ball readable. Squash and score pulses return to their original scales; repeated triggers restart bounded animations. The trail clears when the ball respawns to avoid a line across the board. Brick sounds use two alternating voices to reduce abrupt cutoffs during successive hits.

Animations are implemented in C#, rather than Animator Controllers or animation clips. The feedback component subscribes to the starter's events when enabled and unsubscribes when disabled. `Feedback/FeedbackUnlit.shader` renders the particles, trail, and impact rings without depending on scene lighting.

## Playtest feedback and revision

Playtest feedback indicated that the remaining lives were difficult to notice and that a visual indicator would be clearer than a number. I replaced the numeric life display in After with three coral hearts beside the **LIVES** label. As lives are lost, fewer hearts remain active.

Feedback also identified the start screen's appearance as an area for improvement. I revised its layout, colors, text, and Start button to create a more appealing arcade presentation.

These revisions make the remaining attempts more visible and give the game a clearer opening screen. A separate follow-up tester assessment of these revisions has not been recorded.

## Testing

I ran and recorded Before and After gameplay in Unity. My After recording includes a completed board and victory, and a separate losing round, with game audio. During development, runtime errors involving a destroyed UI reference and the paddle sprite geometry were identified and corrected. The feedback scripts also passed C# compilation checks against Unity 6000.3.2f1 libraries.

A Web build was published on Unity Play at the link above. Later camera and HUD-position adjustments require a fresh build and upload to appear in the published version; the link alone does not confirm that the latest local changes are deployed.

This records the checks performed, rather than claiming exhaustive testing. Multi-browser testing, extended performance testing, and a documented external retest of the final revision have not been completed.

## Additional presentation and round-flow changes

- Hearts replace the numeric lives display in After.
- Bricks and the paddle have generated beveled surfaces, highlights, and shadows.
- Start, game-over, and victory screens use coordinated colors, readable text, final-score displays, and replay buttons.
- Impact rings and a victory sound provide additional feedback.

Before remains the baseline. Shared core and gameplay scripts were retained. Active-round movement, ball-bounce calculations, colliders, scoring, and level layout remain from the starter.

**Round-flow differences:** After waits for Start, stops on game over or victory, and rebuilds the board on replay. These are intentional changes to the starter's round flow, beyond presentation-only feedback.

## AI use

I used **OpenAI ChatGPT/Codex** to help explain the assignment, inspect the starter, implement and debug C# feedback and UI components, create the unlit shader and procedural sounds, and draft documentation. I also learned how an unlit shader keeps effect colors independent of scene lighting.

I reviewed the results in Unity, recorded gameplay with audio, reported runtime errors for correction, and requested revisions to the lives display, menus, text readability, and paddle and brick appearance. AI-assisted compilation and code inspection were used alongside my Unity testing; they were not treated as substitutes for playtesting.

## Credits

- Course starter: [JuiceLab-Dead-Starter](https://gamedevkfupm.github.io/swe402/individual%20assignments/game-feel/JuiceLab-Dead-Starter.zip), adapted from Serkan Ceyhan's MIT-licensed JuiceLab.
- Original attribution and license information are retained in `SOURCE.md` and `LICENSE`.
- Existing starter assets and their licenses are retained. Additional surfaces, hearts, and sounds were generated through code with AI assistance; no additional downloaded art or audio was used.

## Repository contents

The Unity project includes `Assets` (with `.meta` files), `Packages`, and `ProjectSettings`. Generated folders such as `Library`, `Temp`, `Logs`, `Obj`, and Web build output should be excluded from version control. The playable link is supplementary; the narrated demonstration video is linked separately above.
