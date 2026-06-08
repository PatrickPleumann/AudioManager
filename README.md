# AudioTool

**A zero-allocation audio management framework for Unity 6 — built to demonstrate clean architecture, strict test discipline, and disciplined AI-assisted development.**

> **What this repo demonstrates:** SOLID service design in C# · test-driven development with a frozen-test contract · zero-GC runtime patterns · and directing an AI coding agent against a written spec and a frozen test suite.

---

## Why this repo exists

This is a portfolio project. It is a working Unity audio framework — pooled `AudioSource` management, lightweight occlusion, fades, pause, follow — but the reason it is public is to show **how I build**: the architectural decisions, the testing discipline behind them, and how I use AI as a power tool without letting it erode code quality.

If you only read one section, read [Engineering discipline](#engineering-discipline) — it's where the actual decisions live.

---

## Architecture at a glance

A single `MonoBehaviour` singleton exposes a static API and drives per-frame ticks. Everything else is a **plain C# service** with one responsibility — no logic hides inside the MonoBehaviour.

```
AudioManagerDynamic            MonoBehaviour singleton · public API · LateUpdate driver
├── AudioPoolAcquisitionService   fixed AudioObject[] pool · slot handout + generation
├── AudioPlaybackService          dispatch (play / fade-in-silent) · volume resolve · handle gating
│   └── AudioStopService          the single "stop a slot" path (source stop + reset + wall-check stop)
├── AudioUniTaskWallCheckService  raycast loop via UniTask (recommended)   ┐ both only write
├── AudioCoroutineWallCheckService raycast loop via Coroutine (fallback)   ┘ TargetCutoff
├── AudioOcclusionSmoothingService glides filter cutoff toward TargetCutoff (per frame)
├── AudioFollowService            copies emitter position per frame — no re-parenting
├── AudioFadeService              drives all fades per frame via IFadeTarget[]
├── AudioPauseService             scope-aware global pause / unpause
└── AudioManagerDictionaryProvider  volume + layer-mask dictionaries
```

The two wall-check services sit behind one interface (`IAudioWallCheckService`) — a **Strategy** seam, so the async backend is swappable and the manager never touches a `CancellationToken`.

---

## Engineering discipline

### Test-driven, with a frozen-test contract

Every new method follows a fixed loop, and the rule that makes it honest is **step 2**:

1. Write the **failing test first** — it must be red before the implementation exists.
2. **That test is then frozen — never edited to make code pass.** New tests for new behaviour are fine; weakening an existing test to get green is not.
3. Write the method that turns the frozen tests green.
4. **Mutation check:** once green, deliberately introduce a bug and predict *which* test goes red. Proves the tests actually protect something — not tautological change-detectors.

Expected values are **hand-derived from the spec, not read off the implementation.** The occlusion test file says it out loud: *"If the implementation disagrees with these, it is wrong."*

### Pure logic, pulled out of Unity so it can be tested honestly

The math and policy decisions live in small **Unity-free** classes, unit-tested in EditMode without a running engine:

| Class | Responsibility |
|---|---|
| `AudioFadeMath` | fade curve / volume-over-time |
| `WallOcclusionMath` | per-wall cutoff step + floor clamp (the swappable occlusion model seam) |
| `OcclusionSmoothing` | per-frame glide toward target cutoff |
| `LowPassDispatchPolicy` | filter on/off state per dispatch |
| `AudioHandleValidator` | handle currency: bounds + generation |

This is a deliberate trade-off in favour of **honest tests**: where the choice was a testable seam (an interface/pure class checkable in fast EditMode) versus logic only reachable through slow, vague PlayMode, the seam wins. The result is an **EditMode suite of ~50 tests across the pure-logic layer**.

### Zero allocations at runtime

- **Fixed, pre-instantiated `AudioObject[]` pool** — no `Instantiate`/GC during play.
- **`Physics.RaycastNonAlloc`** into a reused buffer for the wall check — no per-frame array churn.
- **`AudioHandle` is a `readonly struct`** `{ PoolIndex, Generation }` — a value-type ticket, not a heap reference.

### Stale-handle safety via generations

Each slot carries a `Generation` counter, bumped on every (re)acquisition. The `AudioHandle` you receive carries the generation it was minted at. `Stop` / `FadeOut` validate **bounds *and* generation** before acting — so an old handle whose pool slot has since been reused becomes a silent no-op instead of stopping a stranger's sound. `O(1)` stop, with correctness guaranteed. (Slot *acquisition* is a deliberate `O(n)` scan — simple, and cheap against a small fixed pool.)

---

## Feature highlights

- **Intelligent pooling** — pre-cached `AudioSource` pool; OneShot slots locked via a `BusyUntilTime` timestamp so short SFX never get cut off early.
- **Lightweight wall occlusion** *(opt-in)* — raycasts from the listener to the source; each obstructing layer lowers an `AudioLowPassFilter` cutoff, clamped to a floor. Soft transitions via per-frame smoothing — no audible "pop" when stepping out of cover. This is intentionally a **lightweight occlusion model, not a full spatializer** (Steam Audio / Oculus) — a feature, not a shortcut.
- **Fade family** — `FadeIn` / `FadeOut` / `Crossfade`, spatial and non-spatial. `Crossfade` is pure composition of `FadeOut + FadeIn`, not a special-cased path.
- **Follow without re-parenting** — spatial sounds track an emitter by copying its position per frame, never via `SetParent` (which would hand a pooled slot to the caller and let it die with the emitter).
- **Scope-aware global pause** — `PauseAll` / `UnpauseAll`, with per-sound opt-out of global pause.
- **Hybrid async backend** — uses allocation-free UniTask when present, and falls back to native Unity coroutines when it is absent. Selected at compile time via an Assembly Definition `versionDefines` gate (`USE_UNITASK`), so the build is safe either way.

---

## Public API

One call plays a sound; the framework owns the `AudioSource` lifecycle from there.

```csharp
// Play
AudioHandle h = AudioManagerDynamic.PlaySpatial(myAdo, sourceTransform); // 3D, positional, optional wall-check
AudioHandle h = AudioManagerDynamic.PlaySpatial(soundRequest);           // same, bundled { Ado, Source }
AudioHandle h = AudioManagerDynamic.PlayNonSpatial(myAdo);               // 2D (UI, music, stingers)

// Stop (no-op unless the handle is still current)
AudioManagerDynamic.Stop(h);

// Fade — always managed, so it always returns a usable handle
AudioHandle h = AudioManagerDynamic.FadeInSpatial(myAdo, sourceTransform, duration);
AudioManagerDynamic.FadeOut(h, duration);                                // ramps down, stops, frees the slot
AudioHandle h = AudioManagerDynamic.CrossfadeSpatial(fromHandle, toAdo, sourceTransform, duration);

// Pause
AudioManagerDynamic.PauseAll();
AudioManagerDynamic.UnpauseAll();
```

Sounds are configured on an `AudioDataObject` (a `ScriptableObject` "control surface") — clips, volume category, spatial blend and flags — which is mirrored onto the pooled `AudioSource` on every dispatch.

---

## Built with AI — under engineering discipline

I built this in partnership with an AI coding agent (Claude). I think how that was done matters more than the fact of it, so I'll be specific:

- **The agent works against a written contract, not vibes.** A `CLAUDE.md` in the repo encodes the architecture, the design invariants, and the rules of collaboration. The AI is steered by it — it doesn't get to redefine the design mid-task.
- **The frozen-test loop constrains the AI, not the other way round.** Tests are written red-first and then frozen. The AI's job is to make a *fixed* specification pass — it cannot quietly weaken a test to get green, because the rule forbids it and the mutation check would expose a test that protects nothing.
- **Architecture decisions are mine.** The pure-logic seams, the Strategy split for the async backend, the no-re-parenting follow model, the generation guard against stale handles — those are deliberate calls, made for testability and correctness, then implemented with AI assistance.

The point is simple: the engineering judgement stays human, and the AI accelerates the execution of a disciplined process.

---

## Status & scope

Actively developed, heading toward a Unity Asset Store release. The foundation (pooling, occlusion + smoothing, fade family, pause, follow) is in place and test-backed; feature breadth is being expanded deliberately, never at the cost of the testing discipline above.

**Deliberately *not* in scope:** a full HRTF spatializer. The wall check is lightweight occlusion by design.

**Environment:** Unity 6 · C# · JetBrains Rider.
