# AudioTool — Test-Qualitäts-Audit (TEST_VERIFICATION.md)

> **Zweck:** Ganzheitliche Bewertung der bestehenden Unit-Tests, Methode für Methode, um daraus
> bessere Tests fürs **Nachtesten des Bestandscodes** (M2 / Gruppe B) abzuleiten.
> **Datum:** 2026-06-15 · **Stand:** 11 getestete Logik-Einheiten, 69 EditMode-Tests (13 Dateien).
> Diese Datei ist eine **Mess-/Analyse-Datei**, kein Wissens- oder Aufgaben-Speicher
> (Wissen → CLAUDE.md · Aufgaben → BACKLOG.md). Sie darf veralten; bei Bedarf neu erstellen.

---

## Bewertungsraster

Jede Methode wurde gegen vier Kriterien geprüft:

1. **Spec-abgeleitet vs. Code-abgeschrieben** — kommen die Erwartungswerte aus dem Vertrag oder aus der Implementierung? (Der zentrale Tautologie-Test.)
2. **Mutation-Resistenz** — würde ein bewusst eingebauter Fehler wirklich einen Test rot machen? Insb.: wird *jeder* Guard/Branch durch einen Test gepinnt, dessen Erwartung sich vom „allgemeinen Pfad" unterscheidet?
3. **Branch-/Grenzwert-Abdeckung** — alle Pfade + Ränder (off-by-one, `==`-Grenze, null/leer)?
4. **Lücken** — was wird *nicht* geprüft, das geprüft gehören sollte?

**Noten:** **Exzellent** · **Stark** · **Solide** · **Dünn (mit Lücke)**.
Keine Note „mangelhaft" vergeben — es gibt in dieser Suite keinen schlechten Test.

---

## Gesamturteil (zuerst, ehrlich)

- ✅ **Kein tautologischer / Change-Detector-Test gefunden.** Kein Test liest die eigene Ausgabe der Implementierung zurück. Der dünnste Fall (`LowPassDispatchPolicy`) pinnt immerhin eine *Entscheidung*, keinen Durchreicher.
- ✅ **Spec-first-Disziplin durchgängig.** Jeder Test-Header trägt die „hand-derived, NOT read off implementation"-Klausel — und die Zahlenwerte (0.3, 0.75, 40, 8000 …) sind unabhängig nachrechenbar, also keine leere Floskel.
- ✅ **Grenzwert-Disziplin stark, wo es zählt.** Die kritischen `>=`-Ränder sind mit *benachbarten true/false-Paaren* gepinnt (siehe `AudioHandleValidator`, `PoolSlotAvailability`, `FadeOperation.IsComplete`). Das ist das Gegenteil eines tautologischen Tests.
- ✅ **Die „Safety"-Tests sind die Kronjuwelen:** Clobber-Guard (`AudioFadeService.ClearFade`), null-Eintrag-vor-gültigem-Eintrag (`FillDictionaryWithKeysAndValues`), Pause-einfrieren-dann-fortsetzen. Genau die Tests, die echte Regressionen fangen.
- ⚠️ **Alle Schwächen sind *minor* und gleichen sich:** (a) einzelne Guards, deren Entfernung am getesteten Input *nichts* ändern würde (also nicht echt gepinnt); (b) unbestätigte Warn-Logs (für ein Asset-Tool Teil des UX-Vertrags); (c) ein paar degenerate Inputs (negative Dauer/Speed); (d) Operator-Äquivalenzen (`|` vs `+`), die bei den gewählten Inputs nicht unterscheidbar sind.

| # | Einheit | Tests | Note | Headline |
|---|---|---|---|---|
| 1 | `AudioFadeMath.Evaluate` | 9 | **Exzellent** | Beide Clamps einzeln gepinnt; nur neg. Dauer ungetestet |
| 2 | `FadeOperation` | 8 | **Stark** | `IsComplete`-Grenze gepinnt; leichte Überlappung mit #1 (gerechtfertigt: Verdrahtung) |
| 3 | `AudioFadeService` | 13 | **Exzellent** | Clobber-Guard + Pause-Freeze + Stop-Count diskriminierend |
| 4 | `LowPassDispatchPolicy.Resolve` | 4 | **Solide** | Dünnste Einheit, aber gegen Hardcode + Inversion abgesichert → nicht tautologisch |
| 5 | `WallOcclusionMath` | 4 | **Stark** | Lücke: „unter Null subtrahieren → Floor rettet" als Komposit ungetestet |
| 6 | `OcclusionSmoothing.Step` | 6 | **Stark** | `maxStep<=0`-Guard nicht echt gepinnt (gen. Pfad fällt zusammen) |
| 7 | `AudioHandleValidator.IsCurrent` | 6 | **Exzellent** | Beste Grenz-Disziplin: beide Ränder mit true/false-Paaren |
| 8 | `…Provider.FillLayerMaskDictionary…` | 4 | **Stark** | keep-first gepinnt; Warn-Log auf Duplikat unbestätigt |
| 9 | `…Provider.FillDictionaryWithKeysAndValues` | 6 | **Exzellent** | null-Eintrag-vor-gültigem = ideale Diskriminierung |
| 10 | `WallLayerMask.FromLayers` | 4 | **Stark** | `|` vs `+` nicht unterscheidbar (praktisch moot bei eindeutigen Keys) |
| 11 | `PoolSlotAvailability.IsFree` | 5 | **Exzellent** | Jede AND-Klausel einzeln gepinnt + `==`-Grenze inklusiv |

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

> **Lehre für M2:** Mustergültig — die *at-boundary*-Tests (AtStart/AtEnd) sind „Form"-Tests, die *past-boundary*-Tests (Before/Past) sind die echten Guard-Pins. Diese Unterscheidung bewusst beibehalten.

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

> **Lehre für M2:** Das `FakeFadeTarget`-Muster ist die **Blaupause für Gruppe B** (Unity-gekoppelt): kleines Recording-Double hinter ein Interface, dann am Double assert. Genau so für `ShouldContinueLoop`, `Gate`/`ResolveVolume`, `PauseAll`/`UnpauseAll`.

### 4 — `LowPassDispatchPolicy.Resolve(useWallCheck, defaultCutoff)` → **Solide** (dünn, aber valide)
**Vertrag:** `Enabled ⟺ useWallCheck`; `CutoffFrequency` = der konfigurierte „offene" Wert, unverändert durchgereicht.

- Die Methode ist heute fast trivial (konstruiert nur den Struct). Trotzdem **nicht tautologisch:**
  - Hardcode `Enabled=true` → `NonWallCheck`-Test fällt. ✔
  - **Inversion** `Enabled=!useWallCheck` → beide Enabled-Tests fallen. ✔
  - Cutoff hardcodiert → der `17500f`-Test fängt es (Cutoff wird *regardless* durchgereicht). ✔
- **Einordnung:** geringster Wert pro Test der Suite, aber legitim — es pinnt eine **Design-Entscheidung** (Filter nur für wand-geprüfte Sounds) gegen spätere „Optimierungen". Kein Handlungsbedarf.

### 5 — `WallOcclusionMath` (`ApplyWall`, `ClampToFloor`) → **Stark** *(Modellwechsel 2026-06-20)*
**Vertrag (jetzt multiplikativ, Variante A):** `ApplyWall = current − (current − floor) · damping`. Eine Wand dämpft den Cutoff um den Bruchteil `damping` (0 = transparent, 1 = fällt in einer Wand auf den Floor) **Richtung Floor**. Über N Wände wird der offene Bereich über dem Floor mit `∏(1 − dᵢ)` skaliert → reihenfolge-unabhängig und **asymptotisch** zum Floor (unterschreitet ihn nie). `ClampToFloor` ist damit nur noch ein Sicherheitsnetz gegen Fehlkonfig (`d>1`) und Float-Drift. *(Frühere lineare Hz-Subtraktion ersetzt — siehe Begründung Occlusion-Abschnitt in CLAUDE.md.)*

- **Tests (9):** 7× `ApplyWall_*` (Einzelwand-Fraktion, `d=0` transparent, `d=1` → Floor, multiplikative Akkumulation, abnehmender Absolut-Schritt, Reihenfolge-Unabhängigkeit, Komposit `ApplyWall→ClampToFloor` bei `d>1`) + 2× `ClampToFloor_*` (unverändert, modell-agnostisch).
- **Spec-abgeleitet:** Ja; alle Erwartungswerte aus der Dämpfungs-/Asymptote-Gleichung hand-abgeleitet (`Open=22000`, `Floor=1000`), nicht aus dem Code.
- **Mutation:** `−`→`+` in `ApplyWall`. **5 Tests rot** (Fraktion, Voll-Dämpfung, Akkumulation, Reihenfolge, Komposit), `ZeroDamping` bleibt grün (`+0 == −0`, pinnt bewusst den Wert, nicht den Operator), beide `ClampToFloor` grün. Mutation gefangen → Suite schützt den Vertrag. ✔
- **Lücke aus der Vorversion geschlossen:** Der „viele Wände → Floor"-Komposit-Pfad ist jetzt als Kette getestet (`ApplyWall_ThenClampToFloor_OverDampedConfigRescuedToFloor`: `d=1.5` → −9500 → Clamp → 1000).

**Zwei Stolperstellen in dieser Session — ehrlich festgehalten** (Anlass für die neuen Schutzregeln in CLAUDE.md):

1. **Float-Toleranz war ein echter Test-Defekt.** `Delta = 1e-5` war zu eng für die verkettete float32-Rechnung des Reihenfolge-Tests (Faktoren `0.3f`/`0.8f` nicht exakt darstellbar → Ergebnis `3939.99976` statt `3940`, Abweichung ~`2.4e-4`). Diagnose lief korrekt die Instanzen durch: Code korrekt, Hand-Herleitung `3940` korrekt → *erst danach* legitim beim Test gelandet (Authoring-Defekt, Kategorie a). Korrigiert auf `1e-2` (perzeptiv exakt, weiterhin um Größenordnungen enger als jede Mutation).
2. **Verfehlte Mutations-Vorhersage war KEIN Test-Defekt, sondern mein Modellfehler.** Vorhergesagt: 6 rot. Tatsächlich: 5. `PerWallAbsoluteStepDiminishes` blieb grün, weil er nur die *relative* Ordnung prüft (`secondStep < firstStep`) — unter dem Vorzeichen-Flip drehen beide Schritte ins Negative und behalten ihre Ordnung (`−15750 < −10500`). Der Mutation-Check war trotzdem bestanden (≥1 rot, Suite-Ebene). Der Reflex, den Test zu „härten", damit die Prognose stimmt, wurde **verworfen** — reines Gold-Plating, das andere Tests auf Suite-Ebene schon abdecken. Test **unangetastet**.

### 6 — `OcclusionSmoothing.Step(current, target, dt, speed)` → **Stark**
**Vertrag:** MoveTowards mit `speed` Hz/s; `speed<=0` → sofort `target`; kein Overshoot.

- **Spec-abgeleitet:** Ja.
- **Mutation:**
  - Richtung hoch/runter beide getestet (Vorzeichenfehler fiele auf). ✔
  - **Kein Overshoot** (`absDiff<=maxStep → target`): `WithinOneStep` (900→1000) — ohne Snap käme 1100. ✔
  - **`speed<=0 → target`:** `ZeroOrNegativeSpeed` unterscheidet sauber von der zweiten Early-Return (`maxStep<=0 → current`): ohne den speed-Guard käme `current` (100) statt `target` (1000). ✔
- **Lücke (minor):** **`maxStep<=0`-Guard ist nicht echt gepinnt.** `Step_ZeroDeltaTime` liefert 100 — aber auch *ohne* diesen Guard ergäbe der allgemeine Pfad 100 (`current + maxStep(0)`). Der Guard schützt eigentlich **negatives** `dt`/`speed` — und genau die sind ungetestet. Der Test „beweist" hier also weniger, als er suggeriert.

> **Lehre für M2 (wichtig):** Ein Guard, dessen Entfernung am getesteten Input *dasselbe* Ergebnis liefert, ist **nicht** getestet. Beim Schreiben fragen: „Wenn ich diese `if`-Zeile lösche — wird *dieser* Test rot?" Wenn nein: anderen Input wählen (hier: negatives `dt`).

### 7 — `AudioHandleValidator.IsCurrent(idx, handleGen, slotGen, poolLen)` → **Exzellent**
**Vertrag:** außerhalb `[0, poolLen)` → nie current (P6); sonst `handleGen == slotGen` (W1).

- **Beste Grenz-Disziplin der Suite — beide Ränder mit benachbarten true/false-Paaren gepinnt:**
  - **Oberer Rand:** `IndexAtPoolLength` (idx==10, pool 10 → false). Mit `>` statt `>=` würde 10 durchrutschen → true; Test fängt es. ✔
  - **Unterer Rand:** `NegativeIndex` (−1 → false) **+** `IndexZeroLowerBound` (0 → true) als Paar. Mit `<=0` statt `<0` würde idx 0 fälschlich raus → der Zero-Test fängt es. ✔✔
  - **Generation:** `StaleGeneration` (7 vs 8 → false) fängt das Ignorieren der Generation. ✔
  - **P6 wörtlich:** `IndexFarAbovePoolLength` (99999, pool 50). ✔
- **Lücke:** praktisch keine nennenswerte.

### 8 — `AudioManagerDictionaryProvider.FillLayerMaskDictionaryWithLayerRelatedValues` → **Stark**
**Vertrag:** `SingleLayer → WallDampingFactor`; Duplikat = **keep-first**; null/leer = stiller No-op.

- **Mutation:** **keep-first** gepinnt (`DuplicateLayer_KeepsFirstValue`): mit `dict[k]=v` statt `TryAdd` käme 9000 statt 5000. ✔ null/leer-Guards getestet (ohne Guard NRE → Test wirft). ✔
- **Lücke (minor):** Die **Warnung** auf Duplikat wird **nicht** mit `LogAssert.Expect` bestätigt. Das *Verhalten* (keep-first) ist gesichert, die *Diagnose-Meldung* nicht — für ein Asset-Tool (Nutzer debuggt Fehlkonfiguration darüber) Teil des Vertrags.

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

> **Lehre für M2:** Wenn zwei Operatoren am gewählten Input dasselbe liefern, ist der Operator nicht gepinnt. Falls die Unterscheidung *vertraglich* zählt, Input wählen, der sie trennt — auch wenn der „in der Praxis nicht vorkommt".

### 11 — `PoolSlotAvailability.IsFree(isPlaying, currentTime, busyUntilTime, isPaused)` → **Exzellent**
**Vertrag:** frei ⟺ `!isPlaying && currentTime >= busyUntilTime && !isPaused`.

- **Jede AND-Klausel einzeln gepinnt:** Playing→belegt; busy-window offen→belegt; paused→belegt (trotz sonst-frei). Jeder Test isoliert genau eine Klausel. ✔
- **`>=`-Grenze inklusiv gepinnt:** `CurrentTimeEqualsBusyUntil → free` — mit `>` käme false. ✔
- Vorbild für die kommenden Prädikat-Extraktionen.

---

## Querschnitt-Befunde

1. **Tautologie-Risiko: praktisch null.** Die kulturelle Markierung (spec-first-Header) ist gelebt, nicht dekorativ.
2. **Stärkstes Muster:** Grenzen mit *benachbarten* true/false-Paaren pinnen (#7, #11, #2). Das ist der wirksamste Schutz gegen `>`/`>=`- und `<`/`<=`-Mutationen.
3. **Wiederkehrende minor-Schwäche (das eigentliche Audit-Ergebnis):** **„Guard ohne echten Pin"** — ein `if`/Early-Return, dessen Entfernung am getesteten Input nichts ändert (#6 `maxStep<=0`; teils #1, dort aber durch Past/Before abgedeckt).
4. **Asset-Tool-spezifisch:** Mehrere **Warn-Logs** (Fehlkonfiguration) sind reines Verhalten ohne `LogAssert`-Bestätigung (#8, #9). Für ein verkauftes Plugin ist die Diagnose-Meldung Teil der UX.
5. **Operator-Äquivalenz-blind:** `|` vs `+` (#10), isolierte statt verkettete Mathe (#5).
6. **Gesunde Redundanz:** #2/#3 beweisen #1-Zahlen erneut durch die jeweils höhere Schicht — fängt Verdrahtungsfehler, kein Ballast.

---

## Konkrete Regeln für das Nachtesten des Bestandscodes (M2 / Gruppe B)

Abgeleitet aus den Befunden — als verbindliche Checkliste beim Schreiben jedes neuen Tests:

- [ ] **Spec-first-Header** als Pflicht-Boilerplate übernehmen (die „hand-derived, NOT read off implementation"-Klausel).
- [ ] **Jeden Guard auf echten Pin prüfen:** „Wenn ich diese `if`-Zeile lösche — wird *dieser konkrete* Test rot?" Wenn nein → Input ändern (Lehre aus #6). Das ist der präziseste Mutation-Check pro Branch.
- [ ] **Grenzen immer als true/false-Paar** um den `==`-Punkt (Lehre aus #7/#11).
- [ ] **Komposite/realistische Sequenzen** testen, nicht nur isolierte Schritte (Lehre aus #5: ApplyWall→ClampToFloor als Kette).
- [ ] **Operator unterscheidbar machen:** Input wählen, der `|`/`+`, `&&`/`&` etc. trennt, wenn der Operator vertraglich zählt (Lehre aus #10).
- [ ] **Vertragliche Seiteneffekte (Warn-Logs) mit `LogAssert.Expect` pinnen** — besonders Fehlkonfigurations-Warnungen des Asset-Tools (Lehre aus #8/#9).
- [ ] **Recording-Double + Interface-Seam** als Standardmuster für Unity-gekoppelte Logik (Blaupause: `FakeFadeTarget` → für `ShouldContinueLoop`, `Gate`/`ResolveVolume`, `PauseAll`).
- [ ] **Degenerate Inputs** (negativ/leer/null) bewusst mitnehmen, wenn der Guard sie abdeckt.

---

## Workflow: Bestandscode nachtesten (Loop)

> Etablierter Ablauf fürs Nachtesten ungetesteter Methoden (M2 / Gruppe B). Variante des normalen
> TDD-Loops (CLAUDE.md), zugeschnitten auf **bestehenden** Code. Eine Methode pro Runde.

### Der Unterschied zu normalem TDD (warum dieser Loop überhaupt eigen ist)

Bei neuem Code existiert die Methode noch nicht — man *kann* nicht abschreiben. Beim Bestandscode liegt
die Implementierung **offen vor einem** → die "read off implementation"-Falle ist hier **maximal stark**.
Daraus folgt der zentrale, neue **Entscheidungspunkt**, den neues TDD nicht braucht:

> Der spec-abgeleitete Test wird gegen den *alten* Code ausgeführt:
> - **grün** → Verhalten ist korrekt und jetzt eingefroren. ✔
> - **rot** → bewusst entscheiden, **niemals** den Erwartungswert still an den Code anpassen:
>   - **Spec richtig → latenter Bug im Bestandscode gefunden.** STOP, an Patrick melden.
>   - **Code richtig, Spec war naiv** → Spec mit Patrick schärfen, das *Warum* verstehen und neu
>     hand-ableiten (nicht den Code-Wert abschreiben — das wäre exakt die Tautologie).

### Die Schritte

0. **Seam-Frage zuerst:** Steckt eine **pure Entscheidung** drin, die sich rausziehen lässt
   (wie `PoolSlotAvailability`)? Wenn ja → erst extrahieren. Wenn nein (echtes Unity-Verhalten) →
   PlayMode-Fall, nicht EditMode.
1. **Formulieren, WAS die Methode soll** — aus dem **Vertrag/der Absicht**, *bewusst nicht* durch
   Paraphrasieren der Code-Zeilen.
2. **Beschreibung schreiben, aus der sich Tests ableiten** — wörtlich als **XML-Doc-Header der
   Testklasse** (die "hand-derived, NOT read off implementation"-Klausel + die Vertrags-Klauseln).
   So wird die Spec Teil des Tests und driftet nicht weg.
3. **Red-First als Stub** (nicht Auskommentieren): die **neue pure Funktion** anlegen, Rumpf mit
   `throw new NotImplementedException()` stubben → Test-Assembly kompiliert, Tests **laufen** und
   scheitern („laufendes Rot"). Die alte Methode bleibt unangetastet, bis grün. *(Korrektur zum ersten
   Entwurf „alte Methode auskommentieren": das bricht alle Aufrufer — Stub ist sauberer und ist die
   etablierte Projekt-Konvention.)*
4. **Normaler Testweg** — grün → Mutation → rot mit Ansage → grün. **Schärfung (Befund #3):** Die
   Mutation ist **kein** Zufalls-Kaputtmachen, sondern **jeden Guard/Branch einzeln** kippen/löschen
   und vorhersagen, *welcher namentliche Test* rot wird. Macht das Löschen einer `if`-Zeile **keinen**
   Test rot → der Guard ist nicht getestet → Lücke schließen, bevor committet wird.
5. **Verdrahtung schließen + 8-Punkte-Checkliste abhaken:** alte Methode zeigt jetzt auf die getestete
   pure Funktion (triviale, per Augenschein prüfbare Delegation). Dann die Checkliste oben durchgehen.
6. **Eine Methode pro Runde, eigener Commit.** Kleine, gegenlesbare Diffs; die neuen Tests sind ab dann
   **eingefroren** wie alle anderen. (Patrick committet.)

### Ehrliche Warnung zum Seam (Schritt 0)

Das Extrahieren ist selbst ein **Refactor von ungetestetem Code** (Henne-Ei). Absicherung: rein
**mechanisch** halten (denselben Ausdruck kopieren, Eingaben zu Parametern heben, **keine**
Logikänderung), im selben kleinen Schritt; die neuen Tests decken die extrahierte Logik sofort ab.
Kommt beim Extrahieren die Versuchung auf, „nebenbei" etwas zu verbessern → STOP, separater Schritt.

---

## Nicht geprüft (bewusst außerhalb dieses Audits)

- **Ungetesteter Glue/Orchestrierung:** `AudioManagerDynamic`, `AudioPlaybackService.Dispatch`, `AudioStopService`, `AudioFollowService`, beide WallCheck-*Schleifen*, `PooledFadeTarget`. → Das ist genau der M2/Gruppe-B-Backlog + die leere PlayMode-Assembly (M3).
- **Korrektheit der Implementierungen selbst:** Im Zuge des Audits gegengelesen — **keine** Implementierung widerspricht ihrer Spec, **kein** Test behauptet etwas Spec-Widriges.
