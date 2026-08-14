# AudioTool — Test-Disziplin & Test-Qualitäts-Audit

> **Was diese Datei ist:** beides zugleich — die **verbindliche Test-Disziplin** dieses Projekts (Teil I)
> *und* das **Audit** der tatsächlich geschriebenen Tests (Teil II). Dieselbe Zweiteilung wie in
> [`REVIEW_PROTOCOL.md`](REVIEW_PROTOCOL.md): erst die Vorschrift, dann die Aufzeichnung.
>
> **Die beiden Teile haben unterschiedliche Haltbarkeit — das ist wichtig:**
> - **Teil I ist verbindlich und darf NICHT veralten.** Er ist die Arbeitsanweisung für jede neue Methode.
>   Änderungen daran nur bewusst und mit dem Go des Maintainers.
> - **Teil II ist eine Mess-/Analyse-Momentaufnahme und DARF veralten.** Er beschreibt einen Stand, keine
>   Regel; bei Bedarf neu erheben.
>
> **Verhältnis zum Rest:** Zusammenarbeit & Einstieg → [`CLAUDE.md`](CLAUDE.md) · Architektur & Warum →
> [`ARCHITECTURE.md`](ARCHITECTURE.md) · Aufgaben → [`BACKLOG.md`](BACKLOG.md) · Review-Verfahren →
> [`REVIEW_PROTOCOL.md`](REVIEW_PROTOCOL.md).

---

# TEIL I — Die Test-Disziplin (verbindlich)

## 1. Der TDD-Loop

DIE Regel für jede neue Methode / jedes neue Feature. Vom Maintainer formalisiert, bindend. In Reihenfolge:

1. **Zuerst den fehlschlagenden Test schreiben.** Er muss rot sein, bevor Implementierung existiert.
2. **Diese Tests sind danach EINGEFROREN — werden nie wieder angefasst.** Bestehende Tests werden nicht
   editiert/abgeschwächt, um Code grün zu bekommen. *Neue* Tests für *neues* Verhalten sind ok.
3. **Die Methode schreiben**, die die eingefrorenen Tests grün macht.
4. **Wenn alles grün ist: bewusst einen Fehler einbauen** (Mutation Check), der mindestens einen Test rot
   macht — vorher vorhersagen welchen. Beweist, dass die Tests wirklich etwas schützen.
5. **Nach bestätigtem Rot: korrekten Zustand wiederherstellen.** Danach die Tests in Ruhe lassen.
6. **Wird eine Methode so umgebaut, dass ihre eingefrorenen Tests obsolet werden**, wandert die Nacharbeit als
   TODO in den BACKLOG — und wird NIE ohne die ausdrückliche Anweisung des Maintainers ausgeführt. Keine stillen
   Test-Rewrites.

**Die Kernsorge des Maintainers sind tautologische / Change-Detector-Tests.** Red-first + Einfrieren + Mutation Check
sind die konkreten Schutzwälle dagegen.

## 2. Gate-Disziplin: ein Schritt, ein Stopp, eine Bestätigung

> **Anlass (2026-06-28):** In der `DuckEnvelope`-Session wurden Stub, grüne Implementierung **und** der
> vorweggenommene Mutation-Check in *einem* Zug geliefert. Damit hatte der Maintainer nie ein echtes, **selbst
> beobachtetes** Rot/Grün in der Hand — die Gates waren entwertet. Wurzel wie 2026-06-20: „Fortschritt
> machen" wurde über „jeden Schritt einzeln beweisen" gestellt.

**Der Loop ist KEINE Schritt-Liste zum Abarbeiten, sondern eine Reihe von Bestätigungs-Gates.** Jeder Schritt
endet mit einem STOPP; der nächste beginnt **erst, wenn der Maintainer das selbst beobachtete Testergebnis
bestätigt hat** — nicht, wenn *ich* den Zustand für richtig halte.

- **Gate 1 — Rot:** Tests + `NotImplementedException`-Stub schreiben. **STOPP.** Keine Implementierung
  schreiben — auch nicht „schon mal vorbereiten" — bevor der Maintainer das laufende Rot bestätigt hat.
- **Gate 2 — Grün:** Implementierung schreiben. **STOPP.** Kein Mutation-Check, bevor der Maintainer das Grün
  bestätigt hat.
- **Gate 3 — Mutation:** Genau eine Mutation einbauen und die Vorhersage (welcher *benannte* Test rot wird)
  im selben Schritt nennen — aber erst *jetzt*, nie früher. **STOPP.** Der Maintainer bestätigt das Rot gegen die
  Vorhersage.
- **Gate 4 — Wiederherstellung:** Korrekten Zustand wiederherstellen. **STOPP.** Der Maintainer bestätigt das erneute
  Grün. Danach sind die Tests eingefroren.

**Eiserne Regeln, die die Gates absichern:**
- **Nie mehr als ein Gate pro Antwort.** Stub und Implementierung niemals im selben Zug.
- **Nie einen späteren Schritt vorab ankündigen oder vorbereiten** (z. B. die Mutation nennen, während wir
  noch im Grün-Gate stehen). Das nimmt dem Maintainer die eigene Beobachtung vorweg.
- **„Der Vertrag ist klar" rechtfertigt schnelleres *Vorbereiten*, nie das Überspringen eines Gates.**
- **Im Zweifel: STOPP und fragen.** Der teurere Fehler ist das Vorpreschen, nicht die Rückfrage.

## 3. Stützende Prinzipien

- **Erwartungswerte kommen aus der SPEZIFIKATION, nicht aus dem Code.** Vor dem Blick auf die Implementierung
  aus dem Vertrag hand-ableiten. Wenn „korrekt" ohne Code-Lesen nicht sagbar ist → STOP, erst das Soll mit
  dem Maintainer klären. *(Dieselbe Grundregel trägt auch das Review-Verfahren — `REVIEW_PROTOCOL.md` §1.)*
- **Erstes Rot darf ein „laufendes Rot" sein:** neuen Typ/Member als `NotImplementedException`-Stub anlegen,
  damit das Test-Assembly kompiliert und die Tests *laufen* und scheitern (klarer als ein Compile-Fehler).
- **Aktuell testen wir NUR neuen Code.** Bestandscode nachzutesten ist eine separate, aufgeschobene Aufgabe
  (BACKLOG) — nie still mit reingezogen.
- **Ehrliche Tests gewinnen Design-Trade-offs.** Wenn die Wahl steht zwischen einer leicht-ehrlich-testbaren
  Architektur (Seam/Interface → EditMode-testbar mit Fake) und einer ohne Abstraktion (nur per
  langsamem/vagem PlayMode prüfbar): die testbare wählen. Ein kleiner Seam ist es wert. Die daraus
  entstandene pure Logik-Schicht ist in [`ARCHITECTURE.md`](ARCHITECTURE.md) §2 gelistet — dort ist ihre
  Single Source, hier bewusst keine zweite Kopie.
- **Die Testgrenze verläuft am Assembly, nicht am Ordnernamen.** Getestet wird, was im **Runtime-Assembly**
  (`AudioFramework`) *entscheidet*. Zwei Sorten Code außerhalb davon werden bewusst **nicht** getestet
  *(entschieden 2026-08-14, für beide Sorten mit derselben Begründung)*:
  - **Editor-Präsentation** (`AudioFramework.Editor`, `includePlatforms: ["Editor"]`) — zustandslos, schreibt
    ausschließlich über `SerializedProperty`, strukturell aus jedem Build ausgeschlossen.
  - **Szenen- und Demo-Glue** (`Assets/Scripts/`, z. B. `TestScript`, `CategoryVolumeSliderBinding`, später die
    Showcase-Szene) — verzweigungsfreies Durchreichen an die öffentliche API, im EditMode nicht instanziierbar.

  Beide scheitern **sichtbar statt still**, und ein Test darauf wäre ein reiner Change-Detector. Das Weglassen
  ist deshalb **keine Ausnahme** von dieser Disziplin, sondern ihre korrekte Anwendung.
  - **Was die Grenze trägt — die Auflage:** Entsteht in solchem Code je eine echte Entscheidung (etwas rechnet
    oder verzweigt), gehört **diese Entscheidung** als pure Einheit ins Runtime-Assembly und wird dort
    test-first gebaut. Die Regel schützt nur so lange, wie außerhalb wirklich nur Kabel liegt.

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
**(b)** bewusste Spec-Änderung mit dem Go des Maintainers, **(c)** obsolet durch Umbau → BACKLOG (Loop-Regel #6). Passt
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
>   - **Spec richtig → latenter Bug im Bestandscode gefunden.** STOP, an den Maintainer melden.
>   - **Code richtig, Spec war naiv** → Spec mit dem Maintainer schärfen, das *Warum* verstehen und neu
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
   eingefroren. (Der Maintainer committet.)

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
> **Audit-Stand: 2026-08-15 — vollständig.** Erhoben über **alle** EditMode-Tests: **165 Tests in 27
> Testklassen** (+ 3 Test-Doubles), die **23 Produktions-Einheiten** abdecken. Damit gibt es erstmals seit
> dem Erst-Audit (2026-06-15) **keine unauditierte Restmenge** mehr; die frühere Tabelle „Noch nicht
> auditiert" ist entfallen, weil sie leer wäre.
>
> **Verifizierte Grundlage:** Batchmode-Lauf gegen Unity `6000.3.6f1` am 2026-08-15 —
> **165/165 grün, 0 rot, 0 übersprungen**, Laufzeit 0,13 s. Jede Bewertung unten steht auf diesem Lauf,
> nicht auf Vermutung.
>
> **📌 Numerierung ist bewusst stabil.** Die Einträge **#1–#12** behalten ihre Nummern aus dem Erst-Audit,
> weil **Teil I §5 namentlich auf sie verweist** (`#3`, `#5`, `#6`, `#7/#11 vs. #12`, `#8/#9`, `#10`,
> `#12 Stolperstein 2`). Neu auditierte Einheiten hängen ab **#13** hinten an. Wer hier umnummeriert, bricht
> die verbindliche Checkliste in Teil I.
>
> **📌 Diese Datei nimmt keine Aufgaben auf.** Befunde, aus denen Arbeit folgt, gehören in
> [`BACKLOG.md`](BACKLOG.md) (Regel aus [`CLAUDE.md`](CLAUDE.md)); hier stehen nur Messung und Bewertung.

## Bewertungsraster

Jede Methode wurde gegen vier Kriterien geprüft:

1. **Spec-abgeleitet vs. Code-abgeschrieben** — kommen die Erwartungswerte aus dem Vertrag oder aus der
   Implementierung? (Der zentrale Tautologie-Test.)
2. **Mutation-Resistenz** — würde ein bewusst eingebauter Fehler wirklich einen Test rot machen? Insb.: wird
   *jeder* Guard/Branch durch einen Test gepinnt, dessen Erwartung sich vom „allgemeinen Pfad" unterscheidet?
3. **Branch-/Grenzwert-Abdeckung** — alle Pfade + Ränder (off-by-one, `==`-Grenze, null/leer)?
4. **Lücken** — was wird *nicht* geprüft, das geprüft gehören sollte?

**Noten:** **Exzellent** · **Stark** · **Solide** · **Dünn (mit Lücke)**.
Keine Note „mangelhaft" vergeben — es gibt in dieser Suite keinen schlechten Test. Wo „Dünn" steht, ist
**die Abdeckung** dünn, nicht die Qualität der vorhandenen Tests.

## Gesamturteil (zuerst, ehrlich)

- ✅ **Kein tautologischer / Change-Detector-Test gefunden — jetzt über alle 165.** Kein Test liest die
  eigene Ausgabe der Implementierung zurück. Der dünnste Fall (`LowPassDispatchPolicy`) pinnt immerhin eine
  *Entscheidung*.
- ✅ **Spec-first-Disziplin nahezu lückenlos gelebt: 26 von 27 Testklassen** tragen die „hand-derived, NOT
  read off implementation"-Klausel im Klassen-`<summary>`, und die Zahlenwerte sind unabhängig nachrechenbar.
  **Die eine Ausnahme** ist `AudioFadeServicePauseTests`: der Header nennt die Spezifikation sauber („a fade
  on a paused slot is FROZEN"), trägt aber die Pflicht-Boilerplate aus Teil I §5 nicht. Inhaltlich kein
  Mangel, formal die einzige Abweichung von der Checkliste.
- ✅ **Grenzwert-Disziplin bleibt die Stärke der Suite.** Die kritischen Ränder sind mit *benachbarten
  true/false-Paaren* gepinnt — und der neue Bestand hält das Niveau: `WallCheckContinuation` (`<`-Grenze des
  Busy-Fensters), `OcclusionRangeValidation.Advise` (beide Schwellen als Paar), `OcclusionRangeValidation`
  (`==`-Fall gegen die schmalstmögliche gültige Spanne).
- ✅ **Neues stärkstes Muster: der Reihenfolge-Diskriminator.** Mehrere Tests beweisen nicht nur *was*
  gefiltert wird, sondern *wann* der Filter relativ zur Aggregation greift — `ActiveTriggerBeatsSmallerInactive`
  (#16), `GenerationMismatch_OverridesPaused` (#14), `DuplicateWithZeroAfterAudibleFirst` (#22),
  `SilentSlotDoesNotStopLaterSlotsFromBeingWritten` (#19). Das ist die wertvollste Test-Sorte der Suite.
- ⚠️ **Die alten minor-Schwächen sind nicht verschwunden — teils haben sie sich reproduziert:**
  (a) „Guard ohne echten Pin" ist in `DuckEnvelope` (#15) **exakt** so wiedergekommen wie in
  `OcclusionSmoothing` (#6) — obwohl die Lehre daraus schon in Teil I §5 stand;
  (b) **kein einziges `LogAssert.Expect` in der gesamten Suite**, während seit dem Erst-Audit ein *weiterer*
  Warn-Log dazugekommen ist (#9);
  (c) die Operator-Äquivalenz `|` vs. `+` (#10) ist bei `Advise` (#25) neu aufgetreten — beide Male
  praktisch folgenlos.
- ⚠️ **Der eine echte Ausreißer: `DuckFactorLedger` (#18).** Die einzige **zustandsbehaftete** pure Einheit
  trägt mit **2 Tests** die dünnste Abdeckung im Verhältnis zu ihrer Oberfläche. Beide Tests sind gut; um sie
  herum ist viel ungepinnt. Das ist das Muster des ganzen Audits: die Suite ist exzellent bei puren
  Funktionen und am dünnsten dort, wo **Zustand** wohnt.

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
| 9 | `…Provider.FillDictionaryWithKeysAndValues` | 4 | **Stark** | keep-first gepinnt; **zwei** unbestätigte Warn-Logs (neu erhoben) |
| 10 | `WallLayerMask.FromLayers` | 4 | **Stark** | `\|` vs `+` nicht unterscheidbar (praktisch moot bei eindeutigen Keys) |
| 11 | `PoolSlotAvailability.IsFree` | 5 | **Exzellent** | Jede AND-Klausel einzeln gepinnt + `==`-Grenze inklusiv |
| 12 | `VolumeResolver.Resolve` | 8 | **Stark** | Alle 3 Faktoren einzeln + Operator gepinnt; oberer Clamp prinzip-bedingt nur an *einem* Punkt |
| 13 | `ListenerCachePolicy.NeedsResolve` | 3 | **Exzellent** | Erschöpfend über die *erreichbare* Domäne; `\|\|`→`&&` wird gefangen |
| 14 | `WallCheckContinuation.ShouldContinue` | 9 | **Exzellent** | Jede Klausel + die **Klausel-Reihenfolge** + `<`-Grenze als Paar |
| 15 | `DuckEnvelope.Step` | 8 | **Stark** | Attack/Release-Wahl beidseitig gepinnt; `maxStep<=0` ist die #6-Lücke im Zwilling |
| 16 | `DuckTargetPolicy.ResolveDuck` | 8 | **Exzellent** | Alle drei `continue`-Guards einzeln + Aktiv-Filter-vor-Min gepinnt |
| 17 | `DuckRuleFlattening.Flatten` | 8 | **Stark** | `Clear()` + null-`Targets` + Ordnung gepinnt; **`rules == null` dokumentiert, aber ungetestet** |
| 18 | `DuckFactorLedger` | 2 | **Dünn (mit Lücke)** | Einzige zustandsbehaftete Einheit, dünnste Abdeckung; Attack-Pfad nie durchlaufen |
| 19 | `AudioVolumeWriteService.Apply` | 9 | **Exzellent** | `continue`-statt-`break` + Per-Slot-Kategorie + fehlende Quelle = 1.0 |
| 20 | `CategoryVolumeSource.For` | 7 | **Exzellent** | `ConfiguredZero` und die zwei Live-Read-Tests sind ideale Diskriminatoren |
| 21 | `CategoryVolumeWriter.Set` | 7 | **Stark** | Beide Clamps + beide Outcomes + Reihenfolge-Pin (`ContainsKey` **vor** dem Schreiben) |
| 22 | `CategoryVolumeCoverage.Evaluate` | 8 | **Stark** | Duplikat-`continue` und Dedup einzeln gepinnt, Enum-Ordnung gepinnt |
| 23 | `DuckConfigValidation.Evaluate` | 4 | **Stark** | Vollständige 2×2-Wahrheitstafel; Domäne selbst winzig (Wert pro Test wie #4) |
| 24 | `OcclusionRangeValidation.Evaluate` | 4 | **Stark** | `==`-Fall gegen 1-Hz-Spanne — echtes Nachbarpaar an einer Diskontinuität |
| 25 | `OcclusionRangeValidation.Advise` | 8 | **Exzellent** | Beide Schwellen als true/false-Paar **und** beide Zweige des Vorrang-Guards gepinnt |

**Summe:** 80 (#1–#12) + 85 (#13–#25) = **165** — deckt sich mit dem gemessenen Lauf.

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
Verteilt auf drei Dateien (7 + 3 + 3 = 13 Tests), damit die eingefrorenen Suiten wörtlich unangetastet blieben.

- **Spec-abgeleitet:** Ja (lineare Kurve, Zahlen aus #1).
- **Mutation — mehrere *echt* diskriminierende Tests:**
  - **Clobber-Guard** (`ClearFade_…_ClobberGuard`): nach `ClearFade` externe Volume-Setzung, `Tick` darf sie **nicht** überschreiben. Fängt fehlendes `Active=false`. ✔ (Sicherheitskern!)
  - **„Nach Settle nicht erneut schreiben"**: fängt, wenn der Fade nach Abschluss nicht deaktiviert wird. ✔
  - **`StopOnEnd`-Zweig:** `StopCallCount` unterscheidet FadeIn (0) von FadeOut (1). ✔
  - **Pause-Freeze:** `Tick_PausedThenResumed_ResumesFromWhereItWas` fährt 0.5 → (pausiert) 0.5 → 1.0. Echte Diskriminierung des `IsPaused`-Short-Circuits; ein fehlerhafter `Tick` liefe hier auf 1 durch. ✔
  - **`Tick_PausedFadeOut_DoesNotCompleteOrStop`** prüft zusätzlich, dass der Freeze auch den `Stop()`-Seiteneffekt zurückhält (`StopCallCount == 0` trotz `Tick(5f)` bei Dauer 2). ✔
  - **`StartFadeOut` startet bei aktueller Lautstärke** (0.8), nicht hardcodiert 1. ✔
- **Lücken (minor, unverändert seit 2026-06-15):** (a) `StartFade` auf einem bereits aktiven Slot (Neustart) ungetestet; (b) **zwei gleichzeitig aktive Fades** auf verschiedenen Slots, die zusammen voranschreiten — der „DoesNotTouchInactiveSlots"-Test hat 2 Slots, aber nur einen aktiven.
- **Formale Abweichung (2026-08-15):** `AudioFadeServicePauseTests` ist die **einzige** der 27 Testklassen ohne die Spec-first-Pflicht-Boilerplate aus Teil I §5. Die Spezifikation steht im Header, nur der Standardsatz fehlt.

> **Lehre:** Das `FakeFadeTarget`-Muster ist die **Blaupause für Gruppe B** (Unity-gekoppelt): kleines
> Recording-Double hinter ein Interface, dann am Double assert. Genau so für `ShouldContinueLoop`,
> `Gate`/`ResolveVolume`, `PauseAll`/`UnpauseAll`.

> ⚠️ **Nachtrag 2026-07-27, 2026-08-15 erneut bestätigt (Kontext, kein Audit-Befund):** Seit dem
> Ducking-Umbau schreibt `PooledFadeTarget.Volume` den **Per-Slot-Fade-Faktor** statt direkt `source.volume`
> ([`ARCHITECTURE.md`](ARCHITECTURE.md) §6). Die hier auditierten Tests blieben davon **unberührt und grün** —
> sie testen die Rampen-Mathematik über `FakeFadeTarget`, also agnostisch dazu, worauf `Volume` physisch
> zeigt. Genau dafür war der Seam da; ein Lehrstück, warum Loop-Regel #6 hier nicht greifen musste.
> *(Der `///`-Summary von `PooledFadeTarget` nennt allerdings weiterhin `AudioDuckService` als Besitzer von
> `source.volume` — seit dem Writer-Umbau falsch. Doku-Drift im Produktionscode, kein Testbefund.)*

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
- **Spec-abgeleitet:** Ja; alle Erwartungswerte aus der Dämpfungs-/Asymptote-Gleichung hand-abgeleitet (`Open=22000`, `Floor=1000`), nicht aus dem Code. Nachgerechnet 2026-08-15: 11500 · 6250 · 3940 (beide Reihenfolgen) · −9500→1000 stimmen exakt.
- **Mutation:** `−`→`+` in `ApplyWall`. **5 Tests rot** (Fraktion, Voll-Dämpfung, Akkumulation, Reihenfolge, Komposit), `ZeroDamping` bleibt grün (`+0 == −0`, pinnt bewusst den Wert, nicht den Operator), beide `ClampToFloor` grün. Mutation gefangen → Suite schützt den Vertrag. ✔
- **Lücke aus der Vorversion geschlossen:** Der „viele Wände → Floor"-Komposit-Pfad ist jetzt als Kette getestet (`ApplyWall_ThenClampToFloor_OverDampedConfigRescuedToFloor`: `d=1.5` → −9500 → Clamp → 1000).
- **Toleranz:** einzige Klasse mit `Delta = 1e-2f` statt `1e-5f`; die Begründung steht als bewusster
  Ausnahme-Kommentar (Kategorie „Magic Number ohne andere Heimat") direkt an der Konstante und ist fachlich
  hergeleitet (Hz-Domäne, JND), nicht per Reflex gesetzt. Vorbildlich.

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
- **Lücke (minor, 2026-08-15 erneut nachgerechnet und bestätigt):** **`maxStep<=0`-Guard ist nicht echt gepinnt.** `Step_ZeroDeltaTime` liefert 100 — aber auch *ohne* diesen Guard ergäbe der allgemeine Pfad `100 + 0` = 100. Der Guard schützt eigentlich **negatives** `dt`/`speed` — und genau die sind ungetestet. Der Test „beweist" hier also weniger, als er suggeriert.
- **Zweite (schwächste) Stelle:** `Step_AlreadyAtTarget_StaysPut` ist der Neutralfall-Test dieser Einheit —
  er dokumentiert, unterscheidet aber keinen Mutanten (vgl. `AllNeutral` in #12).

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

### 9 — `AudioManagerDictionaryProvider.FillDictionaryWithKeysAndValues` → **Stark** *(neu erhoben 2026-08-15)*
**Vertrag:** `CategoryVolume.Category → Volume` aus einer `IReadOnlyList<CategoryVolume>`; null **und** leer
sind derselbe Guard (mit Warnung, Dictionary bleibt leer); Duplikat = keep-first (mit Warnung).

> Diese Einheit hat sich mit dem Volume-Umzug in die `AudioSystemConfig` (2026-08-14) verändert; der Eintrag
> ist deshalb **neu geschrieben**, nicht fortgeschrieben. Die frühere Bewertung „Exzellent" beruhte auf dem
> **null-Eintrag-vor-gültigem-Eintrag**-Diskriminator und auf einem `TearDown` mit `DestroyImmediate` —
> **beides existiert nicht mehr:** `CategoryVolume` ist ein Struct (ein Element kann nicht null sein), und die
> Tests erzeugen keine ScriptableObjects mehr, weshalb es in der ganzen Suite kein `SetUp`/`TearDown` mehr
> gibt. Aus 6 Tests wurden 4. Die Note fällt von **Exzellent** auf **Stark** — nicht, weil Tests schlechter
> wurden, sondern weil der stärkste Diskriminator mit dem Datentyp entfallen ist.

- **Spec-abgeleitet:** Ja.
- **Mutation:** keep-first gepinnt (`DuplicateCategory_KeepsFirstValue`, 0.3 vs 0.9): mit `dict[key] = value`
  statt `TryAdd` käme 0.9. ✔ Beide Zweige des zusammengefallenen Guards einzeln getestet (null / leer). ✔
  Der Kategorie→Volume-Zuschnitt ist über zwei verschiedene Kategorien in einem Aufruf gepinnt. ✔
- **Lücke (minor, aber gewachsen):** Diese Methode loggt inzwischen **zwei** verschiedene Warnungen (Duplikat
  *und* „No category volumes configured … Every category plays at full volume") — **keine** davon ist mit
  `LogAssert.Expect` bestätigt. Für ein verkauftes Asset ist gerade die zweite Meldung reine UX: sie ist der
  einzige Hinweis, den ein Erstnutzer mit leerer Config je bekommt.

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
- **Querbezug (2026-08-15):** `WallCheckContinuation` (#14) pinnt dieselbe Busy-Fenster-Grenze aus der
  *Gegenrichtung* (`currentTime < busyUntilTime` → weiterlaufen). Beide Seiten der Komplementärbedingung sind
  damit unabhängig festgenagelt — eine Verschiebung des Vergleichs in nur einer der beiden Einheiten würde
  auffallen.

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
2. **Die `1.5`-Falle (vom Maintainer gefangen).** Der „Fix" für Stolperstein 1 war, einen Test mit Input `1.5`
   zu ergänzen. Die *Assertion* (1.5 → 1) ist spec-konform — aber den **Wert 1.5 hatte ich rückwärts aus dem
   Mutanten** konstruiert. Das ist genau die Code-getriebene Tautologie: nicht nur die Assertion, auch der
   **Input** muss aus der Spec kommen. Test **verworfen**, nicht aufgenommen.

> **Lehre:** **Neighboring-Pair-Pinning (#7, #11) wirkt nur an Diskontinuitäten** (in/out-of-bounds →
> true/false). An einer **kontinuierlichen** Grenze (Clamp bei 1.0) stimmen beide Seiten überein → die
> Schwelle ist grundsätzlich nicht eng pinnbar; korrekt ist „Existenz + ein Repräsentant", nicht „Schwelle".

---

> **Ab hier: erstmals auditiert (2026-08-15).** Die Einheiten #13–#25 sind seit dem Erst-Audit entstanden und
> waren bis dahin nur als offene Erhebung gelistet. Alle sind regulär im TDD-Loop mit Mutation Check
> entstanden; die Bewertung unten ist eine **unabhängige Nachprüfung**, kein Nachvollziehen der
> Entstehungs-Protokolle.

### 13 — `ListenerCachePolicy.NeedsResolve(hasCached, isAliveAndActive)` → **Exzellent**
**Vertrag:** neu auflösen ⟺ kein Cache **ODER** der gecachte Listener ist nicht mehr lebend & aktiv.

- **Spec-abgeleitet:** Ja; der Header benennt zusätzlich, *welche* Zeile der Wahrheitstafel fehlt und warum:
  `(hasCached: false, isAliveAndActive: true)` ist **unerreichbar** (lebend & aktiv setzt eine Referenz voraus).
  Das ist die saubere Art, eine bewusste Nicht-Abdeckung zu dokumentieren — nicht schweigen, sondern begründen.
- **Mutation:** `||`→`&&` kippt genau `CachedButStale` (true → false). ✔ Weglassen des zweiten Operanden kippt
  denselben Test; Weglassen des ersten kippt `NoCachedListener`. ✔ Jede Klausel einzeln gepinnt.
- **Abdeckung:** 3 von 4 Zeilen — und die vierte ist **erschöpfend**, weil unerreichbar. Mehr geht nicht.
- **Lücke:** keine. Die Einheit ist klein, aber die Tests holen alles heraus, was die Domäne hergibt.

### 14 — `WallCheckContinuation.ShouldContinue(...)` → **Exzellent**
**Vertrag (in dieser Reihenfolge):** Generation ≠ → `false` · pausiert → `true` · OneShot →
`isPlaying || currentTime < busyUntilTime` · sonst → `isPlaying`.

- **Spec-abgeleitet:** Ja, der Klassen-Header schreibt den Vertrag als **nummerierte Reihenfolge** hin — und
  genau diese Reihenfolge wird getestet, nicht nur die Einzelklauseln.
- **Mutation — jede Klausel *und* ihre Position ist gepinnt:**
  - **Generation-Guard:** `GenerationMismatch_DoesNotContinue`. ✔
  - **Reihenfolge Generation VOR Pause:** `GenerationMismatch_OverridesPaused` (Mismatch **und** pausiert →
    false). Stünde der Pause-Check oben, käme `true`. ✔✔ **Der wertvollste Test der Einheit** — er pinnt eine
    Eigenschaft, die man beim Umsortieren zweier `if`-Zeilen versehentlich zerstört.
  - **Pause-Klausel:** `Matched_Paused_Continues` ist bewusst so gefüttert, dass *alle anderen* Pfade `false`
    ergäben (OneShot, still, Busy-Fenster abgelaufen). Ohne die Pause-Zeile: `false ≠ true` → rot. ✔✔
    Genau der Pin, der in #6 fehlt.
  - **OneShot-Zweig:** `OneShot_Silent_BusyWindowOpen_Continues` (still, 3 < 5 → true) fiele ohne den
    OneShot-Zweig auf `isPlaying == false` zurück → rot. ✔
  - **`<`-Grenze als Nachbarpaar:** `BusyWindowOpen` (3<5 → true) **+** `CurrentTimeEqualsBusyUntil` (5<5 →
    false). Mit `<=` würde der Gleichheitsfall durchrutschen. ✔✔
  - **Loop-Zweig:** beide Ausgänge (`Playing` → true, `NotPlaying` → false). ✔
- **Lücke:** keine nennenswerte. Zusammen mit #11 die vorbildlichste Prädikat-Abdeckung der Suite.

### 15 — `DuckEnvelope.Step(current, target, dt, attackRate, releaseRate)` → **Stark**
**Vertrag:** MoveTowards mit **asymmetrischer** Rate — `attackRate` beim Tiefer-Ducken (Faktor fällt),
`releaseRate` beim Erholen (Faktor steigt); nicht-positive gewählte Rate → sofort `target`; kein Overshoot.

- **Spec-abgeleitet:** Ja, alle Werte hand-nachrechenbar (`1 − 4·0.1 = 0.6`, `0.2 + 1·0.1 = 0.3`).
- **Mutation:**
  - **Richtungsabhängige Ratenwahl — beidseitig gepinnt:** `DuckingDeeper` (0.6 mit attack 4) und `Recovering`
    (0.3 mit release 1). Wären die Raten vertauscht, kippen **beide**. ✔✔ Das ist der Kern des Vertrags und
    er ist sauber festgenagelt.
  - **Snap bei nicht-positiver Rate — für beide Richtungen einzeln:** `ZeroAttackRate` und `ZeroReleaseRate`.
    Ohne den Guard liefe man in `maxStep == 0 → current` und bekäme den *Start*wert statt des Ziels. ✔✔
  - **Kein Overshoot, in beide Richtungen:** `WithinOneStepDeeper` (0.5→0.4 statt 0.1) und
    `WithinOneStepRecovering` (0.5→0.55 statt 0.6). ✔
- **⚠️ Lücke — die Wiederkehr von #6:** **`maxStep <= 0` ist nicht echt gepinnt.** `Step_ZeroDeltaTime`
  erwartet 1.0; ohne den Guard liefert der allgemeine Pfad `1.0 + (−0)` = **ebenfalls 1.0**. Der Guard
  schützt negatives `dt` — und das ist ungetestet. **Bemerkenswert:** Die Lehre aus #6 stand zum
  Entstehungszeitpunkt von `DuckEnvelope` (2026-06-28) bereits in Teil I §5, und der Zwilling hat die Lücke
  trotzdem 1:1 geerbt. Das ist der stärkste Beleg des ganzen Audits dafür, dass die Checkliste beim
  **Kopieren einer bestehenden Einheit** aktiv abgehakt werden muss und nicht von allein greift.
- **Lücke (minor):** `Step_AlreadyAtTarget_StaysPut` ist der Neutralfall-Test — dokumentiert, diskriminiert
  nichts (vgl. #6, #12c).

### 16 — `DuckTargetPolicy.ResolveDuck(target, activeCategories, pairs)` → **Exzellent**
**Vertrag:** über alle Paare, die *dieses* Ziel treffen, deren Trigger ≠ Ziel ist (kein Selbst-Duck) und deren
Trigger **aktiv** ist: der **stärkste** Duck gewinnt (`min` der Faktoren). Kein solches Paar → 1.0.

- **Spec-abgeleitet:** Ja.
- **Mutation — alle drei `continue`-Guards einzeln gepinnt, jeweils mit einem Input, an dem das Weglassen
  einen *anderen* Wert erzeugt:**
  - `pair.Target != target` → `PairTargetsOtherCategory` (ohne Guard käme 0.4 statt 1.0). ✔
  - `pair.Trigger == target` → `SelfDuckSkipped` (ohne Guard käme 0.3 statt 1.0). ✔
  - `!IsActive(...)` → `PairTriggerNotActive` / `NoActiveTriggerForTarget` (ohne Guard käme 0.5). ✔
  - **`min`-Stacking:** `TwoActiveTriggers_ReturnsMinimum` (0.5 vor 0.8) — ein `>` statt `<` kippt ihn. ✔
- **Der Reihenfolge-Diskriminator:** `ActiveTriggerBeatsSmallerInactive` (aktiv 0.6 vs. inaktiv 0.3 → 0.6)
  beweist, dass **erst gefiltert, dann minimiert** wird. Würde jemand die Aktiv-Prüfung hinter das Minimum
  ziehen (der naheliegende „Optimierungs"-Fehler), käme 0.3. ✔✔ Zusammen mit `GenerationMismatch_OverridesPaused`
  (#14) die beste Sorte Test in dieser Suite.
- **Lücken (minor, außerhalb der zugesagten Domäne):** `pairs`/`activeCategories` als `null` → NRE, wird vom
  Vertrag aber nicht zugesagt. Ein `DuckedVolume > 1` (Config könnte theoretisch „lauter" schreiben) wird von
  `< result` stillschweigend ignoriert; durch `[Range(0,1)]` auf `DuckTarget.DuckedVolume` praktisch moot —
  dieselbe Kategorie „durch Serialisierung unerreichbar" wie das `|`/`+` in #10.

### 17 — `DuckRuleFlattening.Flatten(rules, results)` → **Stark**
**Vertrag:** `results` leeren, dann pro `(rule.Trigger, target.Category, target.DuckedVolume)` **ein** Paar
anhängen, Regel- dann Ziel-Reihenfolge erhalten; null-Regelliste und null-`Targets` tragen nichts bei;
**kein** Filtern von Selbst-Zielen oder Duplikaten (das gehört der Policy).

- **Spec-abgeleitet:** Ja.
- **Mutation:**
  - **`results.Clear()`** ist explizit gepinnt (`ResultsList_IsClearedBeforeFill`: ein Fremdeintrag vorher →
    Count muss 1 sein, nicht 2). ✔✔ Genau die Zeile, die man beim „Fill-Stil" vergisst.
  - **Ordnung** doppelt gepinnt — über Ziele *innerhalb* einer Regel und über Regeln hinweg. ✔
  - **`targets == null`-Guard:** `RuleWithNullTargets_Skipped` (ohne Guard NRE). ✔
  - **„Dummer Transform":** `SelfTargetAndDuplicates_PassThroughUnfiltered` pinnt, dass hier **nicht**
    gefiltert wird — schützt die Einzigkeit der Filter-Regeln in #16 gegen ein „hilfsbereites" Vorfiltern. ✔✔
- **⚠️ Lücke (konkret, nicht inhärent):** **Der zugesagte `rules == null`-Pfad ist ungetestet.** Sowohl der
  `///`-Summary der Methode („A null rule list … contributes nothing (no exception)") als auch der
  Klassen-Header der Tests nennen ihn ausdrücklich — es gibt aber keinen Test, der `Flatten(null, results)`
  aufruft. Entfernt man `if (rules == null) return;`, wirft die Methode eine NRE und **die Suite bleibt grün**.
  `RuleWithNoTargets_ContributesNothing` deckt das *nicht* ab: `Rule(SFX)` erzeugt über `params` ein **leeres**
  Array, nicht `null`. Das ist die einzige Stelle im Audit, an der ein **schriftlich zugesagter Vertragsteil**
  gar keinen Pin hat.

### 18 — `DuckFactorLedger` (`Step`, `ReleaseAll`, `FactorFor`) → **Dünn (mit Lücke)**
**Vertrag:** Eine Kategorie wird geführt, solange sie konfiguriertes Duck-Ziel **oder** noch in Erholung ist;
pro Frame glidet jede geführte Kategorie (`DuckEnvelope`) auf das von `DuckTargetPolicy` aufgelöste Ziel zu;
bei 1.0 wird sie pensioniert. `ReleaseAll` fährt mit den Raten des **letzten** `Step` aus.

- **Spec-abgeleitet:** Ja — beide Erwartungswerte sind sauber hand-nachrechenbar und wurden 2026-08-15
  unabhängig nachgerechnet: `Step 1` snappt über `attackRate 0` auf 0.5, `Step 2` glidet mit `release 0.25 · 1.0 s`
  auf **0.75** ✔; `ReleaseAll(0.5 s)` mit gemerktem `release 0.4` ergibt `0.5 + 0.2` = **0.7** ✔.
- **Die zwei vorhandenen Tests sind gut** und treffen genau die zwei Bugs, wegen derer die Einheit entstand
  (Einfrieren statt Ausblenden bei entfallener Regel; Zurückschnappen bei verlorener Config). Sie pinnen
  echtes Frame-zu-Frame-Verhalten, nicht nur einen Rückgabewert.
- **Bewusst nicht getestet — und das ist korrekt:** das **Retire bei 1.0**. Durch die öffentliche Oberfläche
  ist „pensioniert" von „mitgeführt bei 1.0" nicht unterscheidbar (`FactorFor` liefert beide Male 1.0); ein
  Test müsste die Dictionary-Größe exponieren und wäre ein Change-Detector. Entscheidung und Begründung
  stehen im BACKLOG; das Audit **bestätigt sie unabhängig** (echter Äquivalenz-Mutant).
- **⚠️ Was tatsächlich ungepinnt ist — und die Note trägt:**
  - **Der `attackRate`-Pfad wird nie durchlaufen.** Beide Tests setzen `attackRate: 0f` und nehmen damit den
    Snap-Pfad von `DuckEnvelope`. Ein *gleitendes Tiefer-Ducken* über Frames — das hörbare Kernverhalten des
    Features — kommt in dieser Einheit nirgends vor.
  - **`FactorFor`-Rückfall auf 1.0 für eine nicht geführte Kategorie ist nicht gepinnt.** Beide Assertions
    fragen `Music` ab, *nachdem* es im Dictionary liegt; ein `: 0f` statt `: 1f` bliebe grün. Das ist
    dieselbe Sorte „Guard ohne echten Pin" wie #6/#15.
  - **`trackedCategories`-Dedup ist nicht gepinnt.** Zwei Paare auf dasselbe Ziel würden es doppelt steppen
    (doppelte Glide-Geschwindigkeit); die Tests haben nie mehr als ein Paar.
  - **`trackedCategories.Clear()` ist an den vorhandenen Inputs ein Äquivalenz-Mutant** — die
    `Contains`-Prüfungen darunter verhindern das Doppeltanhängen ohnehin, solange nur *eine* Kategorie
    im Spiel ist. Mit einem zweiten Ziel wäre es ein echter Pin.
  - **Nur eine Kategorie im gesamten Test.** Es gibt keinen Beleg, dass zwei Kategorien unabhängig
    voneinander geführt werden.
  - **`lastAttackRate` wird nie geprüft** (nur `lastReleaseRate` über `ReleaseAll`).
- **Einordnung, ehrlich:** Die Einheit ist die **einzige zustandsbehaftete** der puren Schicht und die
  einzige mit einer *Historie über Frames*. Genau dort ist Abdeckung am teuersten zu erreichen und am
  wertvollsten — und genau dort ist sie am dünnsten. Kein Test hier ist falsch; es fehlen welche.

### 19 — `AudioVolumeWriteService.Apply()` → **Exzellent**
**Vertrag:** Jeder **klingende** Slot bekommt `clamp01(basis · fade · duck)`, wobei `basis`/`duck` über die
Kategorie **dieses** Slots aufgelöst werden und `fade` sein eigener Faktor ist. Ein stiller Slot wird gar
nicht geschrieben. Eine fehlende Faktor-Quelle steuert 1.0 bei.

- **Getestet über `IVolumeTarget` + `FakeVolumeTarget`** — der Zwilling des `FakeFadeTarget`-Musters (#3),
  hier mit `WriteCount`, damit „gar nicht geschrieben" von „denselben Wert nochmal geschrieben" unterscheidbar ist.
  Das ist genau die Sorte Double, die Teil I §5 als Standardmuster verlangt.
- **`FakeCategoryFactorSource` liefert für unkonfigurierte Kategorien absichtlich `0` statt `1`** — damit ein
  falscher Kategorie-Lookup das Produkt auf null zieht und **laut** scheitert, statt durch ein neutrales 1.0
  hindurchzurutschen. **Herausragende Test-Doubles-Disziplin;** dieses Detail entscheidet, ob
  `SlotCategoryDecidesTheLookup` überhaupt etwas beweist.
- **Mutation:**
  - **Stiller Slot wird übersprungen:** `SilentSlot_IsNotWrittenAtAll` über `WriteCount == 0`. ✔
  - **`continue` statt `break`:** `SilentSlotDoesNotStopLaterSlotsFromBeingWritten` — ein stiller Slot **vor**
    einem klingenden. ✔✔ Das ist der direkte Nachfolger des mit dem Volume-Umbau entfallenen
    null-Eintrag-Diskriminators aus #9; das Muster ist also nicht verloren gegangen, sondern umgezogen.
  - **Kategorie-Zuordnung:** `SlotCategoryDecidesTheLookup` (SFX 0.9 statt Music 0.1) und
    `EachSlotIsResolvedWithItsOwnCategoryAndFade` (0.2 / 0.4 für zwei Slots in **einem** `Apply`). ✔✔
  - **Optionalität jedes Faktors einzeln:** fehlende Duck-Quelle (0.4), fehlende Basis-Quelle (0.25), gar
    keine Quelle (0.6). Jede der drei `?? 1f`-Stellen ist damit separat gepinnt. ✔✔
  - **Clamp:** `ProductAboveUnity_IsClampedToUnity` (basis 2 → 1). ✔
- **Lücken (minor):** `targets == null` ungetestet und ungeguardet (nicht zugesagt). Ein Slot, dessen
  `IsPlaying` *während* eines `Apply` kippt, ist ein Unity-Lebenszyklus-Thema und gehört in den PlayMode-Smoke.

### 20 — `CategoryVolumeSource.For(category)` → **Exzellent**
**Vertrag:** konfigurierte Kategorie → genau ihr Wert (**inklusive 0**); unkonfigurierte → 1.0; **ungeklemmt**
durchgereicht; **live** aus dem Dictionary gelesen, nie gesnapshottet.

- **Mutation — jede Vertragsklausel hat einen Test, der bei ihrem Wegfall einen *anderen* Wert liefert:**
  - **`ConfiguredZero_ReturnsZeroNotUnity`** ist der Kronjuwel dieser Einheit: er trennt „Wert 0" von
    „nicht konfiguriert". Eine naheliegende Fehlimplementierung (`value == 0 ? 1f : value`, oder ein
    `GetValueOrDefault` mit falschem Default) fällt genau hier — und **nur** hier. ✔✔
  - **Rückfall 1.0:** `UnconfiguredCategory` und `EmptyDictionary`. ✔
  - **Kein Clamp:** `ValueAboveUnity_IsReturnedUnclamped` (1.5). ✔ Pinnt eine **Design-Entscheidung**
    (Clampen gehört dem `VolumeResolver`) gegen späteres „Sicherheits"-Clampen an der falschen Stelle.
  - **Live-Read, zweifach:** Wert nach Konstruktion geändert (0.6 → 0.2) **und** Kategorie nach Konstruktion
    *hinzugefügt* (leeres Dictionary → 0.3). Eine Snapshot-Implementierung fiele bei beiden. ✔✔ Der zweite
    Test ist der schärfere: ein Snapshot der *Keys* würde ihn allein kippen.
- **Lücken:** `null`-Dictionary im Ctor → NRE, nicht zugesagt. Sonst nichts. Für eine Einheit dieser Größe
  die vollständigste Abdeckung der Suite.

### 21 — `CategoryVolumeWriter.Set(category, requested)` → **Stark**
**Vertrag:** speichert den Wert **auf `[0,1]` geklemmt** (Read-Back-Ehrlichkeit an der API-Grenze); eine
unbekannte Kategorie wird **angelegt**, nicht abgewiesen; der Unterschied wird als
`CategoryVolumeWriteOutcome` gemeldet.

- **Mutation:**
  - **Beide Clamps:** 1.5 → 1.0 und −0.5 → 0.0. ✔
  - **Beide Outcomes:** `Updated` bei vorhandenem, `EntryCreated` bei fehlendem Eintrag. ✔
  - **Reihenfolge-Pin (leicht zu übersehen, aber echt):** `UnconfiguredCategory_ReportsEntryCreated` pinnt,
    dass `ContainsKey` **vor** dem Schreiben ausgewertet wird. Zieht man die Abfrage hinter die Zuweisung, ist
    sie immer `true` und der Test kippt. ✔✔
  - **Anlegen ohne Kollateralschaden:** `UnconfiguredCategory_IsCreatedWithTheRequestedValue` schreibt SFX in
    ein Dictionary, das bereits Music enthält. ✔
- **Grenzen korrekt behandelt:** `BoundaryValues_ArePassedThroughUnchanged` (0 und 1) — die Schwelle selbst
  ist an dieser **kontinuierlichen** Grenze prinzip-bedingt nicht pinnbar (`clamp(0)=0`, `clamp(1)=1`), also
  ist „Existenz + Repräsentant" hier die *richtige* Wahl. Sauber angewandte Lehre aus #12.
- **Lücken (minor):** dass die *anderen* Einträge unverändert bleiben, wird nicht assertet (nur implizit).
  `NaN` läuft durch beide Vergleiche hindurch und wird gespeichert — außerhalb der zugesagten Domäne und über
  den Inspector (`[Range(0,1)]`) nicht erreichbar.

### 22 — `CategoryVolumeCoverage.Evaluate(configured, allCategories)` → **Stark**
**Vertrag:** meldet drei Arten von Fehlkonfiguration — **fehlend** (in Enum-Reihenfolge), **doppelt** (genau
einmal gemeldet, zählt als konfiguriert), **stumm** (Eintrag auf 0); bei einem Duplikat entscheidet der
**erste** Eintrag über die Stummheit (spiegelt keep-first aus #9). Null-Liste == leere Liste.

- **Mutation:**
  - **`continue` nach dem Duplikat-Fund:** `DuplicateWithZeroAfterAudibleFirst_IsNotSilent` (0.5 dann 0.0 →
    **nicht** stumm). Ohne das `continue` würde der zweite Eintrag die Stumm-Prüfung erreichen → Count 1. ✔✔
    Ein echter Reihenfolge-Diskriminator, und er koppelt die Inspector-Meldung korrekt an das
    **Laufzeit**verhalten (keep-first) statt an das, was im Inspector zuletzt steht.
  - **Dedup der Duplikat-Liste:** `CategoryListedThreeTimes_IsReportedOnce` — ohne
    `if (!duplicated.Contains(...))` käme 2. ✔
  - **Duplikat zählt als konfiguriert:** `RepeatedCategory…` assertet zusätzlich
    `Missing` enthält SFX **nicht**. ✔
  - **Enum-Reihenfolge:** `UnlistedCategories_AreMissingInEnumOrder` prüft nicht nur den Count, sondern die
    drei Positionen. ✔
  - **Null == leer:** beide Wege einzeln. ✔
- **Lücken (minor):** Eine **negative** Lautstärke gilt per `<= 0f` ebenfalls als „stumm", ist aber ungetestet
  — durch `[Range(0f,1f)]` auf `CategoryVolume.Volume` praktisch unerreichbar (Kategorie „moot wie #10").
  `allCategories == null` → NRE (nicht zugesagt). Der Fall „erster Eintrag 0, zweiter hörbar → stumm"
  (Spiegelbild des starken Tests oben) fehlt.

### 23 — `DuckConfigValidation.Evaluate(enabled, ruleCount)` → **Stark**
**Vertrag:** Schalter an ohne Regeln → `EnabledWithoutRules`; Regeln ohne Schalter → `RulesWithoutEnabled`;
die beiden übereinstimmenden Kombinationen → `None`.

- **Abdeckung: vollständige 2×2-Wahrheitstafel.** Mehr ist über dieser Domäne nicht erreichbar.
- **Mutation:** Die beiden Fehlerfälle sind **nicht vertauschbar** (verschiedene Enum-Werte) → ein Dreher
  kippt beide Tests. ✔ Die `> 0`-Schwelle ist über die Inputs 0 und 1 als Nachbarpaar gepinnt: mit `>= 0`
  wäre `hasRules` immer wahr und beide `None`-Tests kippen. ✔
- **Einordnung:** erschöpfend, aber die Domäne ist winzig — **Wert pro Test wie bei #4**. Die Note steht für
  „so gut, wie es hier geht", nicht für Tiefe. Kein Handlungsbedarf.

### 24 — `OcclusionRangeValidation.Evaluate(defaultCutoff, minCutoff)` → **Stark**
**Vertrag:** Boden **über** offenem Cutoff → `MinAboveDefault` (Occlusion invertiert); Boden **gleich**
offenem Cutoff → `MinEqualsDefault` (Occlusion inert); jede noch so schmale Spanne → `None`.

- **Mutation:**
  - **`==`-Grenze gegen die schmalstmögliche gültige Spanne:** `FloorEqualToOpenCutoff` (22000/22000 →
    `MinEqualsDefault`) **+** `NarrowestRange` (22000/21999 → `None`). Echtes Nachbarpaar an einer
    **Diskontinuität** — mit `>=` in der ersten Zeile käme `MinAboveDefault` statt `MinEqualsDefault`, der
    Test fängt es. ✔✔
  - **Beide Fehlermodi** liefern verschiedene Enum-Werte und sind einzeln gepinnt. ✔
- **Vertragstreue Schärfe:** Der `///`-Kommentar begründet, warum die Gleichheit **exakt** und nicht per
  `Mathf.Approximately` geprüft wird (beide Werte werden getippt) — und `NarrowestRange` ist genau der Test,
  der eine spätere „Verbesserung" auf Epsilon rot machen würde. Spec und Test greifen ineinander.
- **Bekannte Lücke (bereits im BACKLOG als P8):** Die *Regel* ist vollständig geprüft, die **ausgelieferten
  Feld-Defaults** von `AudioSystemConfig` sind es nicht. Hier nur als Verweis geführt, nicht doppelt eröffnet.

### 25 — `OcclusionRangeValidation.Advise(defaultCutoff, minCutoff)` → **Exzellent**
**Vertrag:** beschreibt eine **gültige, aber ungewohnte** Spanne über zwei unabhängige Flags — offener Cutoff
unter 20000 Hz → `OpenCutoffNotTransparent`; Boden über 200 Hz → `FloorLimitsMuffling`; **genau auf** der
Schwelle gilt der Wert noch als unauffällig; beide können zugleich gelten; eine Spanne, die `Evaluate` bereits
verwirft, bekommt **gar keinen** Rat.

- **Mutation — beide Schwellen als true/false-Nachbarpaar, beide an echten Diskontinuitäten:**
  - `OpenCutoffExactlyAtTransparent` (20000 → `None`) **+** `OpenCutoffBelowTransparent` (8000 → Flag).
    Mit `<=` statt `<` kippt der Gleichheitsfall. ✔✔
  - `FloorExactlyAtUnobtrusive` (200 → `None`) **+** `FloorAboveUnobtrusive` (5000 → Flag).
    Mit `>=` statt `>` kippt der Gleichheitsfall. ✔✔
- **Der Vorrang-Guard ist in *beiden* Zweigen einzeln gepinnt** — das ist die stärkste Eigenschaft dieser
  Einheit: `FloorAboveOpenCutoff` (100/22000) und `FloorEqualToOpenCutoff` (15000/15000) erwarten beide
  `None`, obwohl **beide Flags** zutreffen würden, wenn die Zeile
  `if (Evaluate(...) != None) return None;` fehlte (→ jeweils `3` statt `0`). ✔✔ Ein Guard, dessen Entfernung
  an *diesen* Inputs wirklich ein anderes Ergebnis liefert — genau das, was #6 und #15 fehlt. **Vorbild.**
- **Kombination:** `BothEndsMoved` (8000/5000 → beide Flags) pinnt, dass sich die Flags nicht gegenseitig
  ausschließen. ✔
- **Lücke (minor, moot — Wiederkehr von #10):** `|=` vs. `+=` ist bei disjunkten Flag-Bits (1 und 2) nicht
  unterscheidbar, und da jedes Flag höchstens einmal gesetzt wird, ist der `+=`-Mutant hier sogar
  **vollständig äquivalent**. Anders als in #10 gibt es keinen Input, der die beiden trennen könnte — also
  keine behebbare Lücke, sondern die Natur eines Flag-Aufbaus aus paarweise verschiedenen Bits.

---

## Struktur-Befunde (Suite-Ebene, nicht pro Methode)

### S1 — Die Testgrenze aus Teil I §3 und der `AudioFramework.Editor`-Bestand gehen auseinander

**Beobachtung, kein Regel-Vorschlag.** Teil I §3 nimmt die **Editor-Präsentation** ausdrücklich von der
Testpflicht aus und begründet das damit, dass sie „zustandslos" sei, „ausschließlich über `SerializedProperty`"
schreibe und dass außerhalb des Runtime-Assemblys „wirklich nur Kabel" liege. Diese Beschreibung trifft auf
`AudioSystemConfigEditor`, `AudioDataObjectEditor`, `AudioInspectorSkin` und `SingleLayerDrawer` zu. Auch
diese Dateien verzweigen — aber ausschließlich über **Darstellung** (Foldouts, Layout, Pluralisierung wie
`DescribeClipCount`), nie über ein **Urteil**. Genau an dieser Trennlinie verläuft der Befund.

Sie trifft **nicht** auf zwei Dateien im selben Assembly zu, deren Verzweigungen Urteile sind — *welcher*
Befund entsteht, mit *welcher* Severity, ab *welcher* Schwelle:

| Datei | Was darin entschieden wird |
|---|---|
| `Editor/AudioDataObjectInspectorModel.cs` (206 Z.) | `Validate` — 5 Befund-Regeln mit Severity · `Describe` — verzweigende Prosa (0/1/n Clips, Blend `<=0`/`>=1`/dazwischen) · `ToVariableName` — Zeichen-Algorithmus (camelCase, führende Ziffern, Leer-Rückfall) · `ClipLengthFormatter.Format` — drei Bereiche mit Rundung · `NeedsSpatialPlayback` — Prädikat |
| `Editor/AudioSystemConfigInspectorModel.cs` (174 Z.) | `Validate` — ~10 Befund-Regeln inkl. zweier `switch` über `OcclusionRangeIssue`/`DuckConfigIssue` · `Describe` — verzweigende Prosa · `Join` — Aufzählungs-Formatierung mit Sonderfall am letzten Element · `CoversEveryCategory` — Prädikat |

Das ist genau der Fall, den die **Auflage** in §3 benennt („Entsteht in solchem Code je eine echte
Entscheidung […], gehört diese Entscheidung als pure Einheit ins Runtime-Assembly"). Drei Belege, dass hier
nicht nur Kabel liegt:

1. **Beide Klassen behaupten selbst, testbar gebaut zu sein.** Ihre `///`-Summaries enden wörtlich auf
   „*so it can be unit tested*". Getestet wird keine von beiden.
2. **Sie sind aus dem Test-Assembly nicht einmal erreichbar:** beide Typen sind `internal` in
   `AudioFramework.Editor`, und `AudioFramework.Tests.EditMode.asmdef` referenziert nur `AudioFramework`.
   Das Fehlen der Tests ist damit **strukturell**, nicht das Ergebnis einer Abwägung pro Methode.
3. **Eine der beiden Selbstbeschreibungen stimmt nicht mehr:** `AudioSystemConfigInspectorModel` erklärt sich
   für „free of […] AudioFramework runtime types", benutzt aber `OcclusionRangeValidation`,
   `DuckConfigValidation`, `OcclusionDefaults` und die beiden Issue-Enums aus dem Runtime-Assembly.

**Kalibrierung, ehrlich:** Der *Schaden* ist begrenzt — falsche Prosa im Inspector ist sichtbar, nicht still,
und `AudioDataObjectInspectorModel` stammt aus dem als „Experiment, nicht Teil des Kanons" markierten
Inspector-Vorhaben ([`INSPECTOR_UI_EXPERIMENT.md`](INSPECTOR_UI_EXPERIMENT.md)).
`AudioSystemConfigInspectorModel` dagegen ist mit dem Config-Umzug (2026-08-14) als reguläre Arbeit
entstanden. Der Befund ist deshalb **keine Regelverletzung, die zu reparieren wäre**, sondern eine
Abweichung zwischen der *Begründung* der Grenze und dem, was heute hinter ihr liegt. **Was daraus folgt,
entscheidet der Maintainer** — Teil I bleibt davon unberührt, und eine daraus folgende Aufgabe gehört in den
[`BACKLOG.md`](BACKLOG.md), nicht hierher.

### S2 — Kein einziges `LogAssert` in der gesamten Suite

Über alle 27 Testklassen kommt **kein** `LogAssert.Expect` / `LogAssert.NoUnexpectedReceived` vor (ebenso
keine `SetUp`/`TearDown`, keine `TestCase`-Parametrisierung, keine `Ignore`/`Explicit`-Marker — die Suite ist
durchgehend „ein Test, ein Fall, kein Fixture-Zustand", was für sich genommen eine Stärke ist).

Die Konsequenz ist der Querschnitt-Befund von 2026-06-15, nur **größer** geworden: Die
Fehlkonfigurations-Meldungen des Tools — inzwischen mindestens drei (`FillLayerMaskDictionary…` Duplikat,
`FillDictionaryWithKeysAndValues` Duplikat **und** „No category volumes configured") — sind reines,
unbestätigtes Verhalten. Für ein verkauftes Asset ist die Diagnose-Meldung der einzige Kanal zwischen einer
stillen Fehlkonfiguration und dem Käufer.

### S3 — Der Zustand ist der blinde Fleck, nicht die Mathematik

Sortiert man die 23 Einheiten nach „wie viel Frame-zu-Frame-**Zustand** trage ich?", ergibt sich das
Audit-Ergebnis fast von allein:

- **17 zustandslose Funktionen** (statische Prädikate, Mathe, Transforms, Validierungen) → durchgehend
  **Stark**/**Exzellent**, keine einzige Ausnahme,
- **3 Einheiten über einem *fremden* Dictionary** (`CategoryVolumeSource` #20, `CategoryVolumeWriter` #21,
  `AudioManagerDictionaryProvider` #8/#9) → **Stark**/**Exzellent**: Der Zustand gehört ihnen nicht, der Test
  hält ihn selbst in der Hand und liest ihn direkt zurück,
- **2 Dienste mit Seam und Recording-Double** (`AudioFadeService` #3, `AudioVolumeWriteService` #19) →
  **Exzellent**, weil der Seam den internen Zustand beobachtbar macht,
- **1 Einheit mit eigenem Gedächtnis** (`DuckFactorLedger` #18) → **die dünnste der Suite.**

Das ist die belastbarste Verallgemeinerung dieses Audits: **Nicht die Komplexität einer Formel erzeugt
Lücken, sondern das Vorhandensein von Zustand.** Wo ein Seam den Zustand nach außen beobachtbar macht
(`FakeFadeTarget`, `FakeVolumeTarget`), verschwindet der Effekt vollständig.

---

## Querschnitt-Befunde

1. **Tautologie-Risiko: praktisch null — jetzt über alle 165 Tests belegt.** Die kulturelle Markierung
   (spec-first-Header) ist gelebt, nicht dekorativ: 26/27 Klassen tragen sie, die eine Ausnahme
   (`AudioFadeServicePauseTests`) benennt ihre Spec trotzdem im Header.
2. **Stärkstes Muster, unverändert:** Grenzen mit *benachbarten* true/false-Paaren pinnen (#7, #11, #2 — neu
   dazu #14, #24, #25). Wirksamster Schutz gegen `>`/`>=`- und `<`/`<=`-Mutationen.
3. **Zweitstärkstes Muster, neu benannt: der Reihenfolge-Diskriminator.** Ein Test, der beweist, *wann* ein
   Guard relativ zu einer Aggregation greift, nicht nur *dass* er greift: #14
   (`GenerationMismatch_OverridesPaused`), #16 (`ActiveTriggerBeatsSmallerInactive`), #22
   (`DuplicateWithZeroAfterAudibleFirst`), #19 (`SilentSlotDoesNotStopLaterSlotsFromBeingWritten`), #21
   (`ContainsKey` vor dem Schreiben). Diese Tests schützen gegen die Umbau-Fehler, die keine Zeile Logik
   ändern, sondern nur ihre Position.
4. **Wiederkehrende minor-Schwäche Nr. 1 — „Guard ohne echten Pin".** Ein `if`/Early-Return, dessen
   Entfernung am getesteten Input nichts ändert: #6 (`maxStep<=0`), **#15 (dieselbe Zeile im Zwilling
   `DuckEnvelope`)**, #18 (`FactorFor`-Rückfall, `Clear()`). **Das ist der eigentliche Audit-Befund 2026-08-15:**
   Die Lehre stand seit dem Erst-Audit in Teil I §5 und wurde beim **Kopieren einer bestehenden Einheit**
   trotzdem nicht angewandt. Gegenprobe, dass es geht: #25 pinnt seinen Vorrang-Guard in beiden Zweigen.
5. **Wiederkehrende minor-Schwäche Nr. 2 — unbestätigte Warn-Logs.** Siehe S2; seit dem Erst-Audit **mehr**
   statt weniger.
6. **Operator-Äquivalenz-blind:** `|` vs `+` in #10 und erneut in #25 — beide Male praktisch folgenlos, in
   #25 sogar unbehebbar. Kein Handlungsbedarf, aber es gehört benannt.
7. **Eine echte, konkret schließbare Vertragslücke im gesamten Bestand:** der zugesagte, aber ungetestete
   `rules == null`-Pfad in #17. Alles andere, was „fehlt", ist entweder inhärent (kontinuierliche Grenzen),
   durch Serialisierung unerreichbar (`[Range]`-Felder) oder bewusst ausgeschlossen (Retire in #18).
8. **Gesunde Redundanz:** #2/#3 beweisen #1-Zahlen erneut durch die jeweils höhere Schicht; #19 beweist die
   Clamp-Semantik von #12 noch einmal durch den Writer. Fängt Verdrahtungsfehler, kein Ballast.
9. **Test-Doubles sind eine Stärke des Projekts, nicht nur ein Hilfsmittel.** `FakeFadeTarget` (Recording),
   `FakeVolumeTarget` (Recording **mit `WriteCount`**, damit „nicht geschrieben" beobachtbar wird) und
   `FakeCategoryFactorSource` (bewusst **nicht**-neutraler Rückfall auf 0, damit falsche Lookups laut
   scheitern) sind drei verschiedene, jeweils begründete Entwurfsentscheidungen. Das ist überdurchschnittlich.

---

## Nicht geprüft (bewusst außerhalb dieses Audits)

- **Ungetesteter Glue/Orchestrierung:** `AudioManagerDynamic`, `AudioPlaybackService.Dispatch`,
  `AudioStopService`, `AudioPoolAcquisitionService`, `AudioFollowService`, `AudioPauseService`,
  `AudioDuckService` (verbleibend: `DeriveActiveCategories`), `AudioOcclusionSmoothingService`, beide
  WallCheck-*Schleifen*, `SceneAudioListenerProvider`, `PooledFadeTarget`, `PooledVolumeTarget`.
  → Das ist der M2/Gruppe-B-Backlog + die leere PlayMode-Assembly (M3) + die offenen PlayMode-Smokes.
- **`AudioFramework.Editor`** — siehe S1; die Grenze selbst steht in Teil I §3 und wird hier nur *gemessen*,
  nicht geändert.
- **Szenen-/Demo-Glue** (`Assets/Scripts/`, u. a. `TestScript`, `CategoryVolumeSliderBinding`) — bewusst
  außerhalb, Begründung in Teil I §3.
- **Korrektheit der Implementierungen selbst:** Im Zuge dieses Audits wurden **alle 23 getesteten Einheiten**
  gegen ihre Spezifikation gegengelesen — **keine** Implementierung widersprach ihrer Spec, **kein** Test
  behauptete etwas Spec-Widriges, und alle nachgerechneten Erwartungswerte (#1, #5, #12, #15, #18, #19, #25)
  stimmten von Hand. Der ungetestete Glue oben ist davon **nicht** erfasst.
