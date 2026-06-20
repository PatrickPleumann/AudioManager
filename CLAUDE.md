# AudioTool — CLAUDE.md

Dieses Dokument gibt Claude in einem neuen Chat den vollständigen Kontext über das Projekt: **was es ist, wie wir zusammenarbeiten, die Architektur und die getroffenen Designentscheidungen.**

> **📌 Dokumentations-Regel (verbindlich):**
> - **Diese Datei (CLAUDE.md) ist die EINZIGE Wissensablage.** Jede durable Erkenntnis — Designentscheidung, Prinzip, IST-Fakt, Arbeitsweise — wird **hier** festgehalten, NICHT in einem PC-gebundenen Claude-Memory. Das Memory wird bewusst nicht mehr beschrieben (vermeidet Drift; Wissen bleibt im Repo, versioniert und für Patrick lesbar).
> - **Neue Aufgaben, TODOs und geplante Features gehören in [`BACKLOG.md`](BACKLOG.md)** (Repo-Root) — nie hierher und nie ins Memory. Beim Abarbeiten dort die Checkboxen pflegen.
> - Kurz: **Wissen → CLAUDE.md · Aufgaben → BACKLOG.md · Memory → bleibt leer.**

---

## Was ist dieses Projekt?

**AudioTool** ist ein Unity Audio-Management-Framework, das als Unity Asset Store Plugin veröffentlicht werden soll. Zielgruppe sind Indie-Entwickler und kleine Teams, die kein Audio-Budget haben und sich nicht in FMOD/Wwise einarbeiten wollen.

Das Tool nimmt dem Entwickler die vollständige Verwaltung von `AudioSource`-Objekten ab. Ein einziger Aufruf reicht — den Rest erledigt das System.

**Wichtig:** Der Entwickler Patrick arbeitet mit Unity 6 und JetBrains Rider. Die Sprache im Chat ist Deutsch.

### Produkt-Strategie & Verkaufsargument (der „Moat")

Das Fundament steht sauber (Pooling, Occlusion, Pause/Follow, Fade-Familie — alle test-gestützt). Der Plan: **bewusst die Feature-Breite ausbauen**, um mit der Konkurrenz (MasterAudio, FMOD-nah) gleichzuziehen — aber jedes Feature mit **überlegener Struktur und Testdisziplin**, die die Konkurrenz nicht liefert. Das Verkaufsargument ist nicht Breite allein, sondern: „dieselben Features wie die großen Tools, aber sauber und testbar". Breite darf **niemals** auf Kosten dieser Qualitätskante gehen.

Ein weiteres bewusstes Verkaufsargument: Der Wall-Check ist **lightweight occlusion** (simpler Low-Pass), KEIN voller Spatializer wie Steam Audio/Oculus. Das ist ein Feature, kein Nachteil — klar so kommunizieren.

---

## Zusammenarbeit mit Patrick

> Die universellen Zusammenarbeits-Regeln (Solo-Dev, kein Ja-Sager, kein eigenständiges Committen, erst erklären dann Go, Sauberkeit vor Workaround, warmer Abschluss, Sprache, Testbarkeit) stehen in der user-level `~/.claude/CLAUDE.md` und laden automatisch in jede Session. Hier nur **AudioTool-Spezifisches**:

- **Pro-Plan-Hinweis:** Patrick stößt an Session-Token-Limits (Opus ist der schwere Treiber). Opus für die harten Teile sparen; Mechanisches kann günstiger laufen.

---

## Arbeitsweise: Test-Driven (nicht verhandelbar)

DIE Regel für jede neue Methode/jedes Feature. Formalisiert von Patrick, bindend. Der Loop, in Reihenfolge:

1. **Zuerst den fehlschlagenden Test schreiben.** Er muss rot sein, bevor Implementierung existiert.
2. **Diese Tests sind danach EINGEFROREN — werden nie wieder angefasst.** Bestehende Tests werden nicht editiert/abgeschwächt, um Code grün zu bekommen. *Neue* Tests für *neues* Verhalten sind ok.
3. **Die Methode schreiben**, die die eingefrorenen Tests grün macht.
4. **Wenn alles grün ist: bewusst einen Fehler einbauen** (Mutation Check), der mindestens einen Test rot macht — vorher vorhersagen welchen. Beweist, dass die Tests wirklich etwas schützen.
5. **Nach bestätigtem Rot: korrekten Zustand wiederherstellen.** Danach die Tests in Ruhe lassen.
6. **Wird eine Methode so umgebaut, dass ihre eingefrorenen Tests obsolet werden**, wandert die Nacharbeit als TODO in den BACKLOG — und wird NIE ohne Patricks ausdrückliche Anweisung ausgeführt. Keine stillen Test-Rewrites.

**Stützende Prinzipien:**
- **Erwartungswerte kommen aus der SPEZIFIKATION, nicht aus dem Code.** Vor dem Blick auf die Implementierung aus dem Vertrag hand-ableiten. Wenn „korrekt" ohne Code-Lesen nicht sagbar ist → STOP, erst das Soll mit Patrick klären.
- **Erstes Rot darf ein „laufendes Rot" sein:** neuen Typ/Member als `NotImplementedException`-Stub anlegen, damit das Test-Assembly kompiliert und die Tests *laufen* und scheitern (klarer als ein bloßer Compile-Fehler).
- **Aktuell testen wir NUR neuen Code.** Bestandscode nachzutesten ist eine separate, aufgeschobene Aufgabe (siehe BACKLOG) — nie still mit reingezogen.
- **Ehrliche Tests gewinnen Design-Trade-offs** („Ehrliche Tests sind besser"). Wenn die Wahl zwischen einer leicht-ehrlich-testbaren Architektur (Seam/Interface → EditMode-testbar mit Fake) und einer ohne Abstraktion (nur per langsamem/vagem PlayMode prüfbar) steht: die testbare wählen. Ein kleiner Seam ist es wert. Konkret umgesetzt: die pure-Logik-Klassen `AudioFadeMath`, `WallOcclusionMath`, `OcclusionSmoothing`, `LowPassDispatchPolicy`, `AudioHandleValidator`, `WallLayerMask` — Unity-frei, EditMode-getestet.

Patricks Kernangst sind tautologische / Change-Detector-Tests. Red-first + Einfrieren + Mutation Check sind die konkreten Schutzwälle.

### Zusätzliche Schutzregeln (aus konkreten Fehlentscheidungen, 2026-06-20)

Härten den Loop gegen die Patzer der Occlusion-Modell-Session. Gemeinsame Wurzel: „grün/erwartungskonform machen" wurde über „Diskrepanz verstehen" gestellt. Genau dieser Default wird hier umgedreht.

- **Bei rotem Test oder verfehlter Mutation-Vorhersage: niemals reflexartig den Test anfassen — erst ganzheitlich analysieren, wo der Fehler sitzt.** Der Test ist die **letzte** Instanz im Verdacht, erreichbar nur per Ausschluss. Diagnose-Reihenfolge; eine Stufe wird erst betreten, wenn die vorige als „nicht falsch" bestätigt ist:
  1. **Die Implementierung** — der Code unter Test.
  2. **Mein Modell / meine Hand-Herleitung** des Erwartungswerts aus der Spec.
  3. **Die Vorhersage selbst** — bei verfehlter Mutation-Prognose ist meist nur mein Modell *des Tests* daneben; der Mutation-Check ist ohnehin bestanden, sobald *mindestens ein* Test rot ist (Suite-Ebene, nicht pro Test).
  4. **Der Test** — zuletzt. Er *kann* falsch sein (falscher Erwartungswert, falsche/zu schwache Assertion, falsch dimensionierte Toleranz), und diese Möglichkeit verschließe ich mir nie. Aber sie wird erst gezogen, wenn der Kontext zwingend ergibt, dass *nur noch* der Test die Quelle sein kann und nichts anderes. Dann — und nur dann — Test **nach explizitem Go** anfassen, mit benannter Begründung *warum*. Nie zählt: ändern, *damit meine Prognose stimmt*, oder Schutz ergänzen, den ein grüner Test auf Suite-Ebene schon liefert (Gold-Plating).

- **Test-Änderung nur mit vorab benannter Kategorie; Default ist Veto.** Tests werden grundsätzlich nicht angefasst. Jeder Änderungsvorschlag muss *vor* der Änderung einer Kategorie zugeordnet werden: (a) echter Authoring-Defekt (z. B. falsche Toleranz), (b) bewusste Spec-Änderung mit Patricks Go, (c) obsolet durch Umbau → BACKLOG (Loop-Regel #6). Passt nichts davon → keine Änderung. Im Zweifel: Test stehen lassen und fragen.

- **Float-Erwartungswerte: Toleranz aus der Rechnung ableiten, nicht aus Reflex.** Entsteht ein Erwartungswert durch verkettete float32-Arithmetik mit nicht exakt darstellbaren Faktoren (z. B. `0.3f`, `0.8f`), die Toleranz an der akkumulierten Rundung ausrichten — für Cutoff-Hz: fachlich vernachlässigbar (~`1e-2`), aber weiterhin um Größenordnungen enger als jede sinnvolle Mutation. Kein Reflex-`1e-5` auf Float-Ketten.

---

## Architektur

```
AudioManagerDynamic (MonoBehaviour — Singleton, öffentliche API, treibt LateUpdate-Ticks)
├── AudioPoolAcquisitionService    → Pool aus AudioObject[] (AudioSource + LowPassFilter); Slot-Vergabe + Generation
├── AudioPlaybackService           → Dispatching (Play/FadeIn-Silent), Stop-Einstieg, Volume-Resolve, Handle-Gating
│   └── AudioStopService           → einziger „Slot stoppen"-Pfad (Source.Stop + Reset + WallCheck stop), fade-frei
├── AudioUniTaskWallCheckService   → Raycast-Loop per UniTask (empfohlen)   ┐ setzen nur noch TargetCutoff
├── AudioCoroutineWallCheckService → Raycast-Loop per Coroutine (Fallback)  ┘ (geteilte WallOcclusionMath + WallLayerMask)
│   └── SceneAudioListenerProvider  → liefert die *aktuelle* AudioListener-Position (lazy + self-heal bei Zugriff, kein Polling)
├── AudioOcclusionSmoothingService → gleitet Filter.cutoffFrequency pro Frame Richtung TargetCutoff (LateUpdate)
├── AudioFollowService             → kopiert Emitter-Position pro Frame (LateUpdate), ohne Parenting
├── AudioFadeService               → treibt alle Fades pro Frame (LateUpdate) über IFadeTarget[]
├── AudioPauseService              → Pause/Unpause der Pool-Slots (global, scope-bewusst)
└── AudioManagerDictionaryProvider → Volume- & LayerMask-Dictionaries
```

**Pure, Unity-freie Logik-Klassen (EditMode-getestet):** `AudioFadeMath`, `WallOcclusionMath` (Pro-Wand-Cutoff-Schritt + Floor-Clamp), `OcclusionSmoothing` (Per-Frame-Glide), `LowPassDispatchPolicy` (Filter-Zustand pro Dispatch), `AudioHandleValidator` (Handle-Currency: Bounds + Generation), `WallLayerMask` (Layer-Indizes → Bitmask, von beiden WallCheck-Services geteilt), `ListenerCachePolicy` (Resolve-Entscheidung: neu auflösen ⟺ kein Cache ODER gecachter Listener nicht mehr lebend & aktiv).

### Wichtige Klassen & Dateien

| Datei | Zweck |
|---|---|
| `AudioManagerDynamic.cs` | Singleton, öffentliche API, LateUpdate-Treiber |
| `AudioDataObject.cs` | ScriptableObject — Konfiguration pro Sound (ADO, „Control Surface") |
| `AudioSystemConfig.cs` | ScriptableObject — zentrale System-Konfiguration |
| `AudioSourceVolumes.cs` | ScriptableObject — Lautstärke pro Kategorie |
| `AudioVolumesTransferObject.cs` | Bündelt alle AudioSourceVolume-Assets (nur eine Instanz) |
| `AudioCategory.cs` | Enum — Lautstärke-Kategorien (Beispielwerte, muss angepasst werden) |
| `AudioHandle.cs` | Readonly struct `{ PoolIndex, Generation }` — Referenz auf eine Slot-Belegung; Ctor `internal` |
| `AudioObject.cs` | Struct — ein Pool-Slot (GameObject, Source, Filter, BusyUntilTime, Generation, TargetCutoff, Follow-/Pause-State) |
| `SoundRequest.cs` | Readonly struct `{ Ado, Source }` — Event-Payload für `PlaySpatial(SoundRequest)` |
| `IAudioWallCheckService.cs` | Interface — Strategy Pattern für WallCheck (UniTask/Coroutine) |
| `IAudioListenerProvider.cs` | Interface — liefert die aktuelle AudioListener-Position (`TryGetPosition`); Seam gegen stale Transform |
| `SceneAudioListenerProvider.cs` | Unity-Impl — lazy Auflösung + Self-Heal des aktiven Listeners (kein Polling) |
| `ListenerCachePolicy.cs` | Pure — „Listener neu auflösen?"-Entscheidung (EditMode-getestet) |
| `IGetPoolIndex.cs` | Interface — toter Platzhalter, steht im BACKLOG zur Entfernung |

---

## Öffentliche API

```csharp
// Abspielen
AudioHandle h = AudioManagerDynamic.PlaySpatial(myADO, sourceTransform);   // 3D, positionsbezogen, optional wall-checked
AudioHandle h = AudioManagerDynamic.PlaySpatial(soundRequest);             // dito, gebündeltes { Ado, Source }
AudioHandle h = AudioManagerDynamic.PlayNonSpatial(myADO);                 // 2D (spatialBlend = 0, kein WallCheck)

// Stoppen (nur wirksam bei gültigem, aktuellem Handle → CanHandleAudioSource == true)
AudioManagerDynamic.Stop(h);

// Faden (Fades sind immer „managed" → liefern IMMER einen Handle, unabhängig von CanHandleAudioSource)
AudioHandle h = AudioManagerDynamic.FadeInNonSpatial(myADO, duration);
AudioHandle h = AudioManagerDynamic.FadeInSpatial(myADO, sourceTransform, duration);
AudioManagerDynamic.FadeOut(h, duration);                                  // fadet runter, stoppt, gibt Slot frei
AudioHandle h = AudioManagerDynamic.CrossfadeNonSpatial(fromHandle, toADO, duration);
AudioHandle h = AudioManagerDynamic.CrossfadeSpatial(fromHandle, toADO, sourceTransform, duration);

// Pause
AudioManagerDynamic.PauseAll();
AudioManagerDynamic.UnpauseAll();
```

`Crossfade` ist **Komposition** aus `FadeOut(from)` + `FadeIn(to)`, kein Spezial-Pfad.

---

## Wichtige Designentscheidungen

### ADO ist die „Control Surface" (zentrales Invariant)
Das `AudioDataObject` ist bewusst ein serialisierter **Spiegel der AudioSource-Einstellungen**, der zur Play-Zeit auf den gepoolten `AudioSource` geschrieben wird. **Jede gespiegelte Eigenschaft MUSS bei JEDEM Dispatch geschrieben werden — unbedingt, nie in einem `if`.** Sonst trägt ein wiederverwendeter Slot den **vorherigen** Sound-Wert → stille, schwer findbare Bugs (genau so passiert mit `spatialBlend`). Beim Hinzufügen eines neuen gespiegelten Feldes immer die unbedingte `source.<prop> = ado.<prop>`-Zeile in `AudioPlaybackService.Dispatch` mitsetzen. Wachstumskandidaten: `pitch`, `loop`, `priority`, `minDistance`/`maxDistance`, `rolloffMode`, `spread`.

### Pool
- Festes `AudioObject[]`-Array, vorab instanziiert — kein GC zur Laufzeit.
- `BusyUntilTime` als Zeitstempel-Trick für OneShot-Slots.
- Pool-Suche O(n) ab Index 0 — bewusst einfach.
- **Generation pro Slot:** Jede (Neu-)Vergabe in `GetFreeAudioSourcePoolIndex` erhöht `AudioObject.Generation`. Der zurückgegebene `AudioHandle` trägt diese Generation. Stop/Fade prüfen via `AudioHandleValidator`/`IsHandleCurrent`, ob Generation **und** Bounds passen — sonst stilles No-op. So kann ein alter Handle nach Slot-Reuse nicht den neuen, fremden Sound stoppen/faden. Der `AudioHandle`-Ctor ist `internal` (Handles sind reine Ausgabewerte; verhindert selbstgebaute Crash-Handles).

### Wall Check (lightweight occlusion)
- `Physics.RaycastNonAlloc` mit `RaycastHit[8]`-Buffer (max. 8 Wände).
- Layer-basierte **Dämpfung** — jeder getroffene Layer dämpft den laufenden Cutoff um einen konfigurierbaren **Faktor `0..1`** (`WallDampingLayer.WallDampingFactor`, `[Range(0,1)]`-geguardet) Richtung Floor (`WallOcclusionMath.ApplyWall`, **multiplikativ**: `current − (current − floor)·d`). Über N Wände skaliert der offene Bereich über dem Floor mit `∏(1 − dᵢ)` → reihenfolge-unabhängig und **asymptotisch** zum Floor; `ClampToFloor` ist nur noch Sicherheitsnetz gegen Fehlkonfig (`d>1`). `0` = Wand transparent, `1` = fällt in einer Wand auf `MinCutoffFreqValue`.
- **Offener Cutoff = `DefaultCutoffFreqValue` ≈ 22000 Hz** (Obergrenze des Gehörs → transparent). Niedrigere Werte klingen dumpf.
- **Filter nur für wand-geprüfte Sounds aktiv:** `filter.enabled = ado.UseWallCheck` bei jedem Dispatch (`LowPassDispatchPolicy`). Alle anderen Sounds (2D-Musik, UI, nicht-occludierte SFX) umgehen den Filter komplett → transparenter Klang + weniger DSP.
- **Weiche Übergänge:** Der WallCheck-Loop setzt nur noch `AudioObject.TargetCutoff`; `AudioOcclusionSmoothingService` gleitet `filter.cutoffFrequency` pro Frame dorthin (`OcclusionSmoothing.Step`, MoveTowards mit `OcclusionSmoothingSpeed` Hz/s; `0` = sofort). Kein „Pop" mehr beim Aus-der-Wand-Treten.
- WallCheck nur wenn aktiv → kein Raycast bei pausierten Sounds. `ShouldContinueLoop()` unterscheidet OneShot (BusyUntilTime) und Loop (isPlaying) und hält den Loop bei `IsPaused` am Leben (sonst kehrt Occlusion nach Unpause nicht zurück).
- **Listener-Bezug self-healing statt gecacht:** Der WallCheck hält nicht mehr die rohe Listener-`Transform`, sondern einen `IAudioListenerProvider`. `SceneAudioListenerProvider` cached den `AudioListener`, validiert ihn aber bei **jedem** Zugriff (`!= null && isActiveAndEnabled`) und löst nur im Ungültig-Fall neu auf (`FindObjectsByType` → erster aktiver Listener). Kein Intervall-Polling — der teure Scan feuert nur im Wechsel-Moment, der Happy Path ist ein Null-Check + ein Bool. Fängt **Respawn** (zerstört → `null`) **und Kamerawechsel per Disable/Enable** (`!isActiveAndEnabled`) ab. `TryGetPosition(out)` → bei `false` weiterhin `DefaultCutoffFreqValue` (unverändertes „kein Listener"-Verhalten). Die Resolve-*Entscheidung* lebt in der pure `ListenerCachePolicy` (Modell-Seam, wie `WallOcclusionMath`).
- **`WallOcclusionMath` ist der Modell-Seam:** Das multiplikative Dämpfungs-Modell lebt allein im `ApplyWall`-Rumpf (Einzelstelle) — ein künftiger Wechsel (z. B. echtes logarithmisches Mapping) bliebe eine lokale Änderung an dieser einen Stelle.

### Pause-Modell (ohne Multi-Pool gelöst)
- Pro-ADO `RespectsGlobalPause` (Default true; regelt NUR die globale `PauseAll`/`UnpauseAll`, nicht `Stop(handle)`).
- Laufzeit-Flag `AudioObject.IsPaused` trackt, was *wir* pausiert haben: (a) `GetFreeAudioSourcePoolIndex` behandelt pausierte Slots als belegt (ein pausierter AudioSource meldet `isPlaying == false` → würde sonst überschrieben), (b) `UnpauseAll` weckt nur, was es pausiert hat, (c) `StopAudio` + Follow-Cleanup räumen `IsPaused`. Pro Dispatch via `SetPausePolicy` gespiegelt (Control-Surface).

### Fade-Familie
- Framework-agnostischer `AudioFadeService`, pro Frame aus `LateUpdate` getrieben (wie Follow). Index-basiert über `IFadeTarget[]` (gleiche Größe/Index wie der Pool → der Pool-Index ist der geteilte Schlüssel).
- Reale Targets = `PooledFadeTarget` (Volume → `source.volume`, Stop → `AudioStopService.StopSlot`). Pause-bewusst: ein pausierter Fade friert ein.
- Fade ist ein **Laufzeit-Override** des kategoriebasierten Volumes; settled am Ende auf die Kategorie-Lautstärke (FadeIn) bzw. erreicht 0 und gibt frei (FadeOut). Reset-Punkte (jeder Dispatch, Stop, Follow-Target-Tod) räumen den Fade, damit er keinen wiederverwendeten Slot überschreibt.

### Follow ohne Parenting
- Spatiale Sounds folgen einem Emitter, indem die Position pro `LateUpdate` kopiert wird — **nie** per `SetParent` (Parenting würde den Pool-Slot dem Aufrufer „schenken": Zerstört der seinen Emitter, würde der gepoolte Slot mitsterben). Stirbt das Follow-Target mitten im Sound, wird gestoppt + Slot freigegeben (ein Follow-Sound ist meist ein Loop → würde sonst ewig am Todesort weiterlaufen).

### Token-Management
- `CancellationTokenSource[]` liegt **ausschließlich** im jeweiligen WallCheck-Service. `AudioManagerDynamic` kennt keine Tokens — vollständige Interface-Abstraktion.

### Singleton-Schutz
- Mehrere Instanzen werden in `Awake` erkannt und zerstört (mit Warning). `OnDestroy` räumt nur, wenn es die echte Instanz ist (`if (instance != this) return;`) — sonst würde ein am Frame-Ende zerstörtes Duplikat (z. B. additives Szenenladen) die statische Referenz auf die lebende Instanz nullen. Vorbedingungen (Config, AudioListener) werden VOR `instance = this` geprüft → Invariante: `instance != null` ⟺ voll initialisiert.

### Doku-Regel
- Die User-Dokumentation beschreibt **nur den aktuellen Zustand** — keine „nicht mehr / früher / jetzt geändert"-Formulierungen. Das Tool ist unveröffentlicht; es gibt keine Vorversion zum Vergleich. (`AudioTool_Documentation_DE.md` / `_EN.md` — EN spiegelt DE 1:1.)

---

## Was NICHT angefasst werden soll ohne Rücksprache

- `TestScript.cs` — nur zum Testen, kein Produktionscode.
- `AudioCoroutineWallCheckService` — Fallback. Nur **parallel** zur UniTask-Version anpassen (beide synchron halten).

---

## UniTask-Versionspolitik (entschieden)

Floor `2.3.0` in `AudioFramework.asmdef` (`versionDefines.expression = "2.3.0"` — Unity verlangt die bloße Version, kein `[2.3.0,)`). Der Gate ist ein **Sicherheitsschalter, kein Min-to-work**: unterhalb fällt `USE_UNITASK` weg und der Code nutzt den voll funktionsfähigen `AudioCoroutineWallCheckService`. Risiko ist asymmetrisch → konservativ/höher ist sicher. Aktiver Modus erkennbar am Console-Log „[AudioTool] UniTask mode was initialized".
