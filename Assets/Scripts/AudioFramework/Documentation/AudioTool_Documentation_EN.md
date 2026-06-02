# AudioTool — Documentation

---

## What is AudioTool?

**AudioTool** is a lightweight audio management framework for Unity that takes care of all `AudioSource` handling for the developer. Instead of manually instantiating, configuring and managing `AudioSource` components, a single call is all it takes — the system handles the rest.

---

## Advantages

- **No manual AudioSource management** — The tool manages a pre-allocated pool of `AudioSource` objects. No new objects are instantiated at runtime, avoiding garbage collection and performance spikes.

- **Automatic wall occlusion (Wall Check)** — Sounds originating behind walls or obstacles are automatically muffled using an `AudioLowPassFilter`. The developer only defines which Unity layers count as walls — the system handles the rest via interval raycasts.

- **Organised volume system** — Sounds are categorised (e.g. Ambient, SFX, Player). Each category has its own `AudioSourceVolume` asset whose value can be overwritten at runtime — ideal for volume sliders in a settings menu.

- **Fire-and-forget or controllable** — The developer decides per sound whether to receive an `AudioHandle` to stop the sound manually later, or whether the sound simply plays through.

- **Simple API** — Playing and stopping sounds is done via three self-explanatory static methods. No event system, no boilerplate code.

- **ScriptableObject-driven** — All configuration is done via assets in the Inspector. No code required to integrate new sounds.

- **UniTask-based** — Internal async logic uses UniTask for minimal overhead. A Coroutine fallback is available.

---

## Setup & Installation

### Prerequisites

- Unity 2022.3 or higher
- **UniTask** — recommended, must be installed separately:
  [https://github.com/Cysharp/UniTask](https://github.com/Cysharp/UniTask)
  *(A Coroutine fallback is available but not recommended.)*

---

### Step 1 — Add AudioManagerDynamic to the scene

Create an empty GameObject in your scene and add the `AudioManagerDynamic` component. This object is the central entry point of the tool and must be present **once per scene**.

---

### Step 2 — Assign the System Config

The included `AudioSystemConfig` asset is already fully pre-configured. Assign it to the **System Config** field on the `AudioManagerDynamic` GameObject in the Inspector.

> All fields of the System Config are covered in detail in a dedicated section later in this documentation.

---

## Setting up the Volume System

### Step 3 — Extend the AudioTypeProvider

The `AudioTypeProvider` is an enum that defines the available volume categories. It can be found at:
> `Assets/Scripts/AudioFramework/Core/AudioTypeProvider.cs`

The existing values (`Ambient`, `Music`, `SFX`, `Player`, `BehindWall`) are **example values only** and should be adapted to your own project. New categories can simply be added at the end of the enum:

```csharp
public enum AudioTypeProvider
{
    Ambient = 1,
    Music,
    SFX,
    Player,
    BehindWall,
    // Add your own categories here:
    Dialogue,
    UI,
    // ...
}
```

> **Note:** If enum values are changed afterwards, the affected `AudioDataObject` assets must be updated accordingly. `AudioSourceVolume` assets are only affected if the order of the enum values is changed.

---

### Step 4 — Create AudioSourceVolume assets

The tool manages volumes through `AudioSourceVolume` assets. Each asset represents a volume category from the `AudioTypeProvider`.

Create a new asset for each desired category via:
> **Right-click in the Project window → Create → Scriptable Objects → AudioSourceVolume**

Assign the following values to each asset in the Inspector:

| Field | Description |
|---|---|
| **Current Audio Type** | The category of this asset — must match the `AudioTypeProvider` value in the corresponding `AudioDataObject`. |
| **Volume** | The default volume value (0.0 – 1.0). This value can be overwritten at runtime, e.g. by a settings slider. |

---

### Step 5 — Populate the AudioVolumesTransferObject

The `AudioVolumesTransferObject` is already included in the provided `AudioSystemConfig` asset and does not need to be created manually.

Click on the `AudioVolumesTransferObject` in the Inspector and press the **Populate Array** button. The system will automatically find all existing `AudioSourceVolume` assets in the project and populate the array.

> **Important:** This step must be repeated every time new `AudioSourceVolume` assets are added.
