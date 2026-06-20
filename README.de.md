# AudioTool

[English](README.md) · 📖 **Deutsch**

**Ein allokationsfreies Audio-Management-Framework für Unity 6 — gebaut, um saubere Architektur, strikte Testdisziplin und disziplinierte KI-gestützte Entwicklung zu demonstrieren.**

> **Was dieses Repo demonstriert:** SOLID-Service-Design in C# · testgetriebene Entwicklung mit einem Frozen-Test-Vertrag · Zero-GC-Laufzeitmuster · und das Steuern eines KI-Coding-Agents gegen eine schriftliche Spezifikation und eine eingefrorene Test-Suite.

---

## Warum dieses Repo existiert

Dies ist ein Portfolio-Projekt. Es ist ein funktionierendes Unity-Audio-Framework — gepooltes `AudioSource`-Management, leichtgewichtige Occlusion, Fades, Pause, Follow — aber der Grund, warum es öffentlich ist, ist zu zeigen, **wie ich baue**: die Architekturentscheidungen, die Testdisziplin dahinter und wie ich KI als Power-Tool nutze, ohne die Code-Qualität erodieren zu lassen.

Wenn du nur einen Abschnitt liest, lies [Engineering-Disziplin](#engineering-disziplin) — dort stecken die eigentlichen Entscheidungen.

---

## Architektur auf einen Blick

Ein einziger `MonoBehaviour`-Singleton stellt eine statische API bereit und treibt die Per-Frame-Ticks. Alles andere ist ein **reiner C#-Service** mit genau einer Verantwortung — im MonoBehaviour versteckt sich keine Logik.

```
AudioManagerDynamic            MonoBehaviour-Singleton · öffentliche API · LateUpdate-Treiber
├── AudioPoolAcquisitionService   festes AudioObject[]-Pool · Slot-Vergabe + Generation
├── AudioPlaybackService          Dispatch (Play / Fade-In-silent) · Volume-Resolve · Handle-Gating
│   └── AudioStopService          der einzige „Slot stoppen"-Pfad (Source-Stop + Reset + WallCheck-Stop)
├── AudioUniTaskWallCheckService  Raycast-Loop via UniTask (empfohlen)    ┐ beide schreiben nur
├── AudioCoroutineWallCheckService Raycast-Loop via Coroutine (Fallback)  ┘ TargetCutoff
├── AudioOcclusionSmoothingService gleitet den Filter-Cutoff Richtung TargetCutoff (pro Frame)
├── AudioFollowService            kopiert die Emitter-Position pro Frame — kein Re-Parenting
├── AudioFadeService              treibt alle Fades pro Frame via IFadeTarget[]
├── AudioPauseService             scope-bewusste globale Pause / Unpause
└── AudioManagerDictionaryProvider  Volume- + Layer-Mask-Dictionaries
```

Die beiden WallCheck-Services liegen hinter einem Interface (`IAudioWallCheckService`) — ein **Strategy**-Seam, sodass das Async-Backend austauschbar ist und der Manager nie einen `CancellationToken` anfasst.

---

## Engineering-Disziplin

### Testgetrieben, mit einem Frozen-Test-Vertrag

Jede neue Methode folgt einem festen Loop, und die Regel, die ihn ehrlich macht, ist **Schritt 2**:

1. Schreibe **zuerst den fehlschlagenden Test** — er muss rot sein, bevor die Implementierung existiert.
2. **Dieser Test ist danach eingefroren — wird nie editiert, damit Code grün wird.** Neue Tests für neues Verhalten sind in Ordnung; einen bestehenden Test abzuschwächen, um Grün zu bekommen, nicht.
3. Schreibe die Methode, die die eingefrorenen Tests grün macht.
4. **Mutation Check:** sobald grün, baue bewusst einen Fehler ein und sage vorher, *welcher* Test rot wird. Beweist, dass die Tests wirklich etwas schützen — keine tautologischen Change-Detector-Tests.

Erwartungswerte werden **aus der Spezifikation hand-abgeleitet, nicht aus der Implementierung abgelesen.** Die Occlusion-Testdatei sagt es laut: *„Wenn die Implementierung diesen widerspricht, ist sie falsch."*

### Reine Logik, aus Unity herausgezogen, damit sie ehrlich testbar ist

Die Mathematik- und Policy-Entscheidungen leben in kleinen **Unity-freien** Klassen, im EditMode ohne laufende Engine unit-getestet:

| Klasse | Verantwortung |
|---|---|
| `AudioFadeMath` | Fade-Kurve / Lautstärke über Zeit |
| `WallOcclusionMath` | multiplikativer Dämpfungsschritt pro Wand (Faktor → Cutoff) + Floor-Clamp (der austauschbare Occlusion-Modell-Seam) |
| `OcclusionSmoothing` | Per-Frame-Glide Richtung Ziel-Cutoff |
| `LowPassDispatchPolicy` | Filter-An/Aus-Zustand pro Dispatch |
| `AudioHandleValidator` | Handle-Aktualität: Bounds + Generation |
| `ListenerCachePolicy` | wann der aktive AudioListener neu aufzulösen ist (self-healing, kein Polling) |

Das ist ein bewusster Trade-off zugunsten **ehrlicher Tests**: Wo die Wahl zwischen einem testbaren Seam (ein Interface / eine reine Klasse, im schnellen EditMode prüfbar) und Logik stand, die nur über langsames, vages PlayMode erreichbar ist, gewinnt der Seam. Das Ergebnis ist eine **EditMode-Suite von ~75 Tests über die Pure-Logic-Schicht**.

### Null Allokationen zur Laufzeit

- **Festes, vorab instanziiertes `AudioObject[]`-Pool** — kein `Instantiate`/GC während des Spiels.
- **`Physics.RaycastNonAlloc`** in einen wiederverwendeten Buffer für den WallCheck — kein Array-Churn pro Frame.
- **`AudioHandle` ist ein `readonly struct`** `{ PoolIndex, Generation }` — ein Value-Type-Ticket, keine Heap-Referenz.

### Schutz vor veralteten Handles via Generations

Jeder Slot trägt einen `Generation`-Zähler, der bei jeder (Neu-)Vergabe erhöht wird. Der `AudioHandle`, den du erhältst, trägt die Generation, mit der er erzeugt wurde. `Stop` / `FadeOut` validieren **Bounds *und* Generation**, bevor sie handeln — so wird ein alter Handle, dessen Pool-Slot inzwischen wiederverwendet wurde, zu einem stillen No-op, statt den Sound eines Fremden zu stoppen. `O(1)`-Stop, mit garantierter Korrektheit. (Die Slot-*Vergabe* ist ein bewusster `O(n)`-Scan — einfach und billig gegen ein kleines festes Pool.)

---

## Feature-Highlights

- **Intelligentes Pooling** — vorgecachtes `AudioSource`-Pool; OneShot-Slots via `BusyUntilTime`-Zeitstempel gesperrt, damit kurze SFX nie zu früh abgeschnitten werden.
- **Leichtgewichtige Wand-Occlusion** *(opt-in)* — Raycasts vom Listener zur Quelle; jeder blockierende Layer trägt einen **Dämpfungsfaktor (0–1)**, der einen `AudioLowPassFilter`-Cutoff multiplikativ Richtung einer Floor-Frequenz zieht. Weil die Dämpfung multiplikativ ist, sind gestapelte Wände **reihenfolge-unabhängig** und nähern sich dem Floor **asymptotisch**, statt auf feste Cutoffs zu springen. Weiche Übergänge via Per-Frame-Smoothing — kein hörbares „Pop", wenn man aus der Deckung tritt. Das ist bewusst ein **leichtgewichtiges Occlusion-Modell, kein voller Spatializer** (Steam Audio / Oculus) — ein Feature, keine Abkürzung.
- **Fade-Familie** — `FadeIn` / `FadeOut` / `Crossfade`, spatial und nicht-spatial. `Crossfade` ist reine Komposition aus `FadeOut + FadeIn`, kein Sonderpfad.
- **Follow ohne Re-Parenting** — spatiale Sounds folgen einem Emitter, indem sie seine Position pro Frame kopieren, nie via `SetParent` (das würde einen gepoolten Slot dem Aufrufer überlassen und ihn mit dem Emitter sterben lassen).
- **Scope-bewusste globale Pause** — `PauseAll` / `UnpauseAll`, mit Per-Sound-Opt-out aus der globalen Pause.
- **Hybrides Async-Backend** — nutzt allokationsfreies UniTask, wenn vorhanden, und fällt auf native Unity-Coroutines zurück, wenn es fehlt. Zur Compile-Zeit über ein Assembly-Definition-`versionDefines`-Gate (`USE_UNITASK`) gewählt, sodass der Build in beiden Fällen sicher ist.

---

## Öffentliche API

Ein Aufruf spielt einen Sound; ab da besitzt das Framework den `AudioSource`-Lebenszyklus.

```csharp
// Abspielen
AudioHandle h = AudioManagerDynamic.PlaySpatial(myAdo, sourceTransform); // 3D, positionsbezogen, optionaler WallCheck
AudioHandle h = AudioManagerDynamic.PlaySpatial(soundRequest);           // dasselbe, gebündelt als { Ado, Source }
AudioHandle h = AudioManagerDynamic.PlayNonSpatial(myAdo);               // 2D (UI, Musik, Stingers)

// Stoppen (No-op, außer der Handle ist noch aktuell)
AudioManagerDynamic.Stop(h);

// Fade — immer managed, liefert daher immer einen nutzbaren Handle
AudioHandle h = AudioManagerDynamic.FadeInSpatial(myAdo, sourceTransform, duration);
AudioManagerDynamic.FadeOut(h, duration);                                // fadet runter, stoppt, gibt den Slot frei
AudioHandle h = AudioManagerDynamic.CrossfadeSpatial(fromHandle, toAdo, sourceTransform, duration);

// Pause
AudioManagerDynamic.PauseAll();
AudioManagerDynamic.UnpauseAll();
```

Sounds werden auf einem `AudioDataObject` konfiguriert (eine `ScriptableObject`-„Control Surface") — Clips, Lautstärke-Kategorie, Spatial Blend und Flags — das bei jedem Dispatch auf den gepoolten `AudioSource` gespiegelt wird.

---

## Mit KI gebaut — unter Engineering-Disziplin

Ich habe dies in Partnerschaft mit einem KI-Coding-Agent (Claude) gebaut. Ich denke, *wie* das gemacht wurde, zählt mehr als die bloße Tatsache, also werde ich konkret:

- **Der Agent arbeitet gegen einen schriftlichen Vertrag, nicht nach Bauchgefühl.** Eine `CLAUDE.md` im Repo kodiert die Architektur, die Design-Invarianten und die Regeln der Zusammenarbeit. Die KI wird davon gesteuert — sie darf das Design nicht mitten in der Aufgabe neu definieren.
- **Der Frozen-Test-Loop schränkt die KI ein, nicht umgekehrt.** Tests werden red-first geschrieben und dann eingefroren. Die Aufgabe der KI ist, eine *feste* Spezifikation zum Bestehen zu bringen — sie kann nicht still einen Test abschwächen, um Grün zu bekommen, weil die Regel es verbietet und der Mutation Check einen Test entlarven würde, der nichts schützt.
- **Die Architekturentscheidungen sind meine.** Die Pure-Logic-Seams, der Strategy-Split fürs Async-Backend, das Follow-Modell ohne Re-Parenting, der Generation-Guard gegen veraltete Handles — das sind bewusste Entscheidungen, getroffen für Testbarkeit und Korrektheit, dann mit KI-Unterstützung implementiert.

Der Punkt ist einfach: Das Engineering-Urteil bleibt menschlich, und die KI beschleunigt die Ausführung eines disziplinierten Prozesses.

---

## Status & Scope

Aktiv in Entwicklung, auf dem Weg zu einem Unity-Asset-Store-Release. Das Fundament (Pooling, Occlusion + Smoothing, Fade-Familie, Pause, Follow) steht und ist test-gestützt; die Feature-Breite wird bewusst ausgebaut, nie auf Kosten der obigen Testdisziplin.

**Bewusst *nicht* im Scope:** ein voller HRTF-Spatializer. Der WallCheck ist per Design leichtgewichtige Occlusion.

**Umgebung:** Unity 6 · C# · JetBrains Rider.
