# AudioTool — Architektur & Designentscheidungen

> **Was diese Datei ist:** die **Single Source** für den IST-Aufbau des Systems und für das **Warum** hinter
> jeder Designentscheidung. Sie beantwortet „wie ist es gebaut und warum so?" — nicht „wie arbeiten wir
> zusammen?" (→ [`CLAUDE.md`](CLAUDE.md)) und nicht „was ist noch zu tun?" (→ [`BACKLOG.md`](BACKLOG.md)).
>
> **Wann lesen:** bevor du ein Subsystem anfasst. `CLAUDE.md` trägt nur den Überblick und die Regeln, die
> bei *jeder* Aufgabe gelten; die Tiefe zu einem Subsystem steht hier. Der Wegweiser in `CLAUDE.md` führt
> pro Thema hierher.
>
> **Pflege-Regel:** Jede durable Architektur-Erkenntnis landet **hier**, nicht in `CLAUDE.md`. Ändert sich
> ein Fakt im Code, wird er hier nachgezogen — es gibt bewusst **keine zweite Kopie** dieser Listen in
> anderen Dateien (die Pure-Klassen-Liste stand einmal in drei Dateien und war in allen dreien
> unterschiedlich veraltet; genau das verhindert die Single-Source-Regel).

---

## 1. Service-Graph

```
AudioManagerDynamic (MonoBehaviour — Singleton, öffentliche API, treibt die LateUpdate-Ticks)
├── AudioPoolAcquisitionService    → Pool aus AudioObject[] (AudioSource + LowPassFilter); Slot-Vergabe + Generation
├── AudioPlaybackService           → Dispatching (Play / FadeIn-Silent), Stop-Einstieg, Handle-Gating
│   └── AudioStopService           → einziger „Slot stoppen"-Pfad (Source.Stop + Reset + WallCheck stop), fade-frei
├── AudioUniTaskWallCheckService   → Raycast-Loop per UniTask (empfohlen)   ┐ setzen nur TargetCutoff
├── AudioCoroutineWallCheckService → Raycast-Loop per Coroutine (Fallback)  ┘ (geteilte WallOcclusionMath + WallLayerMask)
│   └── SceneAudioListenerProvider → liefert die *aktuelle* AudioListener-Position (lazy + self-heal, kein Polling)
├── AudioOcclusionSmoothingService → gleitet Filter.cutoffFrequency pro Frame Richtung TargetCutoff
├── AudioFollowService             → kopiert Emitter-Position pro Frame, ohne Parenting
├── AudioFadeService               → treibt alle Fades pro Frame über IFadeTarget[]; schreibt den Per-Slot-FadeFactor
├── AudioDuckService               → EINZIGER Besitzer von source.volume: basis · fade · duck, live pro Frame
│   └── AudioDuckComponent         → optionaler, passiver Regel-Provider (IDuckRuleProvider) — kein eigener Tick
├── AudioPauseService              → Pause/Unpause der Pool-Slots (global, scope-bewusst)
└── AudioManagerDictionaryProvider → Volume- & LayerMask-Dictionaries
```

### Tick-Reihenfolge (verbindlich)

`AudioManagerDynamic.LateUpdate` treibt vier Ticks in **dieser** Reihenfolge — die Reihenfolge ist Teil des
Vertrags, nicht Zufall:

```
1. followService.UpdateFollowers()            // Positionen zuerst — nach aller Bewegung des Frames
2. occlusionSmoothingService.Tick(unscaledDeltaTime)
3. fadeService.Tick(unscaledDeltaTime)        // schreibt AudioObject.FadeFactor
4. duckService.Tick(unscaledDeltaTime)        // liest FadeFactor → schreibt source.volume
```

**Warum 4 nach 3:** Der Fade schreibt nur noch seinen Per-Slot-**Faktor**; der Duck-Service ist der einzige,
der daraus (zusammen mit Basis-Lautstärke und Duck) `source.volume` auflöst. Liefe der Duck-Tick vor dem
Fade-Tick, hinkte die hörbare Lautstärke dem Fade um einen Frame hinterher.

---

## 2. Die pure, Unity-freie Logik-Schicht (Single Source dieser Liste)

Alle EditMode-getestet, ohne laufende Engine. Das ist die Schicht, die die Testdisziplin überhaupt möglich
macht (→ [`TESTING.md`](TESTING.md)).

| Klasse | Verantwortung |
|---|---|
| `AudioFadeMath` | Fade-Kurve: Volumen über Zeit (lineare Interpolation + Clamps) |
| `FadeOperation` | immutables Per-Slot-Fade-Fortschrittsobjekt (Elapsed → Volumen via `AudioFadeMath`) |
| `WallOcclusionMath` | Pro-Wand-Dämpfungsschritt (Faktor → Cutoff) + Floor-Clamp — **der Occlusion-Modell-Seam** |
| `OcclusionSmoothing` | Per-Frame-Glide Richtung Ziel-Cutoff (MoveTowards) |
| `WallLayerMask` | Layer-Indizes → eine Bitmask (von beiden WallCheck-Backends geteilt) |
| `WallCheckContinuation` | „soll die WallCheck-Schleife weiterlaufen?" inkl. Generation-Guard |
| `ListenerCachePolicy` | „Listener neu auflösen?" (kein Cache ODER gecachter nicht mehr lebend & aktiv) |
| `LowPassDispatchPolicy` | Filter-Zustand pro Dispatch (an ⟺ UseWallCheck) |
| `PoolSlotAvailability` | „Slot frei?" (still + Busy-Fenster abgelaufen + nicht pausiert) |
| `AudioHandleValidator` | Handle-Currency: Bounds + Generation |
| `VolumeResolver` | Stufe-1-Gain: `clamp01(basis · fade · duck)` |
| `DuckEnvelope` | Duck-Glide pro Frame (Attack beim Tiefer-Ducken, Release zurück) |
| `DuckTargetPolicy` | aktive Kategorien + Paar-Faktoren → `duck[kategorie]` (`min`-Stacking, kein Selbst-Duck) |
| `DuckRuleFlattening` | nested `DuckRule` → flache `DuckPair`-Liste (Fill-Stil, GC-frei) |

---

## 3. Wichtige Typen & Dateien

| Datei | Zweck |
|---|---|
| `Core/AudioManagerDynamic.cs` | Singleton, öffentliche API, LateUpdate-Treiber |
| `Core/AudioObject.cs` | Struct — ein Pool-Slot (s. Felder unten) |
| `Core/AudioHandle.cs` | Readonly struct `{ PoolIndex, Generation }`; Ctor `internal` |
| `Core/AudioCategory.cs` | Enum — Lautstärke-Kategorien (Beispielwerte, vom Nutzer anzupassen) |
| `Core/WallDampingLayer.cs` | Serialisiert: Layer + `WallDampingFactor` (`[Range(0,1)]`) |
| `Data/AudioDataObject.cs` | ScriptableObject — Konfiguration pro Sound (ADO, „Control Surface") |
| `Data/AudioSourceVolumes.cs` | ScriptableObject — Lautstärke pro Kategorie |
| `Data/AudioVolumesTransferObject.cs` | Bündelt alle AudioSourceVolume-Assets (nur eine Instanz) |
| `Data/SoundRequest.cs` | Readonly struct `{ Ado, Source }` — Payload für `PlaySpatial(SoundRequest)` |
| `Config/AudioSystemConfig.cs` | ScriptableObject — zentrale System-Konfiguration |
| `Config/DuckRule.cs` · `DuckTarget.cs` | Serialisierte Duck-Regeln: Trigger-Kategorie → Ziele `{ Kategorie, Faktor }` |
| `Config/CategoryMixerRoute.cs` | **Reservierter Stufe-2-Seam** (Kategorie → AudioMixerGroup) — deklariert, noch nicht gelesen |
| `Components/AudioDuckComponent.cs` | Optionale, passive MonoBehaviour: Duck-Matrix + globale Attack/Release |
| `Interfaces/IAudioWallCheckService.cs` | Strategy-Seam für WallCheck (UniTask/Coroutine) |
| `Interfaces/IAudioListenerProvider.cs` | Seam gegen stale Listener-Transform (`TryGetPosition`) |
| `Interfaces/IDuckRuleProvider.cs` | Seam für die Duck-Konfiguration (Regeln + Attack/Release-Rate) |
| `Interfaces/IGetPoolIndex.cs` | Toter Platzhalter — steht im BACKLOG zur Entfernung |
| `Services/IFadeTarget.cs` · `PooledFadeTarget.cs` | Fade-Seam + reale Pool-Implementierung (schreibt den FadeFactor) |
| `Services/DuckPair.cs` | Pure Input-Struct `{ Trigger, Target, DuckedVolume }` für die Duck-Policy |

**Felder von `AudioObject`:** `GameObject`, `Source`, `Filter`, `FollowTarget`, `BusyUntilTime`,
`TargetCutoff`, `FadeFactor`, `Generation`, `Category`, `IsFollowing`, `RespectsGlobalPause`, `IsPaused`.

**Namespaces:** `AudioFramework.Services.Mixing` (Duck-Familie + `VolumeResolver`),
`AudioFramework.Configuration` (Config-/Regel-Typen), `AudioFramework.Interfaces`,
`AudioFramework.Pooling`, `AudioFramework.Services.{Playback,Fading,Following,WallCheck}`,
`AudioFramework.Pause`, `AudioFramework.Core`, `AudioFramework.Data`, `AudioFramework.Utilities`.

---

## 4. ADO ist die „Control Surface" (zentrales Invariant)

Das `AudioDataObject` ist bewusst ein serialisierter **Spiegel der AudioSource-Einstellungen**, der zur
Play-Zeit auf den gepoolten `AudioSource` geschrieben wird.

> **Jede gespiegelte Eigenschaft MUSS bei JEDEM Dispatch geschrieben werden — unbedingt, nie in einem `if`.**

Sonst trägt ein wiederverwendeter Slot den **vorherigen** Sound-Wert → stille, schwer findbare Bugs (genau so
passiert mit `spatialBlend`). Beim Hinzufügen eines neuen gespiegelten Feldes immer die unbedingte
`source.<prop> = ado.<prop>`-Zeile in `AudioPlaybackService.Dispatch` mitsetzen.

**Wachstumskandidaten:** `pitch`, `loop`, `priority`, `minDistance`/`maxDistance`, `rolloffMode`, `spread`
(+ `dopplerLevel`, `panStereo`, `reverbZoneMix` — Begründungen und die bewussten Nicht-Kandidaten stehen im
BACKLOG unter „ADO Control Surface härten + erweitern").

**Sauber getrennt:** *reine Spiegel* (unbedingtes Durchschreiben) vs. *berechnete* Felder, die
Entscheidungslogik sind — `spatialBlend` ⟵ isSpatial, `filter.enabled`/Cutoff ⟵ UseWallCheck via
`LowPassDispatchPolicy`, Volumen ⟵ startSilent + Kategorie.

---

## 5. Pool & Generation

- Festes `AudioObject[]`-Array, vorab instanziiert — **kein GC zur Laufzeit**.
- `BusyUntilTime` als Zeitstempel-Trick für OneShot-Slots (ein `PlayOneShot`-Slot meldet nicht zuverlässig
  „spielt noch", also wird er für die Clip-Länge reserviert).
- Pool-Suche O(n) ab Index 0 — **bewusst einfach**. Die Suche packt den Pool automatisch nach unten (es wird
  immer der erste freie Slot genommen), der Scan bricht also nach ≈ Anzahl klingender Sounds ab. Der
  Schleifenrumpf wird ohnehin von *einem* nativen `Source.isPlaying`-Call dominiert, nicht vom Array-Zugriff
  — eine „schnellere" Datenstruktur (Queue o. ä.) würde daran nichts ändern und die Selbst-Freigabe von
  OneShots nicht mitbekommen. Nicht optimieren.
- **Generation pro Slot:** Jede (Neu-)Vergabe in `GetFreeAudioSourcePoolIndex` erhöht `AudioObject.Generation`.
  Der zurückgegebene `AudioHandle` trägt diese Generation. Stop/Fade prüfen via
  `AudioHandleValidator`/`IsHandleCurrent`, ob Generation **und** Bounds passen — sonst stilles No-op. So kann
  ein alter Handle nach Slot-Reuse nicht den neuen, fremden Sound stoppen. Der `AudioHandle`-Ctor ist
  `internal` (Handles sind reine Ausgabewerte; verhindert selbstgebaute Crash-Handles).

---

## 6. Lautstärke: das Zwei-Gain-Stufen-Modell

Unitys Signalweg ist `AudioSource(source.volume) → outputAudioMixerGroup → Effekte/Sends → Master → Output`.
Das Tool besetzt bewusst **nur die vordere Stufe**:

- **Stufe 1 (gebaut, gehört uns):** `source.volume = clamp01(basis · fade · duck)` — aufgelöst von
  `VolumeResolver`. **Genau ein Besitzer:** `AudioDuckService`.
- **Stufe 2 (später, additiv):** Routing/Effekte/Reverb/Sends/Snapshots — alles *downstream* von
  `source.volume`. Hängt nur an `outputAudioMixerGroup`, **nicht** daran, wie `source.volume` berechnet wird.
  Der leere `CategoryMixerRoute`-Seam hält dafür die Form offen, ohne heute etwas zu implementieren.

**Die drei Faktoren:**

| Faktor | Quelle | Bedeutung |
|---|---|---|
| `basis[kategorie]` | `VolumeDictionary` (aus den Volume-Assets) | **live pro Frame gelesen** → ein Settings-Slider wirkt sofort, ohne eigene API |
| `fade[slot]` | `AudioObject.FadeFactor`, geschrieben vom `AudioFadeService` | 0..1 Per-Slot-Rampe |
| `duck[kategorie]` | im `AudioDuckService` abgeleitet + geglättet | 0..1 Per-Kategorie-Absenkung |

> ⚠️ **Der wichtigste Fakt dieses Abschnitts:** `AudioFadeService` schreibt **nicht** mehr `source.volume`,
> sondern nur den Faktor. Wer eine neue Lautstärke-Beeinflussung baut, macht daraus einen **weiteren Faktor**
> — er schreibt niemals selbst auf `source.volume`. Es gibt genau eine Ausnahme: `AudioPlaybackService.Dispatch`
> setzt beim Dispatch einmalig `VolumeResolver.Resolve(basis, fadeFactor, 1f)`, damit schon der allererste
> Frame stimmt, bevor der Duck-Tick das erste Mal läuft.

---

## 7. Ducking

- **Ausschließlich pro Kategorie**, beliebig viele Nutzer-Kategorien.
- **Tiefe pro (Trigger→Ziel)-Paar** („Duck-Matrix"), gespeichert als **sparse** Liste — nur konfigurierte
  Paare. `DuckRuleFlattening` faltet die nested Inspector-Struktur zu flachen `DuckPair`s.
- **Stärkster Duck gewinnt (`min`)** über alle aktiven Trigger einer Zielkategorie — nicht multiplizieren.
- **Keine Kaskade:** Eine Kategorie, die selbst gerade geduckt wird, triggert ihre eigenen Regeln mit
  **voller** Stärke. Hält den Resolver einstufig, reihenfolge-unabhängig und oszillationsfrei.
- **Eine Kategorie duckt sich nie selbst.**
- **Attack/Release global** (ein Wert fürs ganze System), bewusst nicht pro Regel — schützt „easy to learn".
  Exponiert als **Rate** (Faktor-Einheiten pro Sekunde), 1:1 auf `DuckEnvelope`, ohne Umrechnungslogik.
  *(Ob das für Designer die richtige Einheit ist — Rate vs. Zeit in ms — steht als offene UX-Frage im BACKLOG.)*
- **„Aktiv" wird abgeleitet, nicht gezählt:** „Kategorie aktiv" = hat einen spielenden, nicht-pausierten Slot;
  **pro Frame aus dem Pool ermittelt**, kein +1/−1-Mitzählen. Ein OneShot endet von selbst — Ableiten kann
  nicht driften, Buchführung schon. (Dieselbe Philosophie wie der self-healing Listener.)
- **Optional ohne Kosten:** Ohne registrierten `IDuckRuleProvider` wird der komplette Duck-Scan übersprungen
  (jedes `duck` bleibt 1). Die Volumes werden trotzdem aufgelöst — der Live-Slider funktioniert also auch
  ganz ohne Duck-Komponente.
- **Die Komponente ist passiv:** `AudioDuckComponent` hat **keinen eigenen `LateUpdate`** und schreibt nie
  `source.volume`. Sie ist reiner Konfigurations-Provider hinter `IDuckRuleProvider` — dasselbe Seam-Muster
  wie `IAudioWallCheckService`/`IAudioListenerProvider`. Registrierung in `OnEnable`, Abmeldung in
  `OnDisable`, **kein** Manager-Zugriff im `Awake` (Enable-Reihenfolge darf egal sein).
  `[RequireComponent(typeof(AudioManagerDynamic))]` erzwingt das gemeinsame GameObject.
- **Komponente statt ScriptableObject**, weil die Laufzeit-Tiefen mutabler Per-Szenen-Zustand sind — ein SO
  würde beim Editieren zur Laufzeit das Asset mutieren (Footgun).
- **GC-frei:** alle Per-Frame-Sammlungen im `AudioDuckService` sind wiederverwendete Buffer.

---

## 8. Fade-Familie

- Framework-agnostischer `AudioFadeService`, pro Frame aus `LateUpdate` getrieben. Index-basiert über
  `IFadeTarget[]` (gleiche Größe/Index wie der Pool → der Pool-Index ist der geteilte Schlüssel).
- Reales Target = `PooledFadeTarget`: `Volume` mappt auf **`AudioObject.FadeFactor`** (nicht auf
  `source.volume` — siehe Abschnitt 6), `Stop` routet über den geteilten `AudioStopService.StopSlot`-Pfad.
- **Pause-bewusst:** ein pausierter Fade friert ein und läuft nach dem Unpause weiter, wo er war.
- Fade ist ein **Laufzeit-Override**: FadeIn settled auf Faktor 1 (die hörbare Ziel-Lautstärke fällt aus der
  live gelesenen Kategorie-Lautstärke), FadeOut erreicht 0 und gibt den Slot frei.
- **Reset-Punkte** (jeder Dispatch, Stop, Follow-Target-Tod) räumen den Fade, damit er keinen
  wiederverwendeten Slot überschreibt.
- `Crossfade` ist **Komposition** aus `FadeOut(from)` + `FadeIn(to)` — kein Spezial-Pfad.

---

## 9. Wall Check (lightweight occlusion)

- `Physics.RaycastNonAlloc` mit `RaycastHit[8]`-Buffer (max. 8 Wände).
- Layer-basierte **Dämpfung**: jeder getroffene Layer dämpft den laufenden Cutoff um einen konfigurierbaren
  **Faktor `0..1`** (`WallDampingLayer.WallDampingFactor`) Richtung Floor — `WallOcclusionMath.ApplyWall`,
  **multiplikativ**: `current − (current − floor) · d`. Über N Wände skaliert der offene Bereich über dem
  Floor mit `∏(1 − dᵢ)` → **reihenfolge-unabhängig** und **asymptotisch** zum Floor. `ClampToFloor` ist nur
  noch Sicherheitsnetz gegen Fehlkonfig (`d > 1`). `0` = Wand transparent, `1` = fällt in einer Wand auf
  `MinCutoffFreqValue`.
- **Offener Cutoff = `DefaultCutoffFreqValue` ≈ 22000 Hz** (Obergrenze des Gehörs → transparent).
- **Filter nur für wand-geprüfte Sounds aktiv:** `filter.enabled = ado.UseWallCheck` bei jedem Dispatch
  (`LowPassDispatchPolicy`). Alle anderen Sounds umgehen den Filter komplett → transparenter Klang + weniger DSP.
- **Weiche Übergänge:** Der WallCheck-Loop setzt nur `AudioObject.TargetCutoff`;
  `AudioOcclusionSmoothingService` gleitet `filter.cutoffFrequency` pro Frame dorthin
  (`OcclusionSmoothing.Step`, MoveTowards mit `OcclusionSmoothingSpeed` Hz/s; `0` = sofort). Kein „Pop".
- **Loop-Lebenszeit:** `WallCheckContinuation` unterscheidet OneShot (BusyUntilTime) und Loop (isPlaying),
  hält den Loop bei `IsPaused` am Leben (sonst kehrt Occlusion nach Unpause nicht zurück) und bricht bei
  fremder `Generation` ab (Slot-Reuse). Kein Raycast bei nicht wand-geprüften Sounds.
- **Listener-Bezug self-healing statt gecacht:** Der WallCheck hält einen `IAudioListenerProvider`, nicht die
  rohe `Transform`. `SceneAudioListenerProvider` cached den `AudioListener`, validiert ihn aber bei **jedem**
  Zugriff (`!= null && isActiveAndEnabled`) und löst nur im Ungültig-Fall neu auf. Kein Intervall-Polling —
  der teure Scan feuert nur im Wechsel-Moment. Fängt **Respawn** (zerstört) **und Kamerawechsel per
  Disable/Enable** ab. `TryGetPosition(out)` → bei `false` bleibt es beim `DefaultCutoffFreqValue`.
  Die Resolve-*Entscheidung* lebt pure in `ListenerCachePolicy`.
- **`WallOcclusionMath` ist der Modell-Seam:** Das multiplikative Dämpfungs-Modell lebt allein im
  `ApplyWall`-Rumpf — ein künftiger Wechsel (z. B. logarithmisches Mapping) bliebe eine lokale Änderung.

---

## 10. Pause-Modell (ohne Multi-Pool gelöst)

- Pro-ADO `RespectsGlobalPause` (Default true; regelt **nur** die globale `PauseAll`/`UnpauseAll`, nicht
  `Stop(handle)`).
- Laufzeit-Flag `AudioObject.IsPaused` trackt, was *wir* pausiert haben:
  (a) `GetFreeAudioSourcePoolIndex` behandelt pausierte Slots als **belegt** (ein pausierter AudioSource
  meldet `isPlaying == false` → würde sonst überschrieben),
  (b) `UnpauseAll` weckt nur, was es selbst pausiert hat,
  (c) `StopAudio` + Follow-Cleanup räumen `IsPaused`.
- Pro Dispatch via `SetPausePolicy` gespiegelt (Control-Surface). Wird ein Sound gestartet, während global
  pausiert ist, wird er sofort mitpausiert (`HonorActiveGlobalPause`).

---

## 11. Zeit-Modell: alles läuft auf der echten Uhr

> **Contract (auch user-facing dokumentiert):** Pausieren heißt `PauseAll()`/`UnpauseAll()` — **nicht**
> `Time.timeScale = 0`.

Audio läuft in realen Sekunden und ignoriert `timeScale` (so verhält sich auch Unitys `AudioSource` selbst).
Damit das gesamte Tool konsistent dazu bleibt, hängt **alles** an der ungeskalierten Uhr:

- Alle vier `LateUpdate`-Ticks bekommen `Time.unscaledDeltaTime` (Fade, Occlusion-Glide, Follow, Duck).
- Das OneShot-Busy-Fenster nutzt `Time.unscaledTime` (`SetSlotBusy` **und** die Vergleichsstelle in
  `GetFreeAudioSourcePoolIndex` — eine Uhr, konsistent).
- Der WallCheck-Takt nutzt `DelayType.UnscaledDeltaTime` (UniTask) bzw. `WaitForSecondsRealtime` (Coroutine).

Folge: Bei `timeScale = 0` (Slow-Mo/Bullet-Time) laufen Fades und Occlusion weiter und OneShot-Slots werden
korrekt freigegeben — ein Hänger wäre sonst genau der Bug, den das Modell verhindert. Die konsumierende
Mathematik ist selbstklemmend (kein Overshoot/NaN bei Frame-Spikes). Die pure Schicht
(`PoolSlotAvailability`, `WallCheckContinuation`) nimmt Zeit als **rohe Zahl** entgegen und weiß von der
Uhr nichts — deshalb ist sie EditMode-testbar.

---

## 12. Follow ohne Parenting

Spatiale Sounds folgen einem Emitter, indem die Position pro `LateUpdate` **kopiert** wird — **nie** per
`SetParent`. Parenting würde den Pool-Slot dem Aufrufer „schenken": Zerstört der seinen Emitter, stürbe der
gepoolte Slot mit. Stirbt das Follow-Target mitten im Sound, wird gestoppt und der Slot freigegeben (ein
Follow-Sound ist meist ein Loop → liefe sonst ewig am Todesort weiter).

---

## 13. Token-Management

`CancellationTokenSource[]` liegt **ausschließlich** im jeweiligen WallCheck-Service. `AudioManagerDynamic`
kennt keine Tokens — vollständige Interface-Abstraktion hinter `IAudioWallCheckService`.

---

## 14. Singleton & Lebensdauer

- Mehrere Instanzen werden in `Awake` erkannt und zerstört (mit Warning).
- `OnDestroy` räumt **nur**, wenn es die echte Instanz ist (`if (instance != this) return;`) — sonst würde ein
  am Frame-Ende zerstörtes Duplikat (z. B. additives Szenenladen) die statische Referenz auf die *lebende*
  Instanz nullen.
- Vorbedingungen (Config, AudioListener) werden **vor** `instance = this` geprüft → Invariante:
  `instance != null` ⟺ voll initialisiert.

> ⚠️ **Offener Punkt — Szenen-Persistenz:** Der Manager ruft aktuell **kein** `DontDestroyOnLoad` (im
> gesamten Projekt nicht vorhanden). Die ausgelieferte User-Doku beschreibt ihn aber als
> „`DontDestroyOnLoad`-Singleton, der Szenenwechsel überlebt". **Code und Doku widersprechen sich hier.**
> Die Entscheidung (Persistenz nachrüsten vs. Doku korrigieren) steht offen — siehe BACKLOG.
> Der Rest von Abschnitt 15 (Call-statt-Event) bleibt davon inhaltlich unberührt: die Argumentation gilt
> für einen langlebigen Owner und wird durch echte Persistenz nur *stärker*, nicht hinfällig.

---

## 15. Direkter Call statt Event-getriebener API

Die öffentliche API ist bewusst **synchroner Call mit Rückgabewert**, kein Event-/Pub-Sub-Modell. Das Tool
definiert **kein einziges eigenes Event** (kein `EventBus`, `UnityEvent`, `Action`). Das einzige
event-freundliche Element ist die `SoundRequest`-Struct — eine reine **Payload**, die durch das Event des
*Aufrufers* reist und dann an den synchronen `PlaySpatial(SoundRequest)` übergeben wird. Event-Nutzung ist
damit ein **Adapter am Rand**, nicht das Fundament.

**Kern-Grund — der Handle braucht einen Rückgabewert.** `PlaySpatial` liefert einen `AudioHandle`, an dem der
ganze Lifecycle hängt (`Stop`, `FadeOut`, `Crossfade`). Events sind fire-and-forget → geben nichts zurück.
Eine event-getriebene API hätte die Handle-Kontrolle **strukturell verbaut**. Weitere Gründe: Traceability
(Stacktrace + „Find Usages" statt unsichtbarer „wer feuert / wer lauscht"-Kopplung), Testbarkeit (aufrufen →
Handle asserten, deterministisch in EditMode), Zielgruppe („ein Aufruf, fertig") und vorhersehbares Timing.

**Die Asymmetrie, die alles trägt (Szene → Manager, nie Manager → Szene).** Ein *Call* ist eine momentane
Interaktion und hinterlässt nichts; eine *Subscription* ist eine dauerhafte Bindung, die aktiv abgebaut werden
muss. Szenen-Objekte **rufen** den Manager an (transient); der Manager **subscribed nirgends** in die Szene
zurück. Damit existiert **keine Bindung, die beim Scene-Unload zur Leiche wird** — die
Event-Ownership-Probleme entstehen gar nicht erst, statt clever gelöst zu werden.

**Langlebiger Owner + Events wäre die giftigste Kombination.** Ein Manager, der Szenenwechsel überdauert,
plus Events = ein unsterblicher Subscriber, der über jeden Scene-Load tote Szenen-Subscriber ansammelt
(Doppel-Feuer nach additivem Laden, `MissingReferenceException`, Leaks über die persistente Delegate-Kette).
Langlebiger Owner **plus** Call/Polling ist dagegen die sicherste Kombination: keine Bindungen in die
kurzlebige Szene.

**Intern tick- statt event-getrieben.** Occlusion, Follow, Fade, Ducking pollen alle pro `LateUpdate`. Im
Frame-Loop ist ein deterministischer Tick billiger und ordnungssicher als event-getriebene Invalidierung
(kein Event-Storm, GC-frei, garantierter Zeitpunkt nach aller Bewegung).

**Die zwei unvermeidbaren Szenen-Referenzen — Event-Ownership ohne Events gelöst.** Es gibt genau zwei
Stellen, wo die langlebige Seite doch eine kurzlebige Szenen-Referenz braucht, und beide nutzen dasselbe
Muster (*keine dauerhafte Bindung; flüchtige Referenz pro Tick neu auflösen*): (1) der **AudioListener** über
`IAudioListenerProvider` (Self-Heal, Null-Check pro Zugriff statt Subscription), (2) das **Follow-Target**
über einen Per-Frame-Null-Check (kein `OnDestroy`-Subscribe).

**Native C#-Services tragen über Szenen, weil ihr Zustand pool-index-gekeyt ist.** Die Services sind plain C#,
keine MonoBehaviours, und leben exakt so lange wie der Manager. Generation-Counter,
`CancellationTokenSource[]`, Fade-State, Duck-State — alles über den **Pool-Index** adressiert, nicht über
Szenen-Objekte. Es gibt nichts zu invalidieren und nichts abzubauen.

**Bewusster Trade-off (Kehrseite derselben Entscheidung).** Der Manager **weiß nicht, dass eine Szene entladen
wurde** — er fängt kein Scene-Lifecycle-Event ab. Für szenen-gebundene SFX-Loops ist es damit eine
**Bringschuld des Aufrufers**: Handle halten und selbst `Stop`/`FadeOut` rufen. Dieser Caveat ist als
user-facing Einzeiler in der Sektion „Bekannte Einschränkungen" der User-Doku festgehalten — dort ist seine
Single Source.

---

## 16. UniTask-Versionspolitik

Floor `2.3.0` in `AudioFramework.asmdef` (`versionDefines.expression = "2.3.0"` — Unity verlangt die bloße
Version, kein `[2.3.0,)`). Der Gate ist ein **Sicherheitsschalter, kein Min-to-work**: unterhalb fällt
`USE_UNITASK` weg und der Code nutzt den voll funktionsfähigen `AudioCoroutineWallCheckService`. Das Risiko
ist asymmetrisch → konservativ/höher ist sicher. Der aktive Modus ist am Console-Log erkennbar
(„[AudioTool] UniTask mode was initialized" bzw. „Internal Coroutine mode was initialized").
