# REVIEW_PROTOCOL.md — Review-Verfahren & Befund-Protokoll

> **Was diese Datei ist:** beides zugleich — das **Verfahren**, wie in diesem Projekt ein Code-Review
> aufgesetzt wird (Teil I), *und* das **Protokoll** der tatsächlich durchgeführten Reviews und ihrer
> Befunde (Teil II). „Protokoll" im doppelten Wortsinn: Vorschrift *und* Aufzeichnung.
>
> **Portabilität (wichtig):**
> - **Teil I ist projekt-agnostisch** und für copy/paste in andere Projekte gedacht — er enthält bewusst
>   keine projektspezifischen Datei- oder Klassennamen.
> - **Teil II ist projekt-spezifisch.** Wer diese Datei in ein neues Projekt kopiert, **behält Teil I**
>   und **leert Teil II** (Achsen-Zuschnitt neu, Befund-Log frisch).
>
> **Verhältnis zum Rest:** Wissen → [`CLAUDE.md`](CLAUDE.md) · Aufgaben → [`BACKLOG.md`](BACKLOG.md) ·
> Review-Verfahren & -Befunde → **diese Datei**. Wird ein Befund zu echter Nacharbeit, wandert die
> **Aufgabe** in `BACKLOG.md` (nie hier ausführen); wird er zu durabler Erkenntnis, wandert das **Wissen**
> in `CLAUDE.md`. Diese Datei hält das *Review selbst* fest, nicht dessen Folgearbeit.

---

# TEIL I — Das Verfahren (portabel, mitkopieren)

## 1. Grundprinzip

- **Recall sinkt mit Scope.** Ein Reviewer — Mensch *oder* Agent — findet pro Datei *weniger* echte
  Bugs, je größer sein Auftrag ist. „Review einmal alles" ist deshalb der schlechteste Modus, **nicht**
  wegen Kosten, sondern wegen Aufmerksamkeits-Verdünnung. Deshalb wird aufgeteilt.
- **Nach Fehlerklasse aufteilen, nicht nur nach Datei.** Die teuersten Bugs leben *zwischen* den Modulen
  (Lebenszeit, geteilte Invarianten). Ein reines Pro-Datei-Review sieht die strukturell nicht.
- **Zwei Blickwinkel auf dieselbe Zeile sind erwünscht.** Wenn ein Pro-System-Pass und ein
  Quer-Lens denselben Code aus verschiedenen Gründen flaggen: **beides protokollieren**, nicht vorzeitig
  deduplizieren. Dedup passiert erst in der Synthese (Achse 3).
- **Erwartungswert kommt aus der Spezifikation, nicht aus dem Code.** Der Reviewer leitet „korrekt" aus
  dem Vertrag (CLAUDE.md / Doku) ab, *bevor* er die Implementierung liest — sonst rationalisiert er den
  Code weg. (Spiegelt die TDD-Grundregel des Projekts.)

## 2. Das Achsensystem

Vier Achsen, die **verschiedene Fehlerklassen** abdecken. Nicht jede Achse muss bei jedem Durchlauf
laufen — aber bewusst entscheiden, welche man weglässt.

### Achse 0 — Architektur (1 Pass, ganzes Projekt, bewusst flach)
- **Frage:** Halten die Seams? Ist die Schichtung sauber (z. B. pure/framework-freie Logik vs.
  Framework-Kleber)? Gibt es Special-Cases, die man generalisieren sollte (*Altitude*)? Liegt etwas in
  der falschen Schicht?
- **Kein** Zeilen-für-Zeilen. Output ist eine **Risiko-Landkarte** (Hotspots), die Achse 1 & 2 füttert.
- Darf der günstigste Pass sein (Überblick statt Tiefe).

### Achse 1 — Pro-System, tief (1 Agent je System, parallel)
- Jeder Agent besitzt **genau ein** System und liest es **Zeilen-für-Zeilen**.
- **Pflicht: geschriebene Charta** — welche Invarianten das System laut Spec halten muss, plus das
  Mandat, jeden Eingang/Zustand/Timing-Pfad durchzuspielen.
- Schnitt entlang der natürlichen Modulgrenzen der Architektur.

### Achse 2 — Quer-schneidende Lenses (1 Agent je Lens) ⭐ meist der höchste Ertrag
- Schneiden **durch** alle Systeme und fangen, was Pro-System-Reviews strukturell verpassen.
  Generisch wertvolle Lenses (projektunabhängig):
  - **Lifetime & Concurrency:** jede Ressource mit Lebenszyklus (Tokens, Handles, Tasks/Threads,
    zerstörbare Referenzen). Leakt / doppel-freed / agiert etwas auf einer toten Referenz / Race?
  - **Invarianten-Vollständigkeit:** eine Regel, die an *jeder* Stelle gelten muss („immer X schreiben",
    „nie ohne Y") — die Stellen aufzählen und *jede* gegen die Regel prüfen.
  - **Test-Honesty:** sind die Tests tautologisch / Change-Detector / zu schwach in Assertion oder
    Toleranz? Schützt ein grüner Test wirklich Verhalten?
- Welche Lenses für *dieses* Projekt am meisten zählen → Teil II.

### Achse 3 — Synthese (1 Pass)
- **Dedupe** über alle Achsen (gleicher Ort + gleicher Mechanismus → einen behalten, den mit dem
  konkretesten Auslöser).
- **Ranking** nach Schwere; Korrektheits-Bugs schlagen immer Struktur/Cleanup.
- **Getrennte Ausgabe:** *Bugs* und *strukturelle Empfehlungen* nie vermischen.

## 3. Durchführungs-Regeln (die „Einstellungen")

- **Modell:** das stärkste verfügbare für Achse 1 & 2 (Tiefe vor Kosten, wenn Qualität das Ziel ist).
  Achse 0 darf ein günstigerer Überblicks-Pass sein.
- **Agent-Typ:** ein Allzweck-Agent mit Lese-/Such-/VCS-Zugriff (Read + Grep + git-Diff). **Kein** reiner
  Explore-/Auszugs-Agent für die Tiefe — der überfliegt.
- **Größter Qualitätshebel: die geschriebene Charta.** „Finde Bugs in X" ist schwach. „Halte *diese*
  Verträge in X, hier ist der Kontrakt, geh Zeile für Zeile" ist um Klassen besser. Charta-Template → §4.
- **Spec füttern.** Dem Agent die relevanten Vertrags-Auszüge mitgeben, damit er den Erwartungswert aus
  der Spec ableitet, nicht aus dem Code.
- **Recall-Framing + Verify-Pass.** Findern sagen „im Zweifel surfacen". Danach ein separater
  Verify-Pass, der jeden Kandidaten auf CONFIRMED / PLAUSIBLE / REFUTED prüft (REFUTED fällt raus) —
  fängt Halluzinationen, ohne Recall zu opfern.
- **Parallel, wo unabhängig.** Achse-1-Systeme und Achse-2-Lenses sind voneinander unabhängig → parallel
  starten.

## 4. Charta-Template (copy/paste pro Agent)

```
Du reviewst <SYSTEM oder LENS> im Recall-Modus (im Zweifel surfacen).

Vertrag (aus der Spezifikation, NICHT aus dem Code abgeleitet):
- <Invariante 1>
- <Invariante 2>
- …

Auftrag:
- Lies <Dateien/Bereich> Zeile für Zeile; bei Hunks auch die umschließende Funktion.
- Für jede Zeile: welcher Input / Zustand / Timing / welche Plattform macht sie falsch?
- Prüfe jede Invariante oben einzeln an JEDER Stelle, an der sie gelten muss.
- Erwartungswerte aus dem Vertrag herleiten, bevor du die Implementierung liest.

Gib bis zu <N> Befunde im Format aus §5 zurück. Keine Stilmeinungen ohne konkreten Auslöser.
```

## 5. Schweregrade & Befund-Format

**Schweregrade:**
- `[Blocker]` — falsches Verhalten / Crash / Datenverlust auf einem realen Pfad.
- `[Major]` — Bug mit eingeschränktem Auslöser (Timing/Config/Plattform) oder schwerer Korrektheitsriss.
- `[Minor]` — kleiner Korrektheits-/Robustheitsmangel, selten oder gut abgefedert.
- `[Struktur]` — kein Bug: Altitude/Reuse/Simplification/Effizienz/Konvention.
- `[Frage]` — Unsicherheit, die eine Entscheidung braucht (kein Befund, sondern offener Punkt).

**Befund-Format** (ein Block pro Befund, im Log in Teil II):

```
#### <ID> · <Kurztitel>
- **Achse/Lens:** <0 | 1:System | 2:Lens | 3>
- **Ort:** <datei:zeile>
- **Schwere:** <[Blocker] | [Major] | [Minor] | [Struktur] | [Frage]>
- **Befund:** <ein Satz: was ist falsch>
- **Auslöser:** <konkrete Inputs/Zustand → falsche Ausgabe/Crash>
- **Verify:** <CONFIRMED | PLAUSIBLE | REFUTED + ein Satz Begründung>
- **Status:** <offen | behoben @<commit> | akzeptiert | verworfen | → BACKLOG | → CLAUDE.md>
```

ID-Schema: `R<lauf>-<nr>`, z. B. `R1-03` = dritter Befund des ersten Durchlaufs.

---

# TEIL II — Projekt-spezifisch (beim Kopieren in ein neues Projekt LEEREN)

## II.a — Achsen-Zuschnitt für *dieses* Projekt (AudioTool)

> Konkrete Instanziierung der Achsen aus Teil I auf die Architektur in `CLAUDE.md`.

**Achse 0 — Architektur:** Service-Graph aus `CLAUDE.md` gegen den Code; Fokus auf die Sauberkeit der
pure/Unity-freien Grenze (`AudioFadeMath`, `WallOcclusionMath`, `OcclusionSmoothing`,
`LowPassDispatchPolicy`, `AudioHandleValidator`, `WallLayerMask`, `ListenerCachePolicy`) und auf
Special-Cases, die generalisiert gehören.

**Achse 1 — Pro-System (Schnitt):**
1. **Pool + Generation/Handle** — `AudioPoolAcquisitionService`, `AudioObject`, `AudioHandle`,
   `AudioHandleValidator`.
2. **Playback + Stop + Dispatch** — `AudioPlaybackService`, `AudioStopService`, `LowPassDispatchPolicy`.
3. **WallCheck + Listener + Occlusion** — `AudioUniTaskWallCheckService`,
   `AudioCoroutineWallCheckService` (synchron halten!), `SceneAudioListenerProvider`,
   `ListenerCachePolicy`, `WallOcclusionMath`, `WallLayerMask`, `AudioOcclusionSmoothingService`.
4. **Fade-Familie** — `AudioFadeService`, `FadeOperation`, `AudioFadeMath`, `PooledFadeTarget`.
5. **Pause + Follow** — `AudioPauseService`, `AudioFollowService`.

**Achse 2 — Lenses (für AudioTool am wertvollsten):**
- **Lifetime & Concurrency:** `CancellationTokenSource[]` in den WallCheck-Services; Singleton-Teardown
  in `AudioManagerDynamic` (`Awake`/`OnDestroy`); Slot-Reuse + `Generation`; `IsPaused`-Übergänge;
  Follow-Target-Tod → Stop + Slot-Freigabe.
- **Control-Surface-Vollständigkeit:** jede vom ADO gespiegelte Eigenschaft (`CurrentClips`,
  `CurrentType`, `SpatialBlend`, `FollowEmitter`, `IsOneShot`, `CanHandleAudioSource`, `UseWallCheck`,
  `RespectsGlobalPause`, …) → beweisen, dass sie in `AudioPlaybackService.Dispatch` **unbedingt** (nie im
  `if`) geschrieben wird. (Zielt direkt auf die einzige real passierte Bug-Klasse — `spatialBlend`.)
- **Test-Honesty:** alle EditMode-Tests unter `Assets/Scripts/AudioFramework/Tests/EditMode/` auf
  Tautologie / Change-Detector / zu schwache Assertion oder Toleranz prüfen.

**Bewusste Priorisierung:** Die Architektur ist stark dokumentiert → Achse 0 bringt vermutlich wenig
Neues. Der höchste Ertrag liegt in **Achse 2 (Test-Honesty + Control-Surface)** — dort sitzen die
erklärte Kernangst und die einzige je passierte Bug-Klasse. Wenn nur eine Scheibe läuft: diese.

**Projekt-Sonderregeln fürs Review:**
- `TestScript.cs` ist kein Produktionscode → nicht als Befund-Quelle behandeln.
- `AudioCoroutineWallCheckService` ist Fallback und muss mit der UniTask-Version synchron sein →
  Divergenz zwischen beiden ist selbst ein gültiger Befund.

## II.b — Befund-Log

> Chronologisch, neueste oben. Format siehe Teil I §5. Noch keine Durchläufe.

_(leer — erster Durchlauf steht aus)_

## II.c — Durchlauf-Historie

| Lauf | Datum | Achsen | Scope | Befunde (Blocker/Major/Minor/Struktur) | Notiz |
|---|---|---|---|---|---|
| — | — | — | — | — | noch kein Durchlauf |
