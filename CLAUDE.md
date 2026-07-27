# AudioTool — CLAUDE.md

Erster Anlaufpunkt für jede neue Session: **was das Projekt ist, wie wir zusammenarbeiten, welche Regeln
immer gelten — und wo alles andere steht.**

> **📌 Wissensablage-Regel (verbindlich):**
> Wissen lebt **im Repo**, versioniert und für Patrick lesbar — **nicht** in einem PC-gebundenen
> Claude-Memory. Das Memory bleibt bewusst leer (vermeidet Drift).
> Jede Information hat **genau eine** Heimat; es gibt keine zweite Kopie in einer anderen Datei. Wer etwas
> ergänzt, ergänzt es dort, wo es laut Wegweiser unten hingehört, und verlinkt von anderen Stellen nur hin.

---

## 🧭 Wegweiser — welche Datei für welchen Kontext

**Diese Datei enthält nur, was bei *jeder* Aufgabe gilt.** Die Tiefe zu einem Thema steht in der jeweiligen
Fachdatei. Faustregel: *muss ich es immer wissen → hier. Muss ich es wissen, wenn ich genau dieses Subsystem
anfasse → dort.*

| Wenn es um … geht | dann lies | Inhalt |
|---|---|---|
| **TDD-Loop, Gates, Mutation Check, Test-Regeln** | [`TESTING.md`](TESTING.md) **Teil I** | Die verbindliche Test-Disziplin. **Vor jeder neuen Methode zu lesen.** |
| **Test-Qualität, was die Tests wirklich schützen** | [`TESTING.md`](TESTING.md) **Teil II** | Audit pro Methode + die Lehren daraus (Mess-Snapshot, darf veralten) |
| **Architektur, Services, Datenfluss** | [`ARCHITECTURE.md`](ARCHITECTURE.md) §1–3 | Service-Graph, Tick-Reihenfolge, pure Logik-Schicht, Typen-Tabelle |
| **Warum etwas so gebaut ist** (Pool, Occlusion, Ducking, Fade, Pause, Follow, Singleton, Call-statt-Event) | [`ARCHITECTURE.md`](ARCHITECTURE.md) §4–16 | Alle Designentscheidungen mit Begründung |
| **Lautstärke / `source.volume` / Ducking** | [`ARCHITECTURE.md`](ARCHITECTURE.md) §6–7 | Zwei-Gain-Stufen-Modell, Ein-Besitzer-Regel |
| **Offene Aufgaben, Features, Roadmap (V1 / V1.1)** | [`BACKLOG.md`](BACKLOG.md) | Single Source aller offenen Arbeiten |
| **Ein Code-Review aufsetzen / Befunde ablegen** | [`REVIEW_PROTOCOL.md`](REVIEW_PROTOCOL.md) | Verfahren (portabel) + Befund-Log (projektspezifisch) |
| **Demo-Szene / Verkaufsvideo planen** | [`SHOWCASE_REQUIREMENTS.md`](SHOWCASE_REQUIREMENTS.md) | Showcase-Module, Hero-Schnitt |
| **Was der Käufer sieht** | `Assets/…/Documentation/AudioTool_Documentation_DE.md` / `_EN.md` | User-Handbuch inkl. „Bekannte Einschränkungen" |
| **Verkaufsoberfläche / Portfolio** | `README.md` (EN) · `README.de.md` (DE) | 1:1-Spiegel, bewusst stabil |

---

## Was ist dieses Projekt?

**AudioTool** ist ein Unity Audio-Management-Framework, das als Unity Asset Store Plugin veröffentlicht werden
soll. Zielgruppe sind Indie-Entwickler und kleine Teams, die kein Audio-Budget haben und sich nicht in
FMOD/Wwise einarbeiten wollen. Das Tool nimmt dem Entwickler die vollständige Verwaltung von
`AudioSource`-Objekten ab: ein einziger Aufruf reicht — den Rest erledigt das System.

**Umgebung:** Unity 6, JetBrains Rider. Chat-Sprache Deutsch, Code und Kommentare Englisch.

### Produkt-Strategie & Verkaufsargument (der „Moat")

Das Fundament steht sauber (Pooling, Occlusion, Pause/Follow, Fade, Ducking — alle test-gestützt). Der Plan:
**bewusst die Feature-Breite ausbauen**, um mit der Konkurrenz (MasterAudio, Sonity, FMOD-nah) gleichzuziehen
— aber jedes Feature mit **überlegener Struktur und Testdisziplin**, die die Konkurrenz nicht liefert. Das
Verkaufsargument ist nicht Breite allein, sondern: „dieselben Features wie die großen Tools, aber sauber und
testbar". **Breite darf niemals auf Kosten dieser Qualitätskante gehen.**

Zweites bewusstes Verkaufsargument: Der Wall-Check ist **lightweight occlusion** (simpler Low-Pass), KEIN
voller Spatializer wie Steam Audio/Oculus. Das ist ein Feature, kein Nachteil — klar so kommunizieren.

---

## Zusammenarbeit mit Patrick

> Die universellen Regeln (Solo-Dev, kein Ja-Sager, kein eigenständiges Committen, erst erklären dann Go,
> Sauberkeit vor Workaround, warmer Abschluss, Sprache, Testbarkeit) stehen in der user-level
> `~/.claude/CLAUDE.md` und laden automatisch in jede Session. Hier nur **AudioTool-Spezifisches**:

- **Pro-Plan-Hinweis:** Patrick stößt an Session-Token-Limits (Opus ist der schwere Treiber). Opus für die
  harten Teile sparen; Mechanisches kann günstiger laufen. *(Das ist auch der Grund für den Wegweiser oben:
  diese Datei lädt in **jede** Session — was hier steht, wird jedes Mal bezahlt. Tiefe gehört deshalb in die
  Fachdateien, die nur bei Bedarf geöffnet werden.)*

---

## Arbeitsweise: Test-Driven (nicht verhandelbar)

**Die vollständige Disziplin steht in [`TESTING.md`](TESTING.md) Teil I — vor jeder neuen Methode dort
nachlesen.** Der verbindliche Kern in Kurzform, damit er nie „übersehen" werden kann:

1. **Red first.** Erst der fehlschlagende Test, dann die Implementierung.
2. **Tests sind danach EINGEFROREN.** Nie editiert/abgeschwächt, um Code grün zu bekommen. Neue Tests für
   neues Verhalten sind ok. Wird ein Test durch Umbau obsolet → BACKLOG, **nie** stiller Rewrite.
3. **Mutation Check.** Nach Grün bewusst einen Fehler einbauen und *vorher* vorhersagen, welcher benannte
   Test rot wird. Danach korrekten Zustand wiederherstellen.
4. **Ein Gate pro Antwort — dann STOPP.** Rot → Grün → Mutation → Wiederherstellung sind vier
   **Bestätigungs-Gates**, keine Schrittliste. Der nächste Schritt beginnt erst, wenn **Patrick das von ihm
   selbst beobachtete Ergebnis bestätigt hat**. Stub und Implementierung nie im selben Zug; einen späteren
   Schritt nie vorab ankündigen oder vorbereiten.
5. **Erwartungswerte aus der SPEZIFIKATION, nie aus dem Code.** Lässt sich „korrekt" ohne Code-Lesen nicht
   sagen → STOPP und das Soll mit Patrick klären.
6. **Roter Test ≠ Test anfassen.** Diagnose-Reihenfolge: Implementierung → mein Modell → meine Vorhersage →
   *zuletzt* der Test. Test-Änderung nur mit vorab benannter Kategorie und Patricks Go; Default ist Veto.

**Patricks Kernangst sind tautologische / Change-Detector-Tests.** Red-first + Einfrieren + Mutation Check
sind die Schutzwälle dagegen — deshalb sind sie nicht verhandelbar.

---

## Architektur — Überblick

**Details, Tick-Reihenfolge und alle Designentscheidungen → [`ARCHITECTURE.md`](ARCHITECTURE.md).**

```
AudioManagerDynamic (MonoBehaviour — Singleton, öffentliche API, treibt die LateUpdate-Ticks)
├── AudioPoolAcquisitionService     → Pool aus AudioObject[]; Slot-Vergabe + Generation
├── AudioPlaybackService            → Dispatching, Stop-Einstieg, Handle-Gating
│   └── AudioStopService            → einziger „Slot stoppen"-Pfad
├── AudioUniTaskWallCheckService    → Raycast-Loop (empfohlen)   ┐ setzen nur TargetCutoff
├── AudioCoroutineWallCheckService  → Raycast-Loop (Fallback)    ┘
│   └── SceneAudioListenerProvider  → aktuelle AudioListener-Position (self-heal)
├── AudioOcclusionSmoothingService  → gleitet den Cutoff pro Frame
├── AudioFollowService              → kopiert Emitter-Position pro Frame, ohne Parenting
├── AudioFadeService                → treibt Fades; schreibt den Per-Slot-FadeFactor
├── AudioDuckService                → EINZIGER Besitzer von source.volume (basis · fade · duck)
│   └── AudioDuckComponent          → optionaler, passiver Regel-Provider (kein eigener Tick)
├── AudioPauseService               → Pause/Unpause der Pool-Slots (scope-bewusst)
└── AudioManagerDictionaryProvider  → Volume- & LayerMask-Dictionaries
```

Die gesamte Entscheidungslogik liegt in **puren, Unity-freien Klassen** (EditMode-getestet) — Liste und
Verantwortung in [`ARCHITECTURE.md`](ARCHITECTURE.md) §2. Aktuell **118 EditMode-Tests** über 15 Logik-Einheiten.

---

## Öffentliche API

```csharp
// Abspielen
AudioHandle h = AudioManagerDynamic.PlaySpatial(myADO, sourceTransform);   // 3D, optional wall-checked
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

## Invarianten, die bei JEDER Änderung gelten

Die drei Regeln, deren Verletzung schon einmal einen echten Bug erzeugt hat oder erzeugen würde. Begründung
und Details jeweils in [`ARCHITECTURE.md`](ARCHITECTURE.md).

1. **ADO ist die Control Surface** (§4) — jede vom `AudioDataObject` gespiegelte Eigenschaft MUSS bei
   **jedem** Dispatch geschrieben werden, **unbedingt, nie in einem `if`**. Sonst trägt ein wiederverwendeter
   Slot den Wert des Vorgängers. *(So entstand der `spatialBlend`-Bug — die einzige real passierte
   Bug-Klasse.)* Neues gespiegeltes Feld → unbedingte Zeile in `AudioPlaybackService.Dispatch` mitsetzen.
2. **`source.volume` hat genau einen Besitzer** (§6) — `AudioDuckService`. Wer eine neue
   Lautstärke-Beeinflussung baut, macht daraus einen **weiteren Faktor** (wie Fade und Duck) und schreibt
   **nie** selbst auf `source.volume`.
3. **Pausieren heißt `PauseAll()`, nicht `timeScale = 0`** (§11) — das gesamte Tool läuft auf der
   ungeskalierten Uhr (`Time.unscaledTime` / `Time.unscaledDeltaTime`). Neue zeitabhängige Logik hängt an
   derselben Uhr, sonst hängen Slots bei Slow-Mo.

---

## Doku-Regeln

- **User-Doku beschreibt nur den aktuellen Zustand** — keine „nicht mehr / früher / jetzt geändert"-Formulierungen.
  Das Tool ist unveröffentlicht; es gibt keine Vorversion zum Vergleich.
  (`AudioTool_Documentation_DE.md` / `_EN.md` — EN spiegelt DE **1:1**.)
- **README ist zweisprachig und 1:1 gespiegelt:** `README.md` (EN, GitHub-Default) und `README.de.md` (DE) —
  bei Änderungen IMMER beide identisch pflegen. Stil ist portfolio-/feature-orientiert und bewusst stabil;
  Änderungen sind **chirurgisch** (nur was sich fachlich geändert hat), kein Rewrite.
- **Bekannte Einschränkungen** gehören in die dedizierte Sektion am **Ende der User-Doku** (DE/EN 1:1) —
  **nicht** ins README (das bleibt Verkaufsoberfläche). Diese Liste ist die **Single Source** für Caveats.
  Am Fundort (z. B. Wall-Check-Kapitel) höchstens ein **Einzeiler-Verweis** dorthin, kein Duplikat.
- **Neue Aufgaben/TODOs gehören in [`BACKLOG.md`](BACKLOG.md)** — nie in diese Datei, nie ins Memory.

---

## Was NICHT angefasst werden soll ohne Rücksprache

- **`TestScript.cs`** (`Assets/Scripts/`) — nur zum Testen, kein Produktionscode.
- **`AudioCoroutineWallCheckService`** — Fallback. Nur **parallel** zur UniTask-Version anpassen (beide
  synchron halten). Bei aktivem UniTask kompiliert er nicht mit → Spiegelungen werden gesammelt gegengeprüft
  (siehe BACKLOG „Coroutine-Variante gebündelt gegenprüfen").
- **Eingefrorene Tests** — siehe TDD-Regel 2 oben.
