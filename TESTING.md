# AudioTool — Test-Disziplin & Test-Qualitäts-Audit

> **Was diese Datei ist:** beides zugleich — die **verbindliche Test-Disziplin** dieses Projekts (Teil I)
> *und* das **Audit** der tatsächlich geschriebenen Tests (Teil II). Dieselbe Zweiteilung wie in
> [`REVIEW_PROTOCOL.md`](REVIEW_PROTOCOL.md): erst die Vorschrift, dann die Aufzeichnung.
>
> **Die beiden Teile haben unterschiedliche Haltbarkeit — das ist wichtig:**
> - **Teil I ist verbindlich und darf NICHT veralten.** Er ist die Arbeitsanweisung für jede neue Methode.
>   Änderungen daran nur bewusst und mit Patricks Go.
> - **Teil II ist eine Mess-/Analyse-Momentaufnahme und DARF veralten.** Er beschreibt einen Stand, keine
>   Regel; bei Bedarf neu erheben.
>
> **Verhältnis zum Rest:** Zusammenarbeit & Einstieg → [`CLAUDE.md`](CLAUDE.md) · Architektur & Warum →
> [`ARCHITECTURE.md`](ARCHITECTURE.md) · Aufgaben → [`BACKLOG.md`](BACKLOG.md) · Review-Verfahren →
> [`REVIEW_PROTOCOL.md`](REVIEW_PROTOCOL.md).

---

# TEIL I — Die Test-Disziplin (verbindlich)

## 1. Der TDD-Loop

DIE Regel für jede neue Methode / jedes neue Feature. Von Patrick formalisiert, bindend. In Reihenfolge:

1. **Zuerst den fehlschlagenden Test schreiben.** Er muss rot sein, bevor Implementierung existiert.
2. **Diese Tests sind danach EINGEFROREN — werden nie wieder angefasst.** Bestehende Tests werden nicht
   editiert/abgeschwächt, um Code grün zu bekommen. *Neue* Tests für *neues* Verhalten sind ok.
3. **Die Methode schreiben**, die die eingefrorenen Tests grün macht.
4. **Wenn alles grün ist: bewusst einen Fehler einbauen** (Mutation Check), der mindestens einen Test rot
   macht — vorher vorhersagen welchen. Beweist, dass die Tests wirklich etwas schützen.
5. **Nach bestätigtem Rot: korrekten Zustand wiederherstellen.** Danach die Tests in Ruhe lassen.
6. **Wird eine Methode so umgebaut, dass ihre eingefrorenen Tests obsolet werden**, wandert die Nacharbeit als
   TODO in den BACKLOG — und wird NIE ohne Patricks ausdrückliche Anweisung ausgeführt. Keine stillen
   Test-Rewrites.

**Patricks Kernangst sind tautologische / Change-Detector-Tests.** Red-first + Einfrieren + Mutation Check
sind die konkreten Schutzwälle dagegen.

## 2. Gate-Disziplin: ein Schritt, ein Stopp, eine Bestätigung

> **Anlass (2026-06-28):** In der `DuckEnvelope`-Session wurden Stub, grüne Implementierung **und** der
> vorweggenommene Mutation-Check in *einem* Zug geliefert. Damit hatte Patrick nie ein echtes, **selbst
> beobachtetes** Rot/Grün in der Hand — die Gates waren entwertet. Wurzel wie 2026-06-20: „Fortschritt
> machen" wurde über „jeden Schritt einzeln beweisen" gestellt.

**Der Loop ist KEINE Schritt-Liste zum Abarbeiten, sondern eine Reihe von Bestätigungs-Gates.** Jeder Schritt
endet mit einem STOPP; der nächste beginnt **erst, wenn Patrick das von ihm beobachtete Testergebnis
bestätigt hat** — nicht, wenn *ich* den Zustand für richtig halte.

- **Gate 1 — Rot:** Tests + `NotImplementedException`-Stub schreiben. **STOPP.** Keine Implementierung
  schreiben — auch nicht „schon mal vorbereiten" — bevor Patrick das laufende Rot bestätigt hat.
- **Gate 2 — Grün:** Implementierung schreiben. **STOPP.** Kein Mutation-Check, bevor Patrick das Grün
  bestätigt hat.
- **Gate 3 — Mutation:** Genau eine Mutation einbauen und die Vorhersage (welcher *benannte* Test rot wird)
  im selben Schritt nennen — aber erst *jetzt*, nie früher. **STOPP.** Patrick bestätigt das Rot gegen die
  Vorhersage.
- **Gate 4 — Wiederherstellung:** Korrekten Zustand wiederherstellen. **STOPP.** Patrick bestätigt das erneute
  Grün. Danach sind die Tests eingefroren.

**Eiserne Regeln, die die Gates absichern:**
- **Nie mehr als ein Gate pro Antwort.** Stub und Implementierung niemals im selben Zug.
- **Nie einen späteren Schritt vorab ankündigen oder vorbereiten** (z. B. die Mutation nennen, während wir
  noch im Grün-Gate stehen). Das nimmt Patrick die eigene Beobachtung vorweg.
- **„Der Vertrag ist klar" rechtfertigt schnelleres *Vorbereiten*, nie das Überspringen eines Gates.**
- **Im Zweifel: STOPP und fragen.** Der teurere Fehler ist das Vorpreschen, nicht die Rückfrage.

## 3. Stützende Prinzipien

- **Erwartungswerte kommen aus der SPEZIFIKATION, nicht aus dem Code.** Vor dem Blick auf die Implementierung
  aus dem Vertrag hand-ableiten. Wenn „korrekt" ohne Code-Lesen nicht sagbar ist → STOP, erst das Soll mit
  Patrick klären. *(Dieselbe Grundregel trägt auch das Review-Verfahren — `REVIEW_PROTOCOL.md` §1.)*
- **Erstes Rot darf ein „laufendes Rot" sein:** neuen Typ/Member als `NotImplementedException`-Stub anlegen,
  damit das Test-Assembly kompiliert und die Tests *laufen* und scheitern (klarer als ein Compile-Fehler).
- **Aktuell testen wir NUR neuen Code.** Bestandscode nachzutesten ist eine separate, aufgeschobene Aufgabe
  (BACKLOG) — nie still mit reingezogen.
- **Ehrliche Tests gewinnen Design-Trade-offs.** Wenn die Wahl steht zwischen einer leicht-ehrlich-testbaren
  Architektur (Seam/Interface → EditMode-testbar mit Fake) und einer ohne Abstraktion (nur per
  langsamem/vagem PlayMode prüfbar): die testbare wählen. Ein kleiner Seam ist es wert. Die daraus
  entstandene pure Logik-Schicht ist in [`ARCHITECTURE.md`](ARCHITECTURE.md) §2 gelistet — dort ist ihre
  Single Source, hier bewusst keine zweite Kopie.

## 4. Schutzregeln bei rotem Test oder verfehlter Vorhersage

> **Anlass (2026-06-20):** Patzer der Occlusion-Modell-Session. Gemeinsame Wurzel: „grün/erwartungskonform
> machen" wurde über „Diskrepanz verstehen" gestellt. Genau dieser Default wird hier umgedreht.

**Niemals reflexartig den Test anfassen — erst ganzheitlich analysieren, wo der Fehler sitzt.** Der Test ist
die **letzte** Instanz im Verdacht, erreichbar nur per Ausschluss. Diagnose-Reihenfolge; eine Stufe wird erst
betreten, wenn die vorige als „nicht falsch" bestätigt ist:

1. **Die Implementierung** — der Code unter Test.
2. **Mein Modell / meine Hand-Herleitung** des Erwartungswerts aus der Spec.
3. **Die Vorhersage selbst** — bei verfehlter Mutations-Prognose ist meist nur mein Modell *des Tests*
   daneben; der Mutation-Check ist ohnehin bestanden, sobald *mindestens ein* Test rot ist (Suite-Ebene,
   nicht pro Test).
4. **Der Test** — zuletzt. Er *kann* falsch sein (falscher Erwartungswert, zu schwache Assertion, falsch
   dimensionierte Toleranz), und diese Möglichkeit verschließe ich mir nie. Aber sie wird erst gezogen, wenn
   der Kontext zwingend ergibt, dass *nur noch* der Test die Quelle sein kann. Dann — und nur dann — Test
   **nach explizitem Go** anfassen, mit benannter Begründung. Nie zählt: ändern, *damit meine Prognose
   stimmt*, oder Schutz ergänzen, den ein grüner Test auf Suite-Ebene schon liefert (Gold-Plating).

**Test-Änderung nur mit vorab benannter Kategorie; Default ist Veto.** Jeder Änderungsvorschlag muss *vor* der
Änderung einer Kategorie zugeordnet werden: **(a)** echter Authoring-Defekt (z. B. falsche Toleranz),
**(b)** bewusste Spec-Änderung mit Patricks Go, **(c)** obsolet durch Umbau → BACKLOG (Loop-Regel #6). Passt
nichts davon → keine Änderung. Im Zweifel: Test stehen lassen und fragen.

**Float-Erwartungswerte: Toleranz aus der Rechnung ableiten, nicht aus Reflex.** Entsteht ein Erwartungswert
durch verkettete float32-Arithmetik mit nicht exakt darstellbaren Faktoren (z. B. `0.3f`, `0.8f`), die
Toleranz an der akkumulierten Rundung ausrichten — für Cutoff-Hz: fachlich vernachlässigbar (~`1e-2`), aber
weiterhin um Größenordnungen enger als jede sinnvolle Mutation. Kein Reflex-`1e-5` auf Float-Ketten.

## 5. Checkliste beim Schreiben jedes neuen Tests

Abgeleitet aus den Audit-Befunden in Teil II — verbindlich:

- [ ] **Spec-first-Header** als Pflicht-Boilerplate (die „hand-derived, NOT read off implementation"-Klausel).
- [ ] **Jeden Guard auf echten Pin prüfen:** „Wenn ich diese `if`-Zeile lösche — wird *dieser konkrete* Test
      rot?" Wenn nein → Input ändern. Das ist der präziseste Mutation-Check pro Branch. *(Lehre aus #6.)*
- [ ] **Grenzen als true/false-Paar** um den `==`-Punkt — **aber nur an Diskontinuitäten.** An einer
      *kontinuierlichen* Grenze (Clamp bei 1.0, wo `clamp(x) = x`) ist die Schwelle grundsätzlich nicht eng
      pinnbar; dort ist „Existenz + ein Repräsentant" korrekt. *(Lehre aus #7/#11 vs. #12.)*
- [ ] **Komposite/realistische Sequenzen** testen, nicht nur isolierte Schritte. *(Lehre aus #5.)*
- [ ] **Operator unterscheidbar machen:** Input wählen, der `|`/`+`, `&&`/`&` etc. trennt, wenn der Operator
      vertraglich zählt. *(Lehre aus #10.)*
- [ ] **Vertragliche Seiteneffekte (Warn-Logs) mit `LogAssert.Expect` pinnen** — für ein verkauftes
      Asset-Tool ist die Fehlkonfigurations-Meldung Teil der UX. *(Lehre aus #8/#9.)*
- [ ] **Recording-Double + Interface-Seam** als Standardmuster für Unity-gekoppelte Logik. *(Blaupause:
      `FakeFadeTarget`, siehe #3.)*
- [ ] **Degenerate Inputs** (negativ/leer/null) bewusst mitnehmen, wenn der Guard sie abdeckt.
- [ ] **Auch der INPUT muss aus der Spec kommen**, nicht nur die Assertion. Ein Input, der gewählt wird, um
      einen bestimmten Mutanten zu töten, ist Code-Ableitung in Tarnung. *(Lehre aus #12, Stolperstein 2.)*

## 6. Sonder-Loop: Bestandscode nachtesten (M2 / Gruppe B)

> Variante des normalen Loops, zugeschnitten auf **bestehenden** Code. Eine Methode pro Runde.

**Warum eigen:** Bei neuem Code existiert die Methode noch nicht — man *kann* nicht abschreiben. Beim
Bestandscode liegt die Implementierung **offen vor einem** → die „read off implementation"-Falle ist hier
**maximal stark**. Daraus folgt der zentrale Entscheidungspunkt, den neues TDD nicht braucht:

> Der spec-abgeleitete Test wird gegen den *alten* Code ausgeführt:
> - **grün** → Verhalten ist korrekt und jetzt eingefroren. ✔
> - **rot** → bewusst entscheiden, **niemals** den Erwartungswert still an den Code anpassen:
>   - **Spec richtig → latenter Bug im Bestandscode gefunden.** STOP, an Patrick melden.
>   - **Code richtig, Spec war naiv** → Spec mit Patrick schärfen, das *Warum* verstehen und neu
>     hand-ableiten (nicht den Code-Wert abschreiben — das wäre exakt die Tautologie).

**Die Schritte:**

0. **Seam-Frage zuerst:** Steckt eine **pure Entscheidung** drin, die sich rausziehen lässt (wie
   `PoolSlotAvailability`)? Wenn ja → erst extrahieren. Wenn nein (echtes Unity-Verhalten) → PlayMode-Fall.
1. **Formulieren, WAS die Methode soll** — aus dem **Vertrag/der Absicht**, *bewusst nicht* durch
   Paraphrasieren der Code-Zeilen.
2. **Beschreibung schreiben, aus der sich Tests ableiten** — wörtlich als **XML-Doc-Header der Testklasse**.
   So wird die Spec Teil des Tests und driftet nicht weg.
3. **Red-First als Stub** (nicht Auskommentieren): die neue pure Funktion anlegen, Rumpf mit
   `throw new NotImplementedException()` → Test-Assembly kompiliert, Tests **laufen** und scheitern. Die alte
   Methode bleibt unangetastet, bis grün.
4. **Normaler Testweg** — grün → Mutation → rot mit Ansage → grün. **Schärfung:** Die Mutation ist **kein**
   Zufalls-Kaputtmachen, sondern **jeden Guard/Branch einzeln** kippen und vorhersagen, *welcher namentliche
   Test* rot wird. Macht das Löschen einer `if`-Zeile **keinen** Test rot → der Guard ist nicht getestet.
5. **Verdrahtung schließen + Checkliste (§5) abhaken:** die alte Methode zeigt jetzt auf die getestete pure
   Funktion (triviale, per Augenschein prüfbare Delegation).
6. **Eine Methode pro Runde, eigener Commit.** Kleine, gegenlesbare Diffs; die neuen Tests sind ab dann
   eingefroren. (Patrick committet.)

**Ehrliche Warnung zum Seam (Schritt 0):** Das Extrahieren ist selbst ein **Refactor von ungetestetem Code**
(Henne-Ei). Absicherung: rein **mechanisch** halten (denselben Ausdruck kopieren, Eingaben zu Parametern
heben, **keine** Logikänderung), im selben kleinen Schritt; die neuen Tests decken die extrahierte Logik
sofort ab. Kommt beim Extrahieren die Versuchung auf, „nebenbei" etwas zu verbessern → STOP, separater Schritt.

---

# TEIL II — Audit-Snapshot (Mess-Datei, darf veralten)

> **Zweck:** Ganzheitliche Bewertung der Unit-Tests, Methode für Methode, um daraus bessere Tests fürs
> **Nachtesten des Bestandscodes** (M2 / Gruppe B) abzuleiten. Die Lehren daraus sind bereits nach Teil I §5
> hochgezogen — dieser Teil hält die **Belege** dafür.
>
> **Audit-Stand:** 2026-06-15, Nachtrag 2026-06-28 (`VolumeResolver`). Auditierte Einheiten: **12** (82 Tests).
> **Projekt-IST (nachgezählt 2026-07-27): 15 getestete Logik-Einheiten, 118 EditMode-Tests in 19 Dateien.**
> Die Differenz ist noch **nicht** auditiert (Liste am Ende von Teil II) — das ist eine offene Erhebung,
> kein Qualitätsurteil.

## Bewertungsraster

Jede Methode wurde gegen vier Kriterien geprüft:

1. **Spec-abgeleitet vs. Code-abgeschrieben** — kommen die Erwartungswerte aus dem Vertrag oder aus der
   Implementierung? (Der zentrale Tautologie-Test.)
2. **Mutation-Resistenz** — würde ein bewusst eingebauter Fehler wirklich einen Test rot machen? Insb.: wird
   *jeder* Guard/Branch durch einen Test gepinnt, dessen Erwartung sich vom „allgemeinen Pfad" unterscheidet?
3. **Branch-/Grenzwert-Abdeckung** — alle Pfade + Ränder (off-by-one, `==`-Grenze, null/leer)?
4. **Lücken** — was wird *nicht* geprüft, das geprüft gehören sollte?

**Noten:** **Exzellent** · **Stark** · **Solide** · **Dünn (mit Lücke)**.
Keine Note „mangelhaft" vergeben — es gibt in dieser Suite keinen schlechten Test.

## Gesamturteil (zuerst, ehrlich)

- ✅ **Kein tautologischer / Change-Detector-Test gefunden.** Kein Test liest die eigene Ausgabe der
  Implementierung zurück. Der dünnste Fall (`LowPassDispatchPolicy`) pinnt immerhin eine *Entscheidung*.
- ✅ **Spec-first-Disziplin durchgängig.** Jeder Test-Header trägt die „hand-derived, NOT read off
  implementation"-Klausel — und die Zahlenwerte (0.3, 0.75, 40, 8000 …) sind unabhängig nachrechenbar, also
  keine leere Floskel.
- ✅ **Grenzwert-Disziplin stark, wo es zählt.** Die kritischen `>=`-Ränder sind mit *benachbarten
  true/false-Paaren* gepinnt (`AudioHandleValidator`, `PoolSlotAvailability`, `FadeOperation.IsComplete`).
- ✅ **Die „Safety"-Tests sind die Kronjuwelen:** Clobber-Guard (`AudioFadeService.ClearFade`),
  null-Eintrag-vor-gültigem-Eintrag (`FillDictionaryWithKeysAndValues`), Pause-einfrieren-dann-fortsetzen.
- ⚠️ **Alle Schwächen sind *minor* und gleichen sich:** (a) einzelne Guards, deren Entfernung am getesteten
  Input *nichts* ändern würde; (b) unbestätigte Warn-Logs; (c) ein paar degenerate Inputs (negative
  Dauer/Speed); (d) Operator-Äquivalenzen (`|` vs `+`), die bei den gewählten Inputs nicht unterscheidbar sind.

| # | Einheit | Tests | Note | Headline |
|---|---|---|---|---|
| 1 | `AudioFadeMath.Evaluate` | 9 | **Exzellent** | Beide Clamps einzeln gepinnt; nur neg. Dauer ungetestet |
| 2 | `FadeOperation` | 8 | **Stark** | `IsComplete`-Grenze gepinnt; leichte Überlappung mit #1 (gerechtfertigt: Verdrahtung) |
| 3 | `AudioFadeService` | 13 | **Exzellent** | Clobber-Guard + Pause-Freeze + Stop-Count diskriminierend |
| 4 | `LowPassDispatchPolicy.Resolve` | 4 | **Solide** | Dünnste Einheit, aber gegen Hardcode + Inversion abgesichert → nicht tautologisch |
| 5 | `WallOcclusionMath` | 9 | **Stark** | Komposit-Kette `ApplyWall→ClampToFloor` geschlossen |
| 6 | `OcclusionSmoothing.Step` | 6 | **Stark** | `maxStep<=0`-Guard nicht echt gepinnt (gen. Pfad fällt zusammen) |
| 7 | `AudioHandleValidator.IsCurrent` | 6 | **Exzellent** | Beste Grenz-Disziplin: beide Ränder mit true/false-Paaren |
| 8 | `…Provider.FillLayerMaskDictionary…` | 4 | **Stark** | keep-first gepinnt; Warn-Log auf Duplikat unbestätigt |
| 9 | `…Provider.FillDictionaryWithKeysAndValues` | 6 | **Exzellent** | null-Eintrag-vor-gültigem = ideale Diskriminierung |
| 10 | `WallLayerMask.FromLayers` | 4 | **Stark** | `\|` vs `+` nicht unterscheidbar (praktisch moot bei eindeutigen Keys) |
| 11 | `PoolSlotAvailability.IsFree` | 5 | **Exzellent** | Jede AND-Klausel einzeln gepinnt + `==`-Grenze inklusiv |
| 12 | `VolumeResolver.Resolve` | 8 | **Stark** | Alle 3 Faktoren einzeln + Operator gepinnt; oberer Clamp prinzip-bedingt nur an *einem* Punkt |

---

## Detail pro Methode

### 1 — `AudioFadeMath.Evaluate(from, to, elapsed, duration)` → **Exzellent**
**Vertrag:** lineare Interpolation; clampt außerhalb `[0, duration]`; `duration <= 0` → sofort `to`.

- **Spec-abgeleitet:** Ja. `0.3` (0→0.6 @ halb), `0.75` (1→0 @ ¼) sind hand-gerechnet, Kommentare zeigen die Herleitung.
- **Mutation/Guards — beide Clamps sind *echt* gepinnt:**
  - **Unterer Clamp:** `Evaluate_AtStart` (elapsed 0) pinnt ihn **nicht** — bei `t==0` liefert auch die nackte Formel `from`. Gepinnt wird er durch **`Evaluate_BeforeStart`** (elapsed −1): ohne Guard käme −0.5, erwartet 0. ✔
  - **Oberer Clamp:** `Evaluate_AtEnd` (`t==1`) pinnt ihn **nicht** (Formel liefert dort exakt `to`). Gepinnt durch **`Evaluate_PastEnd`** (elapsed 3): ohne Guard käme 1.5. ✔
  - **`duration<=0`-Guard:** `Evaluate_ZeroDuration` — ohne Guard `0/0 = NaN`, `NaN`-Vergleiche false → Ergebnis NaN ≠ 1. ✔
  - Ziel-Wert ist **nicht** auf 1 hardcodiert: die 0.6-Tests fangen das.
- **Lücke (minor):** **negative** Dauer ungetestet (Guard ist `<= 0`, nur `== 0` geprüft). `from == to` (No-op-Fade) ungetestet, trivial.

> **Lehre:** Die *at-boundary*-Tests (AtStart/AtEnd) sind „Form"-Tests, die *past-boundary*-Tests
> (Before/Past) sind die echten Guard-Pins. Diese Unterscheidung bewusst beibehalten.

### 2 — `FadeOperation` (`CurrentVolume`, `IsComplete`, `Advanced`) → **Stark**
**Vertrag:** immutables Wertobjekt; `Advanced` akkumuliert Zeit in eine NEUE Instanz; `IsComplete ⟺ Elapsed >= Duration`.

- **Spec-abgeleitet:** Ja.
- **Mutation:**
  - **Immutabilität/Akkumulation:** `Advanced_AccumulatesElapsedAcrossMultipleCalls` (0.5 + 0.5) ist der diskriminierende Test — eine In-Place-Mutation oder fehlende Akkumulation fiele auf. ✔
  - **`IsComplete`-Grenze (`>=`):** `Advanced_ToFullDuration` (Elapsed == Duration → true). Mit `>` statt `>=` wäre es false → Test fängt es. ✔
- **Überlappung:** `CurrentVolume` delegiert an `AudioFadeMath`; die 0.5/0.75-Werte tauchen erneut auf. **Gerechtfertigt** — pinnt die *Verdrahtung* (Feld-Reihenfolge From/To/Elapsed/Duration), nicht nur die Mathe.
- **Lücke (minor):** „Elapsed knapp unter Duration → nicht komplett" nur indirekt (Midpoint-Test).

### 3 — `AudioFadeService` (`StartFade`, `Tick`, `ClearFade`, `StartFadeOut`) → **Exzellent**
Getestet über `FakeFadeTarget` (Recording-Double) — saubere Seam, kein echter `AudioSource`.

- **Spec-abgeleitet:** Ja (lineare Kurve, Zahlen aus #1).
- **Mutation — mehrere *echt* diskriminierende Tests:**
  - **Clobber-Guard** (`ClearFade_…_ClobberGuard`): nach `ClearFade` externe Volume-Setzung, `Tick` darf sie **nicht** überschreiben. Fängt fehlendes `Active=false`. ✔ (Sicherheitskern!)
  - **„Nach Settle nicht erneut schreiben"**: fängt, wenn der Fade nach Abschluss nicht deaktiviert wird. ✔
  - **`StopOnEnd`-Zweig:** `StopCallCount` unterscheidet FadeIn (0) von FadeOut (1). ✔
  - **Pause-Freeze:** `Tick_PausedThenResumed_ResumesFromWhereItWas` — der Kommentar nennt explizit „a buggy Tick would complete to 1 here". Echte Diskriminierung des `IsPaused`-Short-Circuits. ✔
  - **`StartFadeOut` startet bei aktueller Lautstärke** (0.8), nicht hardcodiert 1. ✔
- **Lücken (minor):** (a) `StartFade` auf einem bereits aktiven Slot (Neustart) ungetestet; (b) **zwei gleichzeitig aktive Fades** auf verschiedenen Slots, die zusammen voranschreiten — der „DoesNotTouchInactiveSlots"-Test hat 2 Slots, aber nur einen aktiven.

> **Lehre:** Das `FakeFadeTarget`-Muster ist die **Blaupause für Gruppe B** (Unity-gekoppelt): kleines
> Recording-Double hinter ein Interface, dann am Double assert. Genau so für `ShouldContinueLoop`,
> `Gate`/`ResolveVolume`, `PauseAll`/`UnpauseAll`.

> ⚠️ **Nachtrag 2026-07-27 (Kontext, kein Audit-Befund):** Seit dem Ducking-Umbau schreibt
> `PooledFadeTarget.Volume` den **Per-Slot-Fade-Faktor** statt direkt `source.volume`
> ([`ARCHITECTURE.md`](ARCHITECTURE.md) §6). Die hier auditierten Tests blieben davon **unberührt und grün** —
> sie testen die Rampen-Mathematik über `FakeFadeTarget`, also agnostisch dazu, worauf `Volume` physisch
> zeigt. Genau dafür war der Seam da; ein Lehrstück, warum Loop-Regel #6 hier nicht greifen musste.

### 4 — `LowPassDispatchPolicy.Resolve(useWallCheck, defaultCutoff)` → **Solide** (dünn, aber valide)
**Vertrag:** `Enabled ⟺ useWallCheck`; `CutoffFrequency` = der konfigurierte „offene" Wert, unverändert durchgereicht.

- Die Methode ist heute fast trivial (konstruiert nur den Struct). Trotzdem **nicht tautologisch:**
  - Hardcode `Enabled=true` → `NonWallCheck`-Test fällt. ✔
  - **Inversion** `Enabled=!useWallCheck` → beide Enabled-Tests fallen. ✔
  - Cutoff hardcodiert → der `17500f`-Test fängt es (Cutoff wird *regardless* durchgereicht). ✔
- **Einordnung:** geringster Wert pro Test der Suite, aber legitim — es pinnt eine **Design-Entscheidung**
  (Filter nur für wand-geprüfte Sounds) gegen spätere „Optimierungen". Kein Handlungsbedarf.

### 5 — `WallOcclusionMath` (`ApplyWall`, `ClampToFloor`) → **Stark** *(Modellwechsel 2026-06-20)*
**Vertrag (multiplikativ):** `ApplyWall = current − (current − floor) · damping`. Eine Wand dämpft den Cutoff
um den Bruchteil `damping` (0 = transparent, 1 = fällt in einer Wand auf den Floor) **Richtung Floor**. Über N
Wände wird der offene Bereich über dem Floor mit `∏(1 − dᵢ)` skaliert → reihenfolge-unabhängig und
**asymptotisch** zum Floor. `ClampToFloor` ist damit nur noch ein Sicherheitsnetz gegen Fehlkonfig (`d>1`) und
Float-Drift. *(Modell-Begründung → [`ARCHITECTURE.md`](ARCHITECTURE.md) §9.)*

- **Tests (9):** 7× `ApplyWall_*` (Einzelwand-Fraktion, `d=0` transparent, `d=1` → Floor, multiplikative Akkumulation, abnehmender Absolut-Schritt, Reihenfolge-Unabhängigkeit, Komposit `ApplyWall→ClampToFloor` bei `d>1`) + 2× `ClampToFloor_*` (modell-agnostisch).
- **Spec-abgeleitet:** Ja; alle Erwartungswerte aus der Dämpfungs-/Asymptote-Gleichung hand-abgeleitet (`Open=22000`, `Floor=1000`), nicht aus dem Code.
- **Mutation:** `−`→`+` in `ApplyWall`. **5 Tests rot** (Fraktion, Voll-Dämpfung, Akkumulation, Reihenfolge, Komposit), `ZeroDamping` bleibt grün (`+0 == −0`, pinnt bewusst den Wert, nicht den Operator), beide `ClampToFloor` grün. Mutation gefangen → Suite schützt den Vertrag. ✔
- **Lücke aus der Vorversion geschlossen:** Der „viele Wände → Floor"-Komposit-Pfad ist jetzt als Kette getestet (`ApplyWall_ThenClampToFloor_OverDampedConfigRescuedToFloor`: `d=1.5` → −9500 → Clamp → 1000).

**Zwei Stolperstellen in dieser Session — ehrlich festgehalten** (Anlass für die Schutzregeln in Teil I §4):

1. **Float-Toleranz war ein echter Test-Defekt.** `Delta = 1e-5` war zu eng für die verkettete
   float32-Rechnung des Reihenfolge-Tests (Faktoren `0.3f`/`0.8f` nicht exakt darstellbar → Ergebnis
   `3939.99976` statt `3940`, Abweichung ~`2.4e-4`). Die Diagnose lief korrekt die Instanzen durch: Code
   korrekt, Hand-Herleitung `3940` korrekt → *erst danach* legitim beim Test gelandet (Authoring-Defekt,
   Kategorie a). Korrigiert auf `1e-2`.
2. **Verfehlte Mutations-Vorhersage war KEIN Test-Defekt, sondern ein Modellfehler.** Vorhergesagt: 6 rot.
   Tatsächlich: 5. `PerWallAbsoluteStepDiminishes` blieb grün, weil er nur die *relative* Ordnung prüft
   (`secondStep < firstStep`) — unter dem Vorzeichen-Flip drehen beide Schritte ins Negative und behalten ihre
   Ordnung (`−15750 < −10500`). Der Mutation-Check war trotzdem bestanden (≥1 rot, Suite-Ebene). Der Reflex,
   den Test zu „härten", damit die Prognose stimmt, wurde **verworfen** — reines Gold-Plating. Test
   **unangetastet**.

### 6 — `OcclusionSmoothing.Step(current, target, dt, speed)` → **Stark**
**Vertrag:** MoveTowards mit `speed` Hz/s; `speed<=0` → sofort `target`; kein Overshoot.

- **Spec-abgeleitet:** Ja.
- **Mutation:**
  - Richtung hoch/runter beide getestet (Vorzeichenfehler fiele auf). ✔
  - **Kein Overshoot** (`absDiff<=maxStep → target`): `WithinOneStep` (900→1000) — ohne Snap käme 1100. ✔
  - **`speed<=0 → target`:** `ZeroOrNegativeSpeed` unterscheidet sauber von der zweiten Early-Return (`maxStep<=0 → current`): ohne den speed-Guard käme `current` (100) statt `target` (1000). ✔
- **Lücke (minor):** **`maxStep<=0`-Guard ist nicht echt gepinnt.** `Step_ZeroDeltaTime` liefert 100 — aber auch *ohne* diesen Guard ergäbe der allgemeine Pfad 100 (`current + maxStep(0)`). Der Guard schützt eigentlich **negatives** `dt`/`speed` — und genau die sind ungetestet. Der Test „beweist" hier also weniger, als er suggeriert.

> **Lehre (wichtig):** Ein Guard, dessen Entfernung am getesteten Input *dasselbe* Ergebnis liefert, ist
> **nicht** getestet. Beim Schreiben fragen: „Wenn ich diese `if`-Zeile lösche — wird *dieser* Test rot?"

### 7 — `AudioHandleValidator.IsCurrent(idx, handleGen, slotGen, poolLen)` → **Exzellent**
**Vertrag:** außerhalb `[0, poolLen)` → nie current; sonst `handleGen == slotGen`.

- **Beste Grenz-Disziplin der Suite — beide Ränder mit benachbarten true/false-Paaren gepinnt:**
  - **Oberer Rand:** `IndexAtPoolLength` (idx==10, pool 10 → false). Mit `>` statt `>=` würde 10 durchrutschen → true; Test fängt es. ✔
  - **Unterer Rand:** `NegativeIndex` (−1 → false) **+** `IndexZeroLowerBound` (0 → true) als Paar. Mit `<=0` statt `<0` würde idx 0 fälschlich raus → der Zero-Test fängt es. ✔✔
  - **Generation:** `StaleGeneration` (7 vs 8 → false) fängt das Ignorieren der Generation. ✔
  - **Crash-Guard wörtlich:** `IndexFarAbovePoolLength` (99999, pool 50). ✔
- **Lücke:** praktisch keine nennenswerte.

### 8 — `AudioManagerDictionaryProvider.FillLayerMaskDictionaryWithLayerRelatedValues` → **Stark**
**Vertrag:** `SingleLayer → WallDampingFactor`; Duplikat = **keep-first**; null/leer = stiller No-op.

- **Mutation:** **keep-first** gepinnt (`DuplicateLayer_KeepsFirstValue`): mit `dict[k]=v` statt `TryAdd` käme 9000 statt 5000. ✔ null/leer-Guards getestet (ohne Guard NRE → Test wirft). ✔
- **Lücke (minor):** Die **Warnung** auf Duplikat wird **nicht** mit `LogAssert.Expect` bestätigt. Das *Verhalten* (keep-first) ist gesichert, die *Diagnose-Meldung* nicht — für ein Asset-Tool Teil des Vertrags.

### 9 — `AudioManagerDictionaryProvider.FillDictionaryWithKeysAndValues` → **Exzellent**
**Vertrag:** `CurrentAudioType → Volume`; drei null/leer-Guards; **null-Eintrag = skip-but-continue**; Duplikat = keep-first.

- **Mutation — die null-Eintrag-Logik ist ideal diskriminierend gebaut:** `NullEntry_IsSkipped_RestStillMapped` setzt das `null` **vor** einen gültigen Eintrag. Mit `break` statt `continue` bliebe Music ungemappt (Count 0); ohne null-Check → NRE. Beides fällt. ✔✔
- Drei Guard-Branches (null transfer / null array / leer) einzeln getestet. ✔ keep-first gepinnt. ✔
- **Sauberkeit:** `TearDown` mit `DestroyImmediate` für die erzeugten ScriptableObjects — vorbildlich (kein Native-Leak in EditMode).
- **Lücke (minor):** Warn-Logs (wie #8) unbestätigt.

### 10 — `WallLayerMask.FromLayers(layers)` → **Stark**
**Vertrag:** `mask |= 1 << layer` über alle Layer; null/leer → 0.

- **Mutation:** `1<<layer` vs `layer` gepinnt (Single 3 → 8). null-Guard getestet (sonst NRE). ✔
- **Lücke (minor, praktisch moot):** **`|` vs `+` ist nicht unterscheidbar** — bei nicht-überlappenden Bits gilt OR == Summe (`{3,5}`: 8|32 = 8+32 = 40). Eine `+=`-Mutation würde **nicht** gefangen. Zum Unterscheiden bräuchte es überlappende Inputs (z. B. `{3,3}`: OR 8, `+` 16). **Aber:** der reale Aufrufer übergibt `Dictionary.Keys` (immer eindeutig), daher in der Praxis irrelevant.

> **Lehre:** Wenn zwei Operatoren am gewählten Input dasselbe liefern, ist der Operator nicht gepinnt. Falls
> die Unterscheidung *vertraglich* zählt, Input wählen, der sie trennt — auch wenn der „in der Praxis nicht
> vorkommt".

### 11 — `PoolSlotAvailability.IsFree(isPlaying, currentTime, busyUntilTime, isPaused)` → **Exzellent**
**Vertrag:** frei ⟺ `!isPlaying && currentTime >= busyUntilTime && !isPaused`.

- **Jede AND-Klausel einzeln gepinnt:** Playing→belegt; busy-window offen→belegt; paused→belegt (trotz sonst-frei). Jeder Test isoliert genau eine Klausel. ✔
- **`>=`-Grenze inklusiv gepinnt:** `CurrentTimeEqualsBusyUntil → free` — mit `>` käme false. ✔
- Vorbild für die kommenden Prädikat-Extraktionen.

### 12 — `VolumeResolver.Resolve(basis, fade, duck)` → **Stark** *(neu, TDD, 2026-06-28)*
**Vertrag (Stufe-1-Gain):** `clamp01(basis · fade · duck)`. Drei unabhängige Gain-Faktoren
(Kategorie-Basis / Per-Slot-Fade / Per-Kategorie-Duck), multiplikativ verkettet, hart auf `[0,1]` geklemmt.
Einziger Besitzer von `source.volume` (Stufe 1) → [`ARCHITECTURE.md`](ARCHITECTURE.md) §6.

- **Spec-abgeleitet:** Ja. Werte (`0.6`, `0.5`, `0.8·0.5·0.5 = 0.2`, `2`, `-0.5`) sind frei gewählte
  Repräsentanten aus der Vertrags-Domäne, hand-gerechnet — **nicht** aus dem Code. **Eine** Ausnahme wurde
  *verworfen* (siehe Stolperstein 2): ein vorgeschlagener `1.5`-Test war code-getrieben und ist **nicht** in der Suite.
- **Mutation/Guards:**
  - **Jeder der drei Faktoren einzeln gepinnt** (das stärkste Merkmal): Faktor weglassen kippt genau einen Test —
    Basis ignorieren → `CategoryVolumeOnly` (0.6 vs 1) ✔ · Fade ignorieren → `FadeFactorOnly` (0.5 vs 1) ✔ ·
    Duck ignorieren → `DuckFactorOnly` (0.5 vs 1) ✔. Saubere Lokalisierung, welcher Faktor bricht.
  - **Operator `×`→`+`:** gefangen von `CategoryVolumeOnly` (+ → 2.6 → Clamp 1 ≠ 0.6) **und** `AllThree`
    (+ → 1.8 → Clamp 1 ≠ 0.2). ✔
  - **Oberer Clamp — Existenz gepinnt, Schwelle prinzip-bedingt nicht:** `ProductAboveOne` (2→1) killt den
    „Clamp entfernt"-Mutanten. ✔ Aber: an einer **kontinuierlichen** Grenze (`clamp(x)=x` bei `x=1`) ist
    `>` vs `>=` ein **echter Äquivalenz-Mutant**. Das ist **keine** behebbare Lücke, sondern die Natur
    einseitiger Clamp-Tests.
  - **Unterer Clamp:** `NegativeFactor` (-0.5 → 0) killt „Clamp entfernt". ✔ Input -0.5 liegt **nicht** auf
    einer „natürlichen" Mutant-Grenze → fängt auch Schwellen-Shifts Richtung -1. `DuckToZero` (0→0) ist primär
    ein **semantischer** Test (voller Duck = Stille), kein Clamp-Test.
- **Lücken (minor, größtenteils inhärent):** (a) obere Clamp-Schwelle nur an einem Punkt; (b) Kommutativität
  ungetestet (Produkt kommutiert ohnehin); (c) `AllNeutral` (1,1,1) ist der schwächste Test — kann `×`/`+`
  nicht unterscheiden und dokumentiert nur den Neutralfall (vgl. die Dünnheit von #4).
- **Float:** `Delta = 1e-5`. Nur `AllThree` hat float32-Subtilität (`0.8f` → ≈ `0.20000000298`); akkumulierter
  Fehler ~`1e-8` bei kurzer 2-Mult-Kette → großzügig, aber weit unter jeder Mutation. **Ehrlich:** `1e-5`
  wurde aus den Bestandstests übernommen, nicht eigens hergeleitet; bei der kurzen Kette vertretbar.

**Zwei Stolperstellen dieser Session — ehrlich festgehalten:**

1. **Erster Mutant war grenz-äquivalent → überlebte (verfehlte Prognose, KEIN Test-Defekt).** Gewählt:
   `>= 1f` → `>= 2f`. Vorhergesagt: `ProductAboveOne` wird rot. Tatsächlich: **alle grün**, weil genau dieser
   Test Produkt **exakt 2.0** füttert — `2 >= 2` klemmt weiterhin auf 1. Diagnose-Leiter sauber durchlaufen:
   Implementierung korrekt → Hand-Herleitung korrekt → Fehler bei **Stufe 3 (Mutant/Prognose)**, nicht beim
   Test. Ersetzt durch einen **echten** Mutanten (`return 1f` → `return product`). Lehre: einen Mutanten nie
   *auf den Test-Input* legen — das ist ein blinder Fleck, kein Schutzbeweis.
2. **Die `1.5`-Falle (von Patrick gefangen).** Der „Fix" für Stolperstein 1 war, einen Test mit Input `1.5`
   zu ergänzen. Die *Assertion* (1.5 → 1) ist spec-konform — aber den **Wert 1.5 hatte ich rückwärts aus dem
   Mutanten** konstruiert. Das ist genau die Code-getriebene Tautologie: nicht nur die Assertion, auch der
   **Input** muss aus der Spec kommen. Test **verworfen**, nicht aufgenommen.

> **Lehre:** **Neighboring-Pair-Pinning (#7, #11) wirkt nur an Diskontinuitäten** (in/out-of-bounds →
> true/false). An einer **kontinuierlichen** Grenze (Clamp bei 1.0) stimmen beide Seiten überein → die
> Schwelle ist grundsätzlich nicht eng pinnbar; korrekt ist „Existenz + ein Repräsentant", nicht „Schwelle".

---

## Querschnitt-Befunde

1. **Tautologie-Risiko: praktisch null.** Die kulturelle Markierung (spec-first-Header) ist gelebt, nicht dekorativ.
2. **Stärkstes Muster:** Grenzen mit *benachbarten* true/false-Paaren pinnen (#7, #11, #2). Wirksamster Schutz gegen `>`/`>=`- und `<`/`<=`-Mutationen.
3. **Wiederkehrende minor-Schwäche (das eigentliche Audit-Ergebnis):** **„Guard ohne echten Pin"** — ein `if`/Early-Return, dessen Entfernung am getesteten Input nichts ändert (#6 `maxStep<=0`).
4. **Asset-Tool-spezifisch:** Mehrere **Warn-Logs** (Fehlkonfiguration) sind reines Verhalten ohne `LogAssert`-Bestätigung (#8, #9). Für ein verkauftes Plugin ist die Diagnose-Meldung Teil der UX.
5. **Operator-Äquivalenz-blind:** `|` vs `+` (#10), isolierte statt verkettete Mathe (#5).
6. **Gesunde Redundanz:** #2/#3 beweisen #1-Zahlen erneut durch die jeweils höhere Schicht — fängt Verdrahtungsfehler, kein Ballast.

---

## Noch nicht auditiert (Stand 2026-07-27)

Diese getesteten Einheiten sind nach dem Audit dazugekommen und **noch nicht** nach dem Raster oben bewertet.
Offene Erhebung, **kein** Qualitätsurteil — alle sind regulär im TDD-Loop mit Mutation Check entstanden.

| Einheit | Tests | Entstanden |
|---|---|---|
| `ListenerCachePolicy` | 3 | 2026-06-20 (W3) |
| `WallCheckContinuation` | 9 | 2026-06-21 (R3) |
| `DuckEnvelope` | 8 | 2026-06-28 (Ducking-Schritt 2) |
| `DuckTargetPolicy` | 8 | 2026-07-02 (Ducking-Schritt 3) |
| `DuckRuleFlattening` | 8 | 2026-07-02 (Ducking-Schritt 4) |

**Summe:** 36 Tests. Zusammen mit den 82 auditierten ergibt das die 118 Tests des IST-Stands.

---

## Nicht geprüft (bewusst außerhalb dieses Audits)

- **Ungetesteter Glue/Orchestrierung:** `AudioManagerDynamic`, `AudioPlaybackService.Dispatch`,
  `AudioStopService`, `AudioFollowService`, beide WallCheck-*Schleifen*, `PooledFadeTarget`,
  `AudioDuckService`, `AudioDuckComponent`. → Das ist genau der M2/Gruppe-B-Backlog + die leere
  PlayMode-Assembly (M3) + die offenen PlayMode-Smokes.
- **Korrektheit der Implementierungen selbst:** Im Zuge des Audits gegengelesen — **keine** Implementierung
  widersprach ihrer Spec, **kein** Test behauptete etwas Spec-Widriges. *(Bezieht sich auf den Stand
  2026-06-28; der Ducking-Glue kam danach dazu und ist nicht mitgeprüft.)*
