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

## 0. Das Bild dazu

[`AudioTool-Sound-Flow.svg`](AudioTool-Sound-Flow.svg) zeichnet den Weg **eines** Sounds von außen nach innen:
vom `Play()`-Aufruf über Slot-Vergabe und Dispatch durch die LateUpdate-Kette bis zum geschriebenen
`source.volume` — und zurück in den Pool. Gedacht als Einstieg **vor** den Listen unten, nicht als deren Ersatz.

> ⚠️ **Momentaufnahme, kein Vertrag.** Das Bild bildet den Stand vom 13.08.2026 (Commit `06057b5`) ab. Die
> Struktur wächst weiter — es muss **nicht zu jeder Zeit dem aktuellen Stand entsprechen**. Bei Abweichung
> gilt: der Code hat recht, dann diese Datei, dann das Bild. Wer das Bild nachzieht, aktualisiert die
> Datumszeile im SVG-Fuß mit.

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
├── AudioDuckService               → liefert duck[kategorie] als ICategoryFactorSource; führt den DuckFactorLedger
│   └── AudioSystemConfig          → ist selbst der Regel-Provider (IDuckRuleProvider); nur bei EnableDucking gebaut
├── CategoryVolumeSource           → liefert basis[kategorie] als ICategoryFactorSource, live aus dem VolumeDictionary
├── CategoryVolumeWriter           → schreibt basis[kategorie] (Settings-Slider); Gegenstück zur Source, gleiches Dictionary
├── AudioVolumeWriteService        → EINZIGER Schreiber von source.volume; kombiniert die Faktoren über IVolumeTarget[]
├── AudioPauseService              → Pause/Unpause der Pool-Slots (global, scope-bewusst)
└── AudioManagerDictionaryProvider → Volume- & LayerMask-Dictionaries
```

### Tick-Reihenfolge (verbindlich)

`AudioManagerDynamic.LateUpdate` treibt fünf Ticks in **dieser** Reihenfolge — die Reihenfolge ist Teil des
Vertrags, nicht Zufall:

```
1. followService.UpdateFollowers()            // Positionen zuerst — nach aller Bewegung des Frames
2. occlusionSmoothingService.Tick(unscaledDeltaTime)
3. fadeService.Tick(unscaledDeltaTime)        // schreibt AudioObject.FadeFactor
4. duckService.Tick(unscaledDeltaTime)        // schreibt den DuckFactorLedger
5. volumeWriteService.Apply()                 // liest alle Faktoren → schreibt source.volume
```

**Warum 5 zuletzt:** Jeder Tick davor löst **einen Faktor** auf und legt ihn an seinem eigenen Ort ab — der
Fade den Per-Slot-Faktor, der Duck den Ledger. Erst der Write liest alle zusammen und macht daraus
`source.volume`. Liefe er früher, hinkte die hörbare Lautstärke den Faktoren um einen Frame hinterher.

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
| `CategoryVolumeSource` | Basis-Gain einer Kategorie aus dem `VolumeDictionary` — **live** gelesen (Slider wirkt sofort), ohne Eintrag `1.0`, bewusst ohne Clamp |
| `CategoryVolumeWriter` | Schreibseite desselben `VolumeDictionary` (Settings-Slider): klemmt auf `[0,1]` **an der API** (Read-Back-Ehrlichkeit), legt eine unbekannte Kategorie an und meldet das als `CategoryVolumeWriteOutcome` |
| `VolumeResolver` | Stufe-1-Gain: `clamp01(basis · fade · duck)` |
| `DuckEnvelope` | Duck-Glide pro Frame (Attack beim Tiefer-Ducken, Release zurück) |
| `DuckTargetPolicy` | aktive Kategorien + Paar-Faktoren → `duck[kategorie]` (`min`-Stacking, kein Selbst-Duck) |
| `DuckRuleFlattening` | nested `DuckRule` → flache `DuckPair`-Liste (Fill-Stil, GC-frei) |
| `DuckConfigValidation` | Widerspruch zwischen Ducking-Master-Schalter und konfigurierten Regeln (beide Richtungen) |
| `DuckFactorLedger` | Duck-Zustand **über die Zeit**: wer wird diesen Frame gestept, Ziel via `DuckTargetPolicy`, Glide via `DuckEnvelope`, Retire bei `1.0` |

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
| `Config/DuckConfigIssue.cs` · `DuckConfigValidation.cs` | Inspector-Guard: Master-Schalter vs. Regeln (Entscheidung pur, Wortlaut im `OnValidate`) |
| `Interfaces/IAudioWallCheckService.cs` | Strategy-Seam für WallCheck (UniTask/Coroutine) |
| `Interfaces/IAudioListenerProvider.cs` | Seam gegen stale Listener-Transform (`TryGetPosition`) |
| `Interfaces/IDuckRuleProvider.cs` | Seam für die Duck-Konfiguration (Regeln + Attack/Release-Rate) |
| `Interfaces/IGetPoolIndex.cs` | Toter Platzhalter — steht im BACKLOG zur Entfernung |
| `Services/IFadeTarget.cs` · `PooledFadeTarget.cs` | Fade-Seam + reale Pool-Implementierung (schreibt den FadeFactor) |
| `Services/IVolumeTarget.cs` · `PooledVolumeTarget.cs` | Volume-Write-Seam + reale Pool-Implementierung (schreibt `source.volume`) |
| `Services/ICategoryFactorSource.cs` | Vertrag einer Faktor-Quelle pro Kategorie (heute Basis + Duck) |
| `Services/DuckPair.cs` | Pure Input-Struct `{ Trigger, Target, DuckedVolume }` für die Duck-Policy |

**Felder von `AudioObject`:** `GameObject`, `Source`, `Filter`, `FollowTarget`, `BusyUntilTime`,
`TargetCutoff`, `FadeFactor`, `Generation`, `Category`, `IsFollowing`, `RespectsGlobalPause`, `IsPaused`.

**Namespaces:** `AudioFramework.Services.Mixing` (Duck-Familie, `VolumeResolver`, `CategoryVolumeSource`),
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

- **Stufe 1 (gebaut, gehört uns):** `source.volume = clamp01(basis · fade · duck)` — die Gleichung liegt in
  `VolumeResolver`, **genau ein Schreiber:** `AudioVolumeWriteService`. Jeder Faktor wird von einer **eigenen**
  Einheit aufgelöst (`CategoryVolumeSource`, `AudioFadeService`, `AudioDuckService`); der Schreiber löst nichts
  auf, er kombiniert nur. Eine fehlende Faktor-Quelle steuert `1.0` bei — deshalb ist jeder Faktor einzeln
  weglassbar, ohne dass die übrigen etwas davon merken.
- **Stufe 2 (später, additiv):** Routing/Effekte/Reverb/Sends/Snapshots — alles *downstream* von
  `source.volume`. Hängt nur an `outputAudioMixerGroup`, **nicht** daran, wie `source.volume` berechnet wird.
  Der leere `CategoryMixerRoute`-Seam hält dafür die Form offen, ohne heute etwas zu implementieren.

**Die drei Faktoren:**

| Faktor | Quelle | Bedeutung |
|---|---|---|
| `basis[kategorie]` | `VolumeDictionary` (aus den Volume-Assets) | **live pro Frame gelesen** → ein Settings-Slider wirkt sofort, ohne eigene API |
| `fade[slot]` | `AudioObject.FadeFactor`, geschrieben vom `AudioFadeService` | 0..1 Per-Slot-Rampe |
| `duck[kategorie]` | im `DuckFactorLedger` geführt, pro Frame vom `AudioDuckService` gefüttert | 0..1 Per-Kategorie-Absenkung |

> ⚠️ **Der wichtigste Fakt dieses Abschnitts:** **Kein** Faktor-Lieferant schreibt `source.volume` — weder
> `AudioFadeService` noch `AudioDuckService`. Beide legen nur ihren Faktor ab. Wer eine neue
> Lautstärke-Beeinflussung baut, macht daraus einen **weiteren Faktor** hinter `ICategoryFactorSource` und
> übergibt ihn dem Writer — er schreibt niemals selbst. Es gibt genau eine Ausnahme: `AudioPlaybackService.Dispatch`
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
- **Der Zustand lebt im `DuckFactorLedger`** (pur, EditMode-getestet), nicht im Service. Er führt Buch, welche
  Kategorie gerade wie stark geduckt ist. Getrackt wird eine Kategorie, solange sie **konfiguriertes Ziel ist
  ODER sich noch erholt** — die zweite Hälfte ist der Grund, warum eine Kategorie zurückglidet, wenn ihre Regel
  zur Laufzeit verschwindet, statt auf ihrem letzten Duck-Wert einzufrieren.
- **Optional ohne Kosten — über einen Master-Schalter, nicht über Anwesenheit:** `EnableDucking` in der
  `AudioSystemConfig` wird **einmal beim Start** gelesen. Ist er aus, wird der `AudioDuckService` gar nicht erst
  gebaut: keine Regel wird gelesen, der Per-Frame-Pool-Scan läuft nicht, und `AudioVolumeWriteService` setzt für
  den Duck-Faktor `1.0` ein. Basis-Lautstärke, Live-Slider und Fade laufen unverändert weiter — genau das ist der
  Ertrag der Faktor-Trennung aus §6. Bewusst **kein Laufzeit-Toggle:** Umschalten im Betrieb bräuchte einen
  Ausblend-Pfad, sonst springt jede geduckte Kategorie in *einem* Frame auf voll zurück (hörbarer Knacks).
- **Ein Gate, nicht zwei:** Ein zusätzliches „leeres Regel-Array ⟹ kein Scan" gibt es bewusst **nicht** — zwei
  Mechanismen für dieselbe Sache erzeugen den klassischen „warum duckt es nicht?"-Supportfall. Stattdessen meldet
  die pure `DuckConfigValidation` beide Widersprüche im Inspector: Schalter an ohne Regeln (Scan läuft umsonst)
  und Regeln ohne Schalter (werden nie gelesen).
- **Die Konfiguration lebt im Asset, nicht auf einem GameObject:** Regeln, Attack/Release und der reservierte
  Mixer-Seam sitzen in der `AudioSystemConfig`, die `IDuckRuleProvider` **selbst** implementiert und dem Service
  einmalig im Konstruktor übergeben wird. Der Grund ist die Persistenz: Der Manager überlebt Szenenwechsel, also
  wäre jede szenen-lokale Konfiguration stillschweigend die der **zuerst geladenen** Szene. Ein Asset kennt dieses
  Problem nicht. Nebeneffekt: kein `OnEnable`/`OnDisable`-Lebenszyklus, keine Enable-Reihenfolge, kein „letzter
  gewinnt" bei zwei Komponenten — und **eine** Komponente auf **einem** GameObject als ganzes Setup.
- **ScriptableObject statt Komponente** — die frühere Begründung („Laufzeit-Tiefen sind mutabler Per-Szenen-Zustand,
  ein SO mutiert beim Editieren das Asset") wurde bewusst umgedreht: Bei einer Komponente gehen im Play Mode
  getunte Duck-Werte beim Stoppen **verloren**, beim Asset bleiben sie — für Balance-Tuning ein Vorteil, nicht ein
  Footgun. Die Kehrseite (wer im Play Mode „nur mal testet", ändert sein Asset dauerhaft) gehört in die User-Doku.
- **`DuckFactorLedger.ReleaseAll` ist derzeit unerreichbar:** Der Pfad stammt aus der Zeit, als die Duck-Config
  eine zur Laufzeit deaktivierbare Komponente war. Mit dem Master-Schalter kann der Provider nicht mehr
  verschwinden. Methode **und ihre zwei EditMode-Tests bleiben bewusst stehen** — sie kosten nichts, und ein
  späterer Laufzeit-Toggle bräuchte sie sofort wieder.
- **GC-frei:** alle Per-Frame-Sammlungen im `AudioDuckService` **und** im `DuckFactorLedger` sind
  wiederverwendete Buffer; der Release-Pfad teilt sich statische leere Arrays (`Array.Empty`).

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

- **Der Manager ist persistent** (`DontDestroyOnLoad`, gesetzt in `MakePersistentAcrossScenes` direkt nach
  `instance = this` — also nur für die Instanz, die tatsächlich übernimmt). Unity wendet das nur auf
  **Root**-GameObjects an; liegt der Manager als Kind, wird das erkannt und mit Handlungsanweisung gemeldet,
  statt still wirkungslos zu bleiben.
- **Ein Duplikat zerstört nur seine eigene Komponente** (`Destroy(this)`), **nie** das GameObject. Der Manager
  kann sich sein Objekt mit Nutzer-Skripten teilen — das ganze Objekt für ein *erwartetes* Duplikat abzuräumen
  wäre Datenverlust. Preis: ein leeres GameObject bleibt stehen. Bewusst so.
- **Ein Manager pro Szene ist der RICHTIGE Einbau** (sonst lässt sich eine Szene nicht isoliert starten). Mit
  der Persistenz heißt das: Ab der zweiten Szene trifft der Duplikat-Pfad bei **jedem** Laden zu. Er ist
  deshalb **still**. Gewarnt wird nur, wenn zwei Manager in *derselben* Szene liegen — dafür merkt sich der
  Manager seine `originScene`, **bevor** er persistent wird (danach meldet `gameObject.scene` nur noch Unitys
  interne „DontDestroyOnLoad"-Szene, ein Vergleich wäre also immer „verschiedene Szene").
- Ein Duplikat mit **abweichender** `AudioSystemConfig` bekommt eine Warnung: seine Config wird ignoriert, weil
  der zuerst geladene Manager samt seiner Config weiterläuft.
- `OnDestroy` räumt **nur**, wenn es die echte Instanz ist (`if (instance != this) return;`) — sonst würde ein
  am Frame-Ende zerstörtes Duplikat (z. B. additives Szenenladen) die statische Referenz auf die *lebende*
  Instanz nullen.
- **Vorbedingung ist allein die Config:** Sie wird **vor** `instance = this` geprüft → Invariante:
  `instance != null` ⟺ voll initialisiert. Ein fehlender **AudioListener** ist bewusst **keine** Vorbedingung
  mehr (nur eine Warnung): Das saubere Muster für Persistenz ist eine schlanke Bootstrap-Szene, die ihre Level
  — und damit die Kamera — erst danach lädt. `SceneAudioListenerProvider` kommt mit `null` als Startwert klar
  und löst beim ersten Zugriff selbst auf.

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
