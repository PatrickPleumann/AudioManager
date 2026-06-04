# AudioTool — Backlog

> **Single Source aller offenen Arbeiten.** Zwei Teile: **Teil A** = Release-Härtung aus dem Code-Review (IST-Zustand, 2026-06-04). **Teil B** = geplante Features & Roadmap (aus CLAUDE.md zusammengeführt). Wissen, Architektur und Designentscheidungen leben in [`CLAUDE.md`](CLAUDE.md).
> **Reihenfolge = Priorität** (innerhalb der Abschnitte). Zeilennummern driften; im Zweifel über Symbol/Methodenname suchen. Checkboxen beim Abarbeiten pflegen.
>
> **TDD-Regel (nicht verhandelbar):** Jedes neue Feature/jeder Fix wird test-first gebaut — erst rot sehen, dann grün, dann Mutation Check. Tests sind nach dem Schreiben eingefroren (Details in CLAUDE.md). Tests sind Teil jedes Features, kein separater Punkt.
>
> **Aufwand-Legende:** S = klein (Einzeiler bis ~1h) · M = mittel (Feature/Refactor + Tests) · L = groß/laufend.

---

# Teil A — Release-Härtung (Code-Review IST)

## ✅ Erledigt (2026-06-04)
- **K1** — Singleton-`OnDestroy` nullt nicht mehr die lebende Instanz (`if (instance != this) return;`). *(Unit-Test folgt mit dem AudioManagerDynamic-Sammeltest + M3.)*
- **W2** — LowPass-Default 5000→22000 (transparent); `filter.enabled = UseWallCheck` pro Dispatch (`LowPassDispatchPolicy`, 4 Tests). Occlusion-Mathe in pure `WallOcclusionMath` extrahiert (4 Tests), beide WallCheck-Services teilen den Seam. Layer-Reduktionen neu getunt (10000/14000/18000).
- **Occlusion-Glättung (aus W2-Diskussion abgespalten)** — WallCheck setzt nur noch `TargetCutoff`, neuer `AudioOcclusionSmoothingService` gleitet pro Frame (`OcclusionSmoothing`, 6 Tests); `OcclusionSmoothingSpeed` als Config-Feld. Kein „Pop" mehr beim Aus-der-Wand-Treten.
- **W1 + P6** — `AudioHandle` trägt jetzt `Generation`; Stop/Fade prüfen `IsHandleCurrent` (Bounds + Generation, `AudioHandleValidator`, 6 Tests) → stale Handle = stilles No-op. `AudioHandle`-Ctor `internal` + `AudioHandle.Invalid` → P6 (selbstgebauter `{99999}`-Crash) strukturell zu.
- **P8** — CLAUDE.md auf IST-Zustand gebracht (API `PlaySpatial`/`PlayNonSpatial`/Fade-Familie, `AudioCategory`, kein `CallerTransform` mehr, neue Services) + Memory-Wissen eingearbeitet.

---

## 🟠 Wichtig (vor Release)

### W3 — AudioListener-Transform wird nur einmalig gecacht
- [ ] **erledigt**
- **Ort:** `AudioManagerDynamic.Awake` (~Z. 48, `FindFirstObjectByType<AudioListener>()`); genutzt in `AudioUniTaskWallCheckService.CalculateCutoffFrequency` (~Z. 113) bzw. der Coroutine-Variante.
- **Problem:** `playerListener` wird einmal aufgelöst und lebenslang gehalten. Wechselt der aktive AudioListener zur Laufzeit (Kamerawechsel, Respawn, Vehicle-Cam), zeigt der WallCheck auf die alte/zerstörte Transform. Der `== false`-Guard fängt nur Zerstörung ab (dann kein WallCheck), nicht den Wechsel (falscher Bezugspunkt → stiller Occlusion-Fehler).
- **Fix:** Listener nicht cachen, sondern aktuell beziehen (z. B. periodischer Refresh oder Auflösung hinter kleiner Abstraktion). Abwägung Performance vs. Korrektheit dokumentieren.
- **Test:** WallCheck-Mathe hängt an Physics (schwer testbar). Die Listener-Auflösung hinter eine Abstraktion ziehen → diese ist testbar (Wechsel → neue Referenz).
- **Aufwand:** M

---

## 🟡 Mittel

### M1 — Fades nutzen scaled `Time.deltaTime` → Konflikt mit `timeScale = 0`-Pause
- [ ] **erledigt**
- **Ort:** `AudioManagerDynamic.LateUpdate` (~Z. 99, `fadeService.Tick(Time.deltaTime)`).
- **Problem:** Bei der üblichen `Time.timeScale = 0`-Pause ist `deltaTime` 0 → **jeder Fade friert ein**, auch für Sounds mit `RespectsGlobalPause = false` (z. B. Menü-Musik beim Pausenmenü einfaden). Widerspruch zum sonst durchdachten Pause-System.
- **Fix:** `Time.unscaledDeltaTime` im Tick. Global-pausierte Fades fängt der `IsPaused`-Check im `Tick` bereits ab. **Designentscheidung nötig:** Was soll bei `timeScale=0` + globaler Pause genau passieren?
- **Test:** `AudioFadeService` ist gut getestet; die Service-Logik ändert sich nicht. Test eher auf Manager-/Integrationsebene (welche Zeitquelle wird durchgereicht).
- **Aufwand:** S (Code) — Designentscheidung davor.

### M3 — Leeres PlayMode-Test-Assembly
- [ ] **erledigt**
- **Ort:** `Tests/PlayMode/AudioFramework.Tests.PlayMode.asmdef` (nur asmdef + meta, keine Tests).
- **Problem:** Assembly ohne Inhalt — verwirrend, Ballast im Asset-Store-Bundle. Commit-Historie (`4853eae`) erwähnt PlayMode-Tests, die nicht eingecheckt sind.
- **Fix:** Entweder PlayMode-Tests hinzufügen (ideal zusammen mit K1) oder Assembly + Ordner entfernen.
- **Aufwand:** S

---

## ⚪ Niedrig / Politur & Wartbarkeit

### P1 — `AudioCategory.BehindWall` ist ein Zustand, keine Lautstärke-Kategorie
- [ ] **erledigt** — `AudioCategory.cs` (~Z. 9). Beispiel-Satz mischt 4 echte Kategorien mit einem Occlusion-Zustand. Vor Release bereinigen. Aufwand: S

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
- [ ] **Null-Einträge in `CurrentClips` validieren** — `Random.Range` kann `null` wählen → `source.clip = null` → stiller Sound. Inspector-Validierung oder Skip/Warn beim Pick.
- [ ] **PDF-Dokumentation** — `.md`-Dateien existieren (DE/EN), Konvertierung zu PDF offen.
- [ ] **Ordnerstruktur für Asset Store** — noch nicht definiert.
- [ ] **PlayMode-Smoke-Tests für die Fade-Glue** — fadet `source.volume` wirklich über Frames, gibt FadeOut den Slot frei, friert echtes `PauseAll` einen Fade ein. Locked die aktuell nur manuell verifizierte Verdrahtung. Gutes Bündel mit M3 + K1-PlayMode-Test.

## Weitere geplante Features (Priorität offen — können vor 1.0 rein)

- [ ] **Layer-basierte reaktive Geräusche** — Sounds je nach getroffenem Layer unterschiedlich (Footstep auf Holz vs. Metall vs. Boden). Mapping Layer/Material → AudioClip(-Gruppe), analog zum Layer→Cutoff-Dictionary.
- [ ] **Logarithmischer Frequenzabfall bei Occlusion** — Cutoff-Reduktion pro Wand ist aktuell **linear** (fixe Hz-Subtraktion). Wahrnehmung ist logarithmisch: von ~22000 Hz tut ein fixer Abzug fast nichts, unten viel. Multiplikatives Modell (z. B. „jede Wand halbiert den Cutoff" / Reduktion als Faktor) skaliert wahrnehmungsgerecht + macht Layer-Werte baseline-unabhängig. **Strukturell vorbereitet** über `WallOcclusionMath.ApplyWall` (Einzelstelle). Vorab klären: Layer-Werte bleiben „Hz" oder werden „Faktor"?
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
- [ ] **Bestandscode nachtesten (= M2)** — bestehende Methoden sukzessive mit Unit Tests abdecken; ggf. kleine Refactorings für Testbarkeit (pure Logik aus Unity-Abhängigkeiten ziehen, analog `AudioFadeMath`). Kandidaten: Pool-Acquisition (inkl. `IsPaused`-als-belegt), Pause-Logik, `AudioManagerDictionaryProvider` (Duplikat-/Null-Handling). Test-first für jede extrahierte Logik. Aufwand: L (laufend).
- [ ] **Pricing-Analyse** (wenn release-reif) — Asset-Store-Preise der Konkurrenz (MasterAudio & Co.) live benchmarken (WebSearch für aktuelle Zahlen), dann wertbasiert auf die Differenzierer ankern (saubere Architektur + Tests + lightweight Occlusion + Ease-of-use vs. FMOD). Finaler Preis ist Patricks Markt-Call; ich liefere Benchmark + Begründung, kein Verdikt.
