# AudioTool — Backlog: Code-Review IST-Zustand

> **Erstellt:** 2026-06-04 aus der gesamtheitlichen Projektanalyse.
> **Scope:** Momentaufnahme des IST-Zustands. KEINE geplanten Features — die stehen in `CLAUDE.md` unter „Offene Punkte / Geplante Features".
> **Reihenfolge = Priorität.** Zeilennummern können driften; im Zweifel über das genannte Symbol/den Methodennamen suchen.
>
> **TDD-Regel (nicht verhandelbar):** Jeder Fix wird test-first gebaut — erst rot sehen, dann grün. Pro Punkt steht ein Test-Hinweis. Tests sind nach dem Schreiben eingefroren.
>
> **Aufwand-Legende:** S = klein (Einzeiler bis ~1h) · M = mittel (Feature/Refactor + Tests) · L = groß/laufend.

---

## 🔴 Kritisch

### K1 — `OnDestroy` nullt den lebenden Singleton bei Duplikat-Zerstörung
- [ ] **erledigt**
- **Ort:** `AudioManagerDynamic.OnDestroy()` (~Z. 230), Bezug zur Singleton-Logik in `Awake()` (~Z. 35).
- **Problem:** `OnDestroy` setzt `instance = null` **bedingungslos**. Die `Awake`-Singleton-Logik zerstört korrekt ein zweites Manager-GameObject — aber dessen `OnDestroy` läuft am Frame-Ende und nullt die statische Referenz auf die **echte, noch lebende** Instanz. Folge: ab Frame 2 läuft jedes `PlaySpatial`/`FadeIn`/`PauseAll` in den `instance == null`-Zweig → Manager statisch „tot". **Realistischer Trigger: additives Szenenladen** mit einem Manager in zwei Szenen.
- **Fix:** Erste Zeile in `OnDestroy`: `if (instance != this) return;` — nur die echte Instanz räumt auf und nullt `instance`.
- **Test:** PlayMode (zwei Manager instanziieren, 1 Frame warten, prüfen dass `PlaySpatial` weiterhin validen Handle liefert). EditMode schwierig wegen MonoBehaviour-Lifecycle → ggf. die „bin ich die echte Instanz?"-Entscheidung in eine pure, testbare Hilfsmethode ziehen. Passt gut zu M3 (PlayMode-Assembly ist leer).
- **Aufwand:** S (Fix) · M (PlayMode-Test-Setup)

---

## 🟠 Wichtig (vor Release)

### W1 — Stale Handles: keine Generation → fremder Sound wird gestoppt/gefadet
- [ ] **erledigt**
- **Ort:** `AudioHandle` (gesamter Typ); Zugriffe in `AudioPlaybackService.StopAudio` (~Z. 192) und den Fade-Pfaden in `AudioManagerDynamic` (`FadeOut`, `Crossfade*`).
- **Problem:** `AudioHandle` enthält nur `PoolIndex`, keine Versionierung. Nach natürlicher Slot-Freigabe (OneShot endet) + Neuvergabe stoppt/fadet ein alter Handle den **neuen, fremden** Sound auf demselben Slot. Verschärft: Die Fade-API gibt **immer** einen Handle zurück (auch ohne `CanHandleAudioSource`) → größere Angriffsfläche für veraltete Handles.
- **Fix:** Generation/Version pro Slot. `AudioObject` bekommt einen `Generation`-Counter, der bei jeder Neuvergabe in `Dispatch` inkrementiert wird. `AudioHandle = { index, generation }`. Vor jedem Stop/Fade Generation gegen den Slot prüfen; bei Mismatch → no-op (still). `IsValid` entsprechend erweitern.
- **Test:** Gut test-first-fähig. Generation-Vergabe + Validierung als pure Pool-Logik in EditMode testbar (Slot neu vergeben → alter Handle ungültig → Stop ist no-op). Roter Test zuerst: „alter Handle stoppt neuen Sound nicht".
- **Aufwand:** M

### W2 — LowPass-Filter dauerhaft aktiv @ 5000 Hz auf ALLEN Sounds
- [ ] **erledigt**
- **Ort:** `3DAudioSourceObject.prefab` (`AudioLowPassFilter m_Enabled: 1`, `BypassEffects: 0`); `AudioPlaybackService.Dispatch` (~Z. 141, `filter.cutoffFrequency = defaultCutoffValue`); `AudioSystemConfig.DefaultCutoffFreqValue = 5000` (~Z. 21).
- **Problem:** Der LowPass ist immer aktiv und wird bei jedem Dispatch auf 5000 Hz gesetzt. Für wall-checked Sounds danach dynamisch geregelt — korrekt. Für **alle anderen** (2D-Musik, UI, nicht-occludierte SFX) bleibt er bei 5000 Hz. Das ist klanglich **nicht** transparent (dämpft Brillanz 8–16 kHz hörbar; Musik klingt dumpf). Tooltip („usually between 5000-5007") suggeriert Missverständnis — neutral wäre ~22000 Hz.
- **Fix:** (a) Default-Cutoff auf transparenten Wert (~22000) + Tooltip korrigieren. (b) Control-Surface-konform: `filter.enabled = ado.UseWallCheck` bei **jedem** Dispatch setzen — non-wallcheck-Sounds umgehen den Filter komplett (transparenter Klang + weniger DSP-Last).
- **Test:** Die Dispatch-Entscheidung (`UseWallCheck → filter.enabled`) als Verhalten testbar, wenn isoliert. Klangqualität selbst = manueller PlayMode-Hörtest.
- **Aufwand:** S–M

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

### M2 — Testabdeckung endet bei der Fade-Familie
- [ ] **erledigt**
- **Ort:** `Assets/Scripts/AudioFramework/Tests/EditMode`.
- **Problem:** Getestet sind nur `AudioFadeMath`, `FadeOperation`, `AudioFadeService`. Ungetestet: Pool-Acquisition (inkl. `IsPaused`-als-belegt-Logik), Pause-Logik, `AudioManagerDictionaryProvider` (Duplikat-/Null-Handling), Cutoff-Minuenden-Mathematik.
- **Fix:** = deine geplante „Bestandscode nachtesten"-Aufgabe. Cutoff-Minuenden als pure Funktion herausziehen (analog `AudioFadeMath`) → ohne Physics testbar. `AudioManagerDictionaryProvider` ist bereits ohne Unity-Lifecycle testbar.
- **Test:** ist selbst die Aufgabe (test-first für jede extrahierte Logik).
- **Aufwand:** L (laufend)

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

### P6 — Fehlende Bounds-Checks / public `AudioHandle`-Konstruktor
- [ ] **erledigt** — `FadeOut`/`StartFadeOut` prüfen nur `PoolIndex >= 0`, nicht die Obergrenze; `AudioHandle(int)` ist public → manuell gebauter Handle `{99999}` crasht. Defensive Maßnahme (überschneidet sich mit W1). Aufwand: S

### P7 — Fehlende Tooltips für `IsOneShot`, `CanHandleAudioSource`, `UseWallCheck`
- [ ] **erledigt** — `AudioDataObject.cs` (~Z. 41–43). Steht schon auf der Release-Liste; hier bestätigt: weiterhin ohne Tooltip. Aufwand: S

### P8 — `CLAUDE.md` ist gegenüber dem Code veraltet
- [ ] **erledigt** — Architektur-Tabelle nennt `AudioTypeProvider.cs` (→ `AudioCategory.cs`) und API `Play(myADO)`/`Stop(handle)` (real: `PlaySpatial`/`PlayNonSpatial`/Fade-Familie). Onboarding-Doku führt in die Irre. **Hinweis:** CLAUDE.md ist Patricks Dokument — Änderung nur nach Gegenlesen. Aufwand: S

---

## Bereits bekannt (Referenz — teils schon in CLAUDE.md „Offene Punkte")

- **CTS-Allokation pro `Play()`** mit WallCheck (`AudioUniTaskWallCheckService.StartWallCheckLoop`, `CreateLinkedTokenSource`) → „Zero GC" stimmt nicht ganz. CTS-Reuse / `TryReset`.
- **Null-Einträge in `CurrentClips`** → `Random.Range` kann null wählen → `source.clip = null` → stiller Sound. Inspector-Validierung oder Skip/Warn beim Pick.
- **`IGetPoolIndex`** ist toter Platzhalter (Lightweight-Pool nicht mehr geplant) → entfernen.
