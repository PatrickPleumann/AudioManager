# AudioTool — Backlog

> **Single Source aller offenen Arbeiten.** Zwei Teile: **Teil A** = Release-Härtung aus dem Code-Review (IST-Zustand, 2026-06-04). **Teil B** = geplante Features & Roadmap (aus CLAUDE.md zusammengeführt). Wissen, Architektur und Designentscheidungen leben in [`CLAUDE.md`](CLAUDE.md).
> **Reihenfolge = Priorität** (innerhalb der Abschnitte). Zeilennummern driften; im Zweifel über Symbol/Methodenname suchen. Checkboxen beim Abarbeiten pflegen.
>
> **TDD-Regel (nicht verhandelbar):** Jedes neue Feature/jeder Fix wird test-first gebaut — erst rot sehen, dann grün, dann Mutation Check. Tests sind nach dem Schreiben eingefroren (Details in CLAUDE.md). Tests sind Teil jedes Features, kein separater Punkt.
>
> **Aufwand-Legende:** S = klein (Einzeiler bis ~1h) · M = mittel (Feature/Refactor + Tests) · L = groß/laufend.

---

# Teil A — Release-Härtung (Code-Review IST)

## ✅ Erledigt

> Die ersten fünf Punkte (K1, W2, Occlusion-Glättung, W1+P6, P8): 2026-06-04 (Code-Review-Härtung). **W3**: 2026-06-20.

- **K1** — Singleton-`OnDestroy` nullt nicht mehr die lebende Instanz (`if (instance != this) return;`). *(Unit-Test folgt mit dem AudioManagerDynamic-Sammeltest + M3.)*
- **W2** — LowPass-Default 5000→22000 (transparent); `filter.enabled = UseWallCheck` pro Dispatch (`LowPassDispatchPolicy`, 4 Tests). Occlusion-Mathe in pure `WallOcclusionMath` extrahiert (4 Tests), beide WallCheck-Services teilen den Seam. Layer-Reduktionen neu getunt (10000/14000/18000).
- **Occlusion-Glättung (aus W2-Diskussion abgespalten)** — WallCheck setzt nur noch `TargetCutoff`, neuer `AudioOcclusionSmoothingService` gleitet pro Frame (`OcclusionSmoothing`, 6 Tests); `OcclusionSmoothingSpeed` als Config-Feld. Kein „Pop" mehr beim Aus-der-Wand-Treten.
- **W1 + P6** — `AudioHandle` trägt jetzt `Generation`; Stop/Fade prüfen `IsHandleCurrent` (Bounds + Generation, `AudioHandleValidator`, 6 Tests) → stale Handle = stilles No-op. `AudioHandle`-Ctor `internal` + `AudioHandle.Invalid` → P6 (selbstgebauter `{99999}`-Crash) strukturell zu.
- **P8** — CLAUDE.md auf IST-Zustand gebracht (API `PlaySpatial`/`PlayNonSpatial`/Fade-Familie, `AudioCategory`, kein `CallerTransform` mehr, neue Services) + Memory-Wissen eingearbeitet.
- **M1** — Fade- und Occlusion-Ticks laufen auf `Time.unscaledDeltaTime` statt `Time.deltaTime` (`AudioManagerDynamic.LateUpdate`, 2026-06-20). Modell: **Tool-Pause = `PauseAll`, nicht `timeScale`** — `AudioSource`-Wiedergabe ignoriert `timeScale`, also tun es die Echtzeit-Animationen (Fade/Occlusion-Glide) jetzt auch. `timeScale = 0` friert sie nicht mehr ein (das erledigt der `IsPaused`-Gate via `PauseAll`); in Slow-Mo/Fast-Forward laufen sie in realen Sekunden. Konsumierende Mathe ist selbstklemmend (`AudioFadeMath.Evaluate` clampt `[from,to]`, `OcclusionSmoothing.Step` MoveTowards) → Hitch-Spikes geben max. einen Snap-Frame, kein Overshoot/NaN. Kein EditMode-Seam (reiner Unity-Statik-Swap in dünner MonoBehaviour-Glue, Gruppe C) → **verifiziert via PlayMode-Smoke (Bündel unten)**, nicht per erfundenem Unit-Test. **Doku-TODO:** im User-Doc festhalten, dass Fades/Occlusion an `PauseAll` hängen, nicht an `timeScale`.
- **W3** — Listener wird nicht mehr einmalig gecacht: beide WallCheck-Services halten einen `IAudioListenerProvider` statt roher `Transform`. `SceneAudioListenerProvider` cached + self-heilt bei jedem Zugriff (kein Polling), Resolve-Entscheidung pure in `ListenerCachePolicy` (3 EditMode-Tests, mutation-geprüft). Fängt Respawn **und** Kamerawechsel per Disable/Enable ab. Details in CLAUDE.md (Abschnitt „Wall Check"). *Noch offen:* PlayMode-Smoke-Test fürs echte Self-Heal → in Release-Hygiene gelistet.

---

## 🟠 Wichtig (vor Release)

— Aktuell **keine offenen Punkte.** (W3 + M1 erledigt → siehe ✅ Erledigt.) Nächste offene Härtung steht in 🟡 Mittel (M3, M4, R3, R4).

> Die R-Punkte stammen aus dem **zweiten Review-Durchgang (2026-06-21)** — freies Gesamt-Lesen + lokaler Max-Effort-Recall-Pass.
> **Verworfen — R1 (vermeintlich „Loops loopen nicht"):** Falsch-Positiv. Beruhte auf der Fehlannahme `!IsOneShot ⟹ muss loopen`. `IsOneShot` ist die *Dispatch-Methode* (`PlayOneShot` vs. `source.Play()`), **orthogonal** zu `loop`. Ein non-OneShot spielt korrekt **einmal** als gemanagte, stopp-/fadebare Quelle — kein Bug. `loop` ist ohnehin ein bekannter **Wachstumskandidat** (CLAUDE.md), kein kaputtes Feature. Nicht erneut als Bug aufmachen.

---

## 🟡 Mittel

### M3 — Leeres PlayMode-Test-Assembly
- [ ] **erledigt**
- **Ort:** `Tests/PlayMode/AudioFramework.Tests.PlayMode.asmdef` (nur asmdef + meta, keine Tests).
- **Problem:** Assembly ohne Inhalt — verwirrend, Ballast im Asset-Store-Bundle. Commit-Historie (`4853eae`) erwähnt PlayMode-Tests, die nicht eingecheckt sind.
- **Fix:** Entweder PlayMode-Tests hinzufügen (ideal zusammen mit K1) oder Assembly + Ordner entfernen.
- **Aufwand:** S

### M4 — OneShot-`BusyUntilTime` nutzt skaliertes `Time.time` → Slot hängt bei `timeScale = 0`
- [ ] **erledigt**
- **Ort:** `AudioOcclusionSmoothingService.cs` (~Z. 38, `Time.time >= BusyUntilTime`) und die Stelle, die `BusyUntilTime` setzt (Pool-/Playback-Pfad — beim Aufnehmen prüfen).
- **Problem:** `BusyUntilTime` wird aus `Time.time` (skaliert) abgeleitet; bei `Time.timeScale = 0` steht `Time.time`. Ein OneShot-Slot, der während einer `timeScale=0`-Pause läuft, wird rechnerisch nicht freigegeben, obwohl der Clip auf realer DSP-Zeit endet → Slot kann kurz hängen. Vorbestehend, **unabhängig von M1** (dort separat beobachtet, bewusst nicht mitgezogen).
- **Fix-Richtung (offen, vor Code entscheiden):** OneShot-Lebenszeit auf eine zeit-skalen-unabhängige Quelle stellen (`Time.unscaledTime` / `realtimeSinceStartup`) — konsistent mit der DSP-Wiedergabe und mit der M1-Entscheidung „Audio-Timing ≠ `timeScale`". **Designfrage:** soll ein OneShot bei `PauseAll` (echte Pause) anders behandelt werden als bei bloßem `timeScale=0`? Erst klären, dann test-first.
- **Aufwand:** S (Code) — kleine Designklärung davor.

### R3 — WallCheck-Schleife ist generation-blind → kann an einen neu vergebenen Slot „andocken" (Review 2026-06-21)
- [ ] **erledigt**
- **Ort:** `AudioPlaybackService.cs` (`Dispatch` ruft **kein** `StopActiveCheck` bei Neubelegung) + `ShouldContinueLoop` in **beiden** WallCheck-Services.
- **Problem:** Die Schleife arbeitet nur mit `poolIndex`, prüft nie die Generation. Beim Slot-Reuse wird die alte Schleife **nur** gestoppt, wenn der neue Sound selbst `UseWallCheck` hat (dann `StartWallCheckLoop` → Cancel). Belegt ein Sound **ohne** WallCheck den Slot, läuft die alte Schleife verwaist weiter: occludierter OneShot endet, im Rest-Intervallfenster (≤ `TimeIntervalBetweenPositionChecks`, default 0.25 s) wird der Slot neu vergeben → Alt-Schleife sieht `isPlaying == true` des Fremd-Sounds und raycastet weiter für ihn, bei Loop-Nachfolger **endlos**. Inaudibel (`filter.enabled == false` → OcclusionSmoothing überspringt), aber verschwendete Raycasts pro Intervall — VR-/Performance-relevant.
- **Fix-Richtung:** Pragmatisch — `Dispatch` ruft **immer** `wallCheckService.StopActiveCheck(poolIndex)` vor Neubelegung (idempotent). Strukturell sauberer — Schleife generation-aware machen (analog `AudioHandle`); das wäre die tiefere Lösung statt eines Sonderfalls. Vor Code entscheiden welche Tiefe. Test-first.
- **Konfidenz:** Mittel-hoch (Logikschluss, kein Laufzeitbeweis). **Aufwand:** S–M.

### R4 — WallCheck-Intervall nutzt skalierte Zeit → friert bei `timeScale = 0` ein (Review 2026-06-21)
- [ ] **erledigt**
- **Ort:** UniTask `AudioUniTaskWallCheckService.cs` (~Z. 70, `UniTask.Delay(..., DelayType.DeltaTime)`), Coroutine `AudioCoroutineWallCheckService.cs` (~Z. 41, `WaitForSeconds`). Beide sind `timeScale`-skaliert.
- **Problem:** Bei `timeScale = 0` (ohne `PauseAll`) friert der WallCheck-Loop ein → `TargetCutoff` eines weiterspielenden `RespectsGlobalPause=false`-Sounds wird stale. Inkonsistent mit der M1-Linie „Audio-Timing ≠ `timeScale`". **Gehört zur timeScale-Familie M1 (erledigt) + M4 — am besten in einem Rutsch entscheiden.** Verteidbar als „WallCheck pausiert mit dem Spiel", aber dann sollte es bewusst so dokumentiert sein.
- **Fix-Richtung:** `DelayType.UnscaledDeltaTime` / `Realtime` bzw. `WaitForSecondsRealtime` — oder bewusste Doku-Entscheidung. In **beiden** Services spiegeln (Coroutine-Sammel-Check).
- **Aufwand:** S — Designklärung mit M4 bündeln.

---

## ⚪ Niedrig / Politur & Wartbarkeit

### P2 — `AudioFollowService` dupliziert die StopSlot-Logik inline
- [ ] **erledigt** — `AudioFollowService.cs` (~Z. 46–60) wiederholt `AudioStopService.StopSlot` fast wörtlich. Konsolidieren (StopSlot + `ClearFade` + `SetFollowTarget(null)` + Warnung). Wartungsrisiko. Aufwand: S

### P3 — Mutable struct `AudioObject` im Array + lokale Kopie in `Dispatch`
- [ ] **erledigt** — `AudioObject.cs` / `AudioPlaybackService.cs` (~Z. 111). Aktuell korrekt, aber `AudioObject poolObject = PoolArray[i]` als Kopie ist ein Footgun: künftiges `poolObject.<wertfeld> = …` läuft ins Leere. Kommentar oder `ref`-Zugriff. Aufwand: S

### P4 — OneShot setzt `source.clip` UND ruft `PlayOneShot`
- [ ] **erledigt** — `AudioPlaybackService.cs` (~Z. 116 + 145). `PlayOneShot(clip)` braucht `source.clip` nicht — redundant/irreführend. Aufwand: S

### P5 — Fade auf OneShots ist konzeptionell unsauber
- [ ] **erledigt** — Endet der Clip vor Ablauf der Fade-Dauer, bricht der Sound ab statt zu faden. Kein Crash; Erwartung dokumentieren oder Fade auf Loops beschränken. Aufwand: S

### P7 — Fehlende Tooltips für `IsOneShot`, `CanHandleAudioSource`, `UseWallCheck`
- [ ] **erledigt** — `AudioDataObject.cs`. Vor Release alle Inspector-Felder mit vollständigen Tooltips. Aufwand: S

### R5 — `InitializePool` validiert die Prefab-Komponenten nicht (Review 2026-06-21)
- [ ] **erledigt** — `AudioPoolAcquisitionService.cs` (~Z. 37). `GetComponent<AudioSource>()`/`<AudioLowPassFilter>()` ohne Null-Check → ein falsch gebautes Prefab gibt später einen kryptischen NRE in `GetFreeAudioSourcePoolIndex` (`Source.isPlaying`) statt einer klaren Init-Fehlermeldung. Asset-Store-UX (Idiotensicherheit). Aufwand: S

### R6 — `AudioSystemConfig` ohne `OnValidate`-Guards (Review 2026-06-21)
- [ ] **erledigt** — `AudioSystemConfig.cs`. Kein Guard, dass `DefaultCutoffFreqValue > MinCutoffFreqValue` (oder dass Prefab/TransferObject gesetzt sind). Bei Fehlkonfig (Min über Default) floort die Occlusion sofort → alles dumpf ab Frame 1, ohne Inspector-Warnung. Härtung. Aufwand: S

### R8 — Veraltete XML-Doc in `CalculateCutoffFrequency` (Coroutine) (Review 2026-06-21)
- [ ] **erledigt** — `AudioCoroutineWallCheckService.cs` (~Z. 50). `<param name="hitInfo">`/`<param name="originPos">` passen nicht zur Signatur — Copy-Paste-Rest, doc rot in einer ausgelieferten Datei. Trivial. Aufwand: S

---

# Teil B — Geplante Features & Roadmap

> Aus CLAUDE.md zusammengeführt. **Roadmap-Split ist weich, kein hartes Gate:** „nach Launch"-Items können vor 1.0 reingezogen werden. Jedes Feature wird red-first/sauber gebaut (siehe TDD-Regel oben).

## Vor Release (1.0) — fest eingeplant

### Breite-Features (höchster Verkaufshebel — Strategie in CLAUDE.md)
- [ ] **Mixer/Bus + Ducking** — Lautstärke-Kategorien an Unity-AudioMixer-Groups koppeln, Laufzeit-Volume (Settings-Menü) und Ducking (z. B. Dialog senkt Musik). Baut auf dem bestehenden `AudioCategory`/VolumeDictionary-System auf. **Höchster Verkaufshebel** (fast jedes Spiel braucht Volume-Menü + Ducking).
- [ ] **Random Pitch/Volume-Variation** — pro ADO optionale Streuung gegen den „Maschinengewehr-Effekt" bei wiederholten One-Shots (Footsteps, Hülsen). Billiger, sofortiger Qualitätsgewinn; greift mit den layer-reaktiven Sounds ineinander.
- [ ] **Adaptives/interaktives Musik-Layer** — mehrere Musik-Stems, je nach Spielzustand ein-/ausgeblendet (nutzt die Fade-Familie). Größter Abstand zu FMOD/MasterAudio, größter „Wow"-Effekt — und der größte Brocken.

### Release-Hygiene
- [ ] **Tooltips** — alle Inspector-Felder vollständig (insb. `IsOneShot`, `CanHandleAudioSource`, `UseWallCheck`). (= P7 oben.)
- [ ] **`IGetPoolIndex` entfernen** — toter Platzhalter (Lightweight-Pool über per-Sound-Flags gelöst, siehe „Reißbrett" unten). War in der „nicht ohne Rücksprache"-Liste — diese Notiz IST der Rücksprache-Trigger.
- [ ] **Null-Einträge in `CurrentClips` validieren** (= R2, Schwere geschärft 2026-06-21) — `Random.Range` kann einen `null`-Eintrag wählen. Die Validierung in `Dispatch` (~Z. 95) guardet nur `CurrentClips == null || Length == 0`, **nicht** einzelne null-Elemente. Bei OneShot ist `SetSlotBusy(poolIndex, currentClip.length)` (~Z. 135) dann eine **NullReferenceException** — schärfer als das ursprüngliche „stiller Sound". Fix: null-Eintrag beim Pick skippen/warnen, oder ADO-Inspector-Validierung.
- [ ] **Doku: Fade/Occlusion-Timing an `PauseAll`, nicht `timeScale`** (aus M1) — im User-Doc (DE+EN 1:1) festhalten, dass Fades und Occlusion-Glides in realen Sekunden laufen und von `Time.timeScale` entkoppelt sind: `timeScale = 0` pausiert sie NICHT, das macht `PauseAll`. Stil: nur IST-Zustand, kein „früher/jetzt".
- [ ] **PDF-Dokumentation** — `.md`-Dateien existieren (DE/EN), Konvertierung zu PDF offen.
- [ ] **Ordnerstruktur für Asset Store** — noch nicht definiert.
- [ ] **PlayMode-Smoke-Tests für die Fade-Glue** — fadet `source.volume` wirklich über Frames, gibt FadeOut den Slot frei, friert echtes `PauseAll` einen Fade ein. **Plus M1-Fall:** bei `Time.timeScale = 0` läuft ein laufender Fade (und der Occlusion-Glide) weiter — `source.volume` ändert sich über Frames trotz `timeScale = 0` (beweist `unscaledDeltaTime`). Locked die aktuell nur manuell verifizierte Verdrahtung. Gutes Bündel mit M3 + K1-PlayMode-Test.
- [ ] **PlayMode-Smoke-Test für `SceneAudioListenerProvider`** (aus W3) — in kontrollierter Szene das Self-Heal beweisen: (a) `TryGetPosition` liest die *aktuelle* Listener-Position (nicht die Start-Position), (b) nach Disable des alten + Enable eines neuen Listeners liefert er den neuen, (c) nach Zerstörung des Listeners `false`. Bewusst PlayMode statt EditMode, weil `FindObjectsByType` sonst den AudioListener der offenen Editor-Szene fände. Bündelt mit M3 + K1.

## Weitere geplante Features (Priorität offen — können vor 1.0 rein)

- [ ] **Layer-basierte reaktive Geräusche** — Sounds je nach getroffenem Layer unterschiedlich (Footstep auf Holz vs. Metall vs. Boden). Mapping Layer/Material → AudioClip(-Gruppe), analog zum Layer→Cutoff-Dictionary.
- [x] **Multiplikativer (faktorbasierter) Frequenzabfall bei Occlusion — erledigt 2026-06-20.** Cutoff-Reduktion pro Wand ist jetzt **multiplikativ**: jeder Layer trägt einen Dämpfungsfaktor `0..1`, der den laufenden Cutoff Richtung Floor dämpft (`WallOcclusionMath.ApplyWall = current − (current − floor)·d`, 9 EditMode-Tests). Reihenfolge-unabhängig, asymptotisch zum Floor, Layer-Werte baseline-unabhängig. Offene Frage „Hz vs. Faktor" → **Faktor** entschieden: Datenfeld `CutoffFreqLayerBehaviour.CutoffFrequencyValue` → `WallDampingLayer.WallDampingFactor` (`[Range(0,1)]`-geguardet), Array `CutOffFrequenciesPerLayer` → `WallDampingPerLayer`, `ConfigForTests`-Werte auf 0.45/0.65/0.82 migriert. Ein künftiges echtes Log-Mapping bliebe eine lokale Änderung im `ApplyWall`-Rumpf.
- [ ] **Occlusion-Spawn-Snap** (Mini) — ein Sound, der DIREKT hinter einer Wand startet, gleitet aktuell ~0.3 s hell→dumpf. Optional „beim allerersten WallCheck-Tick snappen, danach glätten" via `hasTarget`-Flag pro Slot. Nur falls es stört.
- [ ] **CTS-Reuse im WallCheck** — pro `Play()` mit WallCheck wird eine `CancellationTokenSource` alloziert (`CreateLinkedTokenSource`) → „Zero GC" stimmt noch nicht ganz. Reuse / `TryReset`.
- [ ] **Multiplayer & VOIP** — siehe Reißbrett unten.
- [ ] **VR-Optimierung via RaycastCommand** — Wall-Checks feuern `Physics.Raycast` einzeln auf dem Main Thread. Für mobiles VR (Quest) bei vielen Quellen kritisch. Umstieg auf gebatchte, jobified `RaycastCommand` (Burst/Job System). Architektur ist sonst schon VR-tauglich (kein GC, Pooling, UniTask, ein AudioListener). Konkreter nächster Schritt, sobald VR Zielgruppe wird.

## Größere Vorhaben / Reißbrett

### Multiplayer & VOIP (zwei getrennte Teile)
1. **Multiplayer (networked sounds):** Tool ist client-seitig — Networking entscheidet WANN/WO, das Spiel ruft `PlaySpatial(ado, networkedTransform)`. Großteils „nichts nimmt Single-Player an"-Pass + dokumentierte Patterns. Ein lokaler AudioListener pro Client ist ok. Viele Remote-Emitter → mehr Spatial-Sounds + Wall-Checks → koppelt an VR/RaycastCommand.
2. **VOIP (der härtere, interessantere Teil):** Großes Verkaufsargument = **Proximity-Voice mit Wall-Occlusion** (Stimme hinter Wand wird gedämpft). Snag: VOIP ist ein **Live-PCM-Stream**, kein Clip aus `ado.CurrentClips`. Braucht einen Pfad, um Occlusion/Follow/Volume/Pause über eine AudioSource laufen zu lassen, die das Tool NICHT aus einem ADO-Clip erzeugt hat → entweder (a) „bring-your-own-AudioSource"-Registrierung in einen Slot, oder (b) Streaming-Clip-ADO-Variante. Bewusst entscheiden. Cross-Cutting: VOIP meist `RespectsGlobalPause=false`, `FollowEmitter` auf Remote-Avatar.

### Multi-Pool — als eigenständige Architektur VERWORFEN (Referenz)
Die Rationale (lightweight vs. occlusion; UI/2D) ist via per-Sound-Flags am Einzel-Pool gelöst: `UseWallCheck` (Loop startet nur bei true → 0 Raycast sonst), `PlayNonSpatial` (spatialBlend 0), `filter.enabled=false` für nicht-occludierte. Pause-Scope (war DER Blocker) ist via `RespectsGlobalPause`/`IsPaused` gelöst. **Einziger echter Verlust eines Einzel-Pools:** Kapazitäts-Partitionierung (spammy Footsteps können einen wichtigen Occlusion-Sound aushungern). Für ~50 kurzlebige Slots klein/billig zu mitigieren (großzügig dimensionieren; optional Priority/Eviction später). Erst bauen, wenn Patrick heavy simultanes Lightweight-Spam hat — dann sauber via `IAudioPool` + Shared Base, nicht angeklebte `if`s.

## Gesonderte Aufgaben (nach den ersten Features)

- [ ] **Kontext-Refactor: Main- vs. Projektdatei trennen → erweitern → Fragebogen blockweise** — Claudes Kontext entmischen. Schritte in Reihenfolge: **(1)** Aktuelle `CLAUDE.md` dekomponieren in *allgemeingültigen Kanon (gilt für JEDES von Patricks Projekten)* → wandert in eine **user-level Main-Datei `~/.claude/CLAUDE.md`** (lädt Claude Code automatisch in jedes Projekt), und *projektspezifisches AudioTool-Wissen* → bleibt in der projekt-lokalen `CLAUDE.md`. **(2)** Beide Dateien um relevante Kontexte erweitern. Projekt-*Typ* (Asset-Store-Plugin vs. gekapseltes Game-Feature, inkl. „Idiotensicherheit"-Dosierung) NICHT als drittes File, sondern als Abschnitt in der Projektdatei — Fragmentierung/Drift vermeiden. **(3)** Erst danach den **Fragebogen** erstellen und **blockweise/iterativ** durchgehen (z. B. SOLID/Clean Code → Design Patterns → Unity-Idiome → API-/Asset-Store-Härtung), um den Kontext zu schärfen. Fragen sollen Trade-off-Entscheidungen + Grenzen hervorlocken, nicht Lehrbuch-Bekenntnisse. **Patrick trifft die Entscheidungen, Claude entscheidet nicht selbst.** Universelle Konstanten (gelten für alle): Testbarkeit, Wartbarkeit, Erweiterbarkeit, Skalierbarkeit; Unit-Tests immer & überall. Überschneidet sich mit „Coding Guidelines gemeinsam erarbeiten" (ggf. zusammenführen). Aufwand: L (laufend, kollaborativ).

- [ ] **Coding Guidelines gemeinsam erarbeiten** — über die nächsten Sessions verteilt einen festen Satz Code-Konventionen/Stil-Richtlinien mit Patrick ausarbeiten (Naming, Struktur, Kommentare, Test-Konventionen, Service-/Seam-Muster usw.). Ergebnis wird dokumentiert (eigener Abschnitt in CLAUDE.md oder separate `CODING_GUIDELINES.md` — beim Start klären). Laufende, kollaborative Aufgabe.
- [ ] **Bestandscode nachtesten (= M2)** — bestehende Methoden sukzessive mit Unit Tests abdecken; ggf. kleine Refactorings für Testbarkeit (pure Logik aus Unity-Abhängigkeiten ziehen, analog `AudioFadeMath`). Vollständiges Methoden-Audit (Session 2026-06-08) in drei Gruppen nach Testbarkeit. Test-first für jede extrahierte Logik. Aufwand: L (laufend).
  - **✅ Gruppe A — pure/fast-pure, erledigt 2026-06-08** (14 Tests, alle mutation-geprüft):
    - `AudioManagerDictionaryProvider.FillLayerMaskDictionaryWithLayerRelatedValues` — 4 Tests (keep-first bei Duplikat-Layer).
    - `AudioManagerDictionaryProvider.FillDictionaryWithKeysAndValues` — 6 Tests (null/empty-Guards + skip-but-continue bei null-Eintrag + keep-first).
    - `WallLayerMask.FromLayers` — neu aus beiden WallCheck-Services extrahiert (pure Bitmask-Faltung), 4 Tests (OR-Faltung). Beide Services rufen jetzt den geteilten Helper.
  - **Gruppe B — echte Entscheidungslogik, Unity-gekoppelt → kleinen Seam VOR dem Test ziehen:**
    - [x] **`AudioPoolAcquisitionService.GetFreeAudioSourcePoolIndex` — erledigt 2026-06-15** (Commit `e0ac058`). „Slot frei?"-Prädikat in pure `PoolSlotAvailability.IsFree(isPlaying, currentTime, busyUntilTime, isPaused)` extrahiert (Unity-frei, nimmt rohe Werte statt `AudioObject`), 5 EditMode-Tests (je eine Klausel: silent+elapsed+unpaused=frei, playing=belegt, busy-window offen=belegt, paused=belegt trotz sonst-frei, `currentTime == busyUntil`=frei [inklusive Grenze]). `IsPaused`-als-belegt mit abgedeckt.
    - [ ] `ShouldContinueLoop` (beide WallCheck-Services) — OneShot/Loop/Paused-Fortsetzungsprädikat extrahieren.
    - [ ] `CalculateCutoffFrequency` — Akkumulation über den Hit-Buffer als Fold-Helper (Pro-Wand-Schritt ist via `WallOcclusionMath` schon getestet).
    - [ ] `AudioPlaybackService.Gate` + `ResolveVolume` — Gating (`CanHandleAudioSource`) bzw. Volume-Fallback-Entscheidung.
    - [ ] `AudioPauseService.PauseAll`/`UnpauseAll` — Scope-Logik (nur wecken, was wir pausiert haben); braucht Fake/Seam übers Pause-Primitiv.
  - **Gruppe C — dünne Glue/Orchestrierung & MonoBehaviour → PlayMode oder geringer Testwert:** `AudioManagerDynamic`-API (einzige echte Logik = Crossfade-Komposition), `AudioStopService.StopSlot`, `AudioFollowService.UpdateFollowers`, `AudioOcclusionSmoothingService.Tick`, WallCheck-Token-Lebenszyklus. Bündelt mit M3 + K1-PlayMode-Tests.

- [ ] **Coroutine-Variante gebündelt unter `!USE_UNITASK` gegenprüfen** — Änderungen an `AudioCoroutineWallCheckService` werden von Hand 1:1 zur UniTask-Version gespiegelt, bei aktivem UniTask aber NICHT mitkompiliert (`#if`). Statt jedes Mal umzuschalten, prüft Patrick solche Spiegelungen **gesammelt in einem Rutsch** (UniTask testweise aus → Compile + Coroutine-Smoke). **Offen seit 2026-06-08:** `GenerateLayerMaskFromDictionary` → `WallLayerMask.FromLayers`-Umstellung. **Dazu (2026-06-20):** W3-Umstellung — Ctor-Param `Transform _playerListener` → `IAudioListenerProvider _listenerProvider` und `CalculateCutoffFrequency` (`playerListener.position`/`== false` → `listenerProvider.TryGetPosition(out …)`) in *beiden* Services gespiegelt; bei aktivem UniTask kompiliert die Coroutine-Variante nicht mit → im Sammel-Check (UniTask aus) gegenprüfen. Künftige Doppel-Service-Änderungen hier anhängen, bis der Sammel-Check läuft. **Dazu (R7, Review 2026-06-21):** die Coroutine-Variante cached `AudioSource targetSource = poolArray[poolIndex].Source` einmal (~Z. 65) und prüft `targetSource == false`, während die UniTask-Variante `poolArray[poolIndex].Source` bei jedem Tick live liest — Mirroring-Drift (kein aktiver Bug, Pool-Source ist stabil). Im Sammel-Check angleichen.
- [ ] **Pricing-Analyse** (wenn release-reif) — Asset-Store-Preise der Konkurrenz (MasterAudio & Co.) live benchmarken (WebSearch für aktuelle Zahlen), dann wertbasiert auf die Differenzierer ankern (saubere Architektur + Tests + lightweight Occlusion + Ease-of-use vs. FMOD). Finaler Preis ist Patricks Markt-Call; ich liefere Benchmark + Begründung, kein Verdikt.
