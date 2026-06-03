# AudioTool — Documentation

---

## What is AudioTool?

**AudioTool** is a lightweight audio management framework for Unity that takes care of all `AudioSource` handling for the developer. Instead of manually instantiating, configuring and managing `AudioSource` components, a single call is all it takes — the system handles the rest.

---

## Advantages

- **No manual AudioSource management** — The tool manages a pre-allocated pool of `AudioSource` objects. No new pool objects are instantiated at runtime, avoiding garbage collection and performance spikes.

- **Automatic wall occlusion (Wall Check)** — Sounds originating behind walls or obstacles are automatically muffled using an `AudioLowPassFilter`. Multiple walls between the sound and the player muffle it further — configurable per Unity layer.

- **Organised volume system** — Sounds are categorised (e.g. Ambient, SFX, Player). Each category has its own `AudioSourceVolume` asset whose value can be overwritten at runtime — ideal for volume sliders in a settings menu.

- **2D and 3D sounds** — Spatial sounds (`PlaySpatial`) play at a world position, are attenuated by distance and optionally wall-checked. Non-spatial sounds (`PlayNonSpatial`) play everywhere at equal level — ideal for UI clicks, music or global stingers.

- **Fire-and-forget or controllable** — The developer decides per sound whether to receive an `AudioHandle` to stop the sound manually later, or whether the sound simply plays through.

- **Simple API** — Playing and stopping sounds is done via self-explanatory static methods. No boilerplate code.

- **ScriptableObject-driven** — All configuration is done via assets in the Inspector. No code required to integrate new sounds.

- **UniTask-based** — Internal async logic uses UniTask for minimal overhead. A Coroutine fallback is available automatically.

---

## Setup & Installation

### Prerequisites

- Unity 6 or higher
- An **AudioListener** in the scene (by default on the Main Camera). Without an AudioListener the `AudioManagerDynamic` disables itself and logs an error.
- **UniTask** (version **2.3.0** or higher) — recommended, must be installed separately:
  [https://github.com/Cysharp/UniTask](https://github.com/Cysharp/UniTask)
  *(If UniTask is not installed, or an older version is present, the tool automatically switches to the Coroutine variant. It works fully but is not recommended.)*

---

### Step 1 — Add AudioManagerDynamic to the scene

Create an empty GameObject in your scene and add the `AudioManagerDynamic` component. This object is the central entry point of the tool and must be present **once per scene**. Additional instances are automatically detected and destroyed.

---

### Step 2 — Assign the System Config

The included `AudioSystemConfig` asset is already fully pre-configured. Assign it to the **System Config** field on the `AudioManagerDynamic` GameObject in the Inspector.

> **Important:** If no System Config is assigned, the AudioManager disables itself on start and logs an error.

> All fields of the System Config are covered in detail in a dedicated section later in this documentation.

---

## AudioSystemConfig — Reference

The `AudioSystemConfig` asset is the central configuration file of the tool. All values can be adjusted in the Inspector.

---

### General Values

| Field | Description | Recommendation |
|---|---|---|
| **Number Of Audio Sources** | Number of pre-allocated `AudioSource` objects in the pool. The more sounds you expect to play simultaneously, the higher this value should be. Too many objects increase memory usage. | 20–50 for most projects |
| **Default Cutoff Freq Value** | The default frequency value of the `AudioLowPassFilter` when no wall contact is detected. At this value the sound plays normally without any filtering. | 5000 – 5007 |
| **Min Cutoff Freq Value** | The lower limit of the cutoff frequency. The frequency will never drop below this value — regardless of how many walls are between the sound and the player. Set to 10 if sounds should become completely inaudible behind many walls. | 10 – 1000 |

---

### Wall Check Interval

| Field | Description | Recommendation |
|---|---|---|
| **Time Interval Between Position Checks** | The interval in seconds between two wall-check raycasts. Smaller values react faster but cost more performance. | 0.1 – 0.25 |

---

### Cutoff Frequencies Per Layer

A list of Unity layers that count as walls, each with a reduction value. For every layer hit by the raycast the cutoff frequency is reduced by the defined value. Multiple hits are accumulated (up to 8 walls per raycast are taken into account).

| Field | Description |
|---|---|
| **Single Layer** | The Unity layer that counts as a wall. |
| **Cutoff Frequency Value** | The value by which the cutoff frequency is reduced per hit. |

---

### References

| Field | Description |
|---|---|
| **Transfer Object** | The included `AudioVolumesTransferObject` asset. Contains all `AudioSourceVolume` assets managed by the system. |
| **Audio GameObject Prefab** | The included audio source prefab. Instantiated for each pool slot. Do not change this field. |

---

## Performance Tips

### Pool Size

**Number Of Audio Sources** directly affects memory usage. All pool objects are instantiated when the scene starts — even if they are never used. Set the value as low as possible while still being high enough to cover all simultaneously expected sounds.

> Rule of thumb: Start with a low value and increase if needed rather than setting it too high from the start.

---

### Wall Check Interval

**Time Interval Between Position Checks** determines how often per second a raycast is fired. For the human ear the difference between 0.1 and 0.25 seconds is barely perceptible — but the performance difference with many simultaneous wall-check sounds is noticeable.

> Rule of thumb: 0.25 is a good default. Only go lower if a very fast response to wall contact is needed.

---

### Follow Emitter

**Follow Emitter** in the ADO should only be enabled if the sound truly needs to move with an object — e.g. a moving vehicle. The sound then tracks the position of the provided Transform every frame. For short sounds like gunshots or explosions this option is unnecessary and costs one position update per frame.

> Rule of thumb: Always leave disabled for short sounds.

---

### Use Wall Check

**Use Wall Check** should only be enabled if the sound can realistically originate behind a wall. For sounds that always play in open areas — e.g. UI sounds or music — the wall check is unnecessary raycast overhead. (It only takes effect on spatial sounds played via `PlaySpatial` anyway.)

> Rule of thumb: Leave disabled by default and only enable where needed.

---

### Number of simultaneous Wall Checks

Every active sound with **Use Wall Check** fires a raycast at regular intervals. With many simultaneously playing sounds with wall check enabled this adds up quickly. It is worth considering which sounds truly need a wall check and which do not.

> Rule of thumb: Only enable wall check for sounds that can realistically be near walls — e.g. enemy voicelines, but not player footsteps.

---

## Setting up the Volume System

### Step 3 — Adapt the AudioCategory

`AudioCategory` is an enum that defines the available volume categories. It can be found at:
> `AudioFramework/Core/AudioCategory.cs`

The existing values are **example values only** and should be fully replaced with your own categories:

```csharp
namespace AudioFramework.Core
{
    public enum AudioCategory
    {
        // Example values — replace entirely with your own categories:
        Ambient = 1,
        Music,
        SFX,
        // ...
    }
}
```

> **Note:** Once defined, enum values should not be reordered as this affects the associated `AudioDataObject` and `AudioSourceVolume` assets. New values can safely be added at the end at any time. The first enum value must always be explicitly set to `= 1`.

---

### Step 4 — Create AudioSourceVolume assets

The tool manages volumes through `AudioSourceVolume` assets. Each asset represents a volume category from `AudioCategory`.

Create a new asset for each desired category via:
> **Right-click in the Project window → Create → Scriptable Objects → AudioSourceVolume**

| Field | Description |
|---|---|
| **Current Audio Type** | The category of this asset — must match the `AudioCategory` value in the corresponding `AudioDataObject`. |
| **Volume** | The default volume value (0.0 – 1.0). This value can be overwritten at runtime, e.g. by a settings slider. |

---

### Step 5 — Populate the AudioVolumesTransferObject

The `AudioVolumesTransferObject` is already included in the provided `AudioSystemConfig` asset and does not need to be created manually.

Click on the `AudioVolumesTransferObject` in the Inspector and press the **Populate Array** button. The system will automatically find all existing `AudioSourceVolume` assets in the project and populate the array.

> **Important:** This step must be repeated every time new `AudioSourceVolume` assets are added.

---

## Configuring the Wall Check

### Step 6 — Define Unity Layers

The Wall Check uses Unity layers to determine which objects count as walls. First define the desired layers in Unity:
> **Edit → Project Settings → Tags and Layers**

Examples for useful layer names: `WallThick`, `WallThin`, `WallGlass`.

---

### Step 7 — Configure Cutoff Frequencies Per Layer

Open the `AudioSystemConfig` asset in the Inspector. Under **Cutoff Frequencies Per Layer** you can define a reduction value for each wall layer.

Click **+** to add a new entry and assign the following values:

| Field | Description |
|---|---|
| **Single Layer** | The Unity layer that counts as a wall. |
| **Cutoff Frequency Value** | The value by which the cutoff frequency is reduced when this layer is hit by the raycast. |

**Example:**

| Layer | Reduction | Result at Default 5000 |
|---|---|---|
| WallThin | 500 | 4500 |
| WallThick | 2000 | 3000 |
| WallThin + WallThick | 500 + 2000 | 2500 |

---

### Step 8 — Configure the minimum value

The **Min Cutoff Freq Value** field in the `AudioSystemConfig` asset defines the lower limit of the cutoff frequency. The frequency will never drop below this value regardless of how many walls are between the sound and the player.

| Scenario | Recommended value |
|---|---|
| Sound should always remain audible | 500 – 1000 |
| Sound should become inaudible behind many walls | 10 (Unity's absolute minimum) |

> **Note:** Unity's `AudioLowPassFilter` accepts a minimum value of 10 Hz. A value of 10 makes the sound practically inaudible to the player.

---

## AudioDataObject (ADO)

The `AudioDataObject` (ADO for short) is the central configuration object for every sound. Create a new ADO via:
> **Right-click in the Project window → Create → Scriptable Objects → AudioDataObject**

| Field | Description |
|---|---|
| **Current Clips** | The AudioClips used for this sound. An ADO always represents exactly one sound category — e.g. multiple footstep variations or a single explosion. If multiple clips are assigned, the system randomly selects one each time the sound is played. A single clip is perfectly fine. |
| **Current Type** | The volume category of this sound — must match an existing `AudioCategory` value. |
| **Spatial Blend** | The spatialization of the sound (0.0 – 1.0). 1 = full 3D (positional, attenuated by distance), 0 = full 2D (same level everywhere). This value only takes effect when playing with `PlaySpatial(ado, transform)`. `PlayNonSpatial(ado)` always forces 2D (0) and ignores this value. |
| **Follow Emitter** | If enabled, the sound tracks the position of the Transform passed to `PlaySpatial` every frame — e.g. the engine loop of a passing vehicle. If that object is destroyed while the sound is still playing, the sound stops. Leave disabled for sounds at a fixed position (gunshots, explosions, most one-shots) — it saves one position update per frame. |
| **Is One Shot** | If enabled, the sound plays once and automatically releases the AudioSource afterwards. |
| **Can Handle Audio Source** | If enabled, `PlaySpatial()` / `PlayNonSpatial()` return a valid `AudioHandle` that can be used to stop the sound manually. |
| **Use Wall Check** | If enabled, the system checks at regular intervals (for spatial sounds) whether a wall exists between the sound and the player, and muffles the sound accordingly. |
| **Respects Global Pause** | If enabled (default), this sound responds to the global `PauseAll()` / `UnpauseAll()` methods. Disable for sounds that must keep playing while the game is paused — e.g. UI clicks, menu music or global stingers. Only affects the global pause; unrelated to `Stop(handle)`. |

---

## API Reference

> **Namespaces:** The public API class `AudioManagerDynamic` lives in the `AudioFramework.Core` namespace, while the `AudioDataObject` and `SoundRequest` types live in `AudioFramework.Data`. Your code needs `using AudioFramework.Core;` and `using AudioFramework.Data;` accordingly.

### Playing a sound — spatial (3D)

```csharp
AudioHandle handle = AudioManagerDynamic.PlaySpatial(myAudioDataObject, sourceTransform);
```

Plays the sound of the provided `AudioDataObject` as a spatial 3D sound at the position of `sourceTransform`. The sound is attenuated by distance and — if enabled in the ADO — wall-checked. Returns a valid `AudioHandle` if **Can Handle Audio Source** is enabled in the ADO and a pool slot was free, otherwise an invalid handle.

> **Important:** The position of the sound is determined by the provided `Transform`. It must not be `null` for spatial sounds.

```csharp
// Example: play a sound at this GameObject's position
AudioHandle handle = AudioManagerDynamic.PlaySpatial(myAudioDataObject, transform);
```

Alternatively a `SoundRequest` (ADO + Transform bundled together) can be passed — handy for event-driven dispatch, where the request travels through an event as a single payload:

```csharp
AudioHandle handle = AudioManagerDynamic.PlaySpatial(new SoundRequest(myAudioDataObject, transform));
```

---

### Playing a sound — non-spatial (2D)

```csharp
AudioHandle handle = AudioManagerDynamic.PlayNonSpatial(myAudioDataObject);
```

Plays the sound without a position: no distance attenuation, no wall check, same level everywhere. Ideal for UI clicks, music and global stingers. The ADO's `SpatialBlend` value is ignored (always 2D).

---

### Stopping a sound

```csharp
AudioManagerDynamic.Stop(handle);
```

Stops the sound of the provided `AudioHandle`. Only effective if **Can Handle Audio Source** was enabled in the ADO (otherwise the handle is invalid).

---

### Pausing / resuming all sounds

```csharp
AudioManagerDynamic.PauseAll();
AudioManagerDynamic.UnpauseAll();
```

Pauses or resumes all currently playing sounds — e.g. when opening a pause menu. Sounds whose ADO has **Respects Global Pause** disabled keep playing (e.g. UI/music). `PauseAll()` acts as a sustained state: sounds started *while* paused also start paused (if **Respects Global Pause** is enabled) and are resumed together by `UnpauseAll()`.

---

## Example: Explosion

In this example a one-shot explosion sound is played at a specific position. The sound is not controllable — it plays once and automatically releases the AudioSource afterwards.

**1. Configure the ADO**

Create a new `AudioDataObject` and configure it in the Inspector:

| Field | Value |
|---|---|
| **Current Clips** | ExplosionClip |
| **Current Type** | SFX |
| **Spatial Blend** | 1 (full 3D) |
| **Follow Emitter** | false |
| **Is One Shot** | true |
| **Can Handle Audio Source** | false |
| **Use Wall Check** | true |

**2. Play the sound via code**

```csharp
public class ExplosionHandler : MonoBehaviour
{
    [SerializeField] private AudioDataObject explosionADO;

    public void Explode()
    {
        AudioManagerDynamic.PlaySpatial(explosionADO, transform);
    }
}
```

---

## Example: Engine Loop with Stop

In this example an engine sound loops and can be stopped manually — e.g. when the vehicle shuts down. Since **Follow Emitter** is enabled, the sound follows the vehicle while it drives.

**1. Configure the ADO**

| Field | Value |
|---|---|
| **Current Clips** | EngineLoopClip |
| **Current Type** | Ambient |
| **Spatial Blend** | 1 (full 3D) |
| **Follow Emitter** | true |
| **Is One Shot** | false |
| **Can Handle Audio Source** | true |
| **Use Wall Check** | false |

**2. Play and stop the sound via code**

```csharp
public class VehicleEngine : MonoBehaviour
{
    [SerializeField] private AudioDataObject engineADO;
    private AudioHandle engineHandle;

    public void StartEngine()
    {
        engineHandle = AudioManagerDynamic.PlaySpatial(engineADO, transform);
    }

    public void StopEngine()
    {
        AudioManagerDynamic.Stop(engineHandle);
    }
}
```

> **Note:** If the vehicle GameObject is destroyed while the engine loop is still playing, the sound stops automatically (the behaviour of **Follow Emitter**).
