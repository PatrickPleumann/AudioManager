# AudioTool — Showcase Requirements (60-Sek-Feature-Video)

> **Zweck:** Abarbeitbare Liste, mit der wir **jedes vom Nutzer wahrnehmbare** Feature des AudioTools vorführbar machen — als kleine, in sich geschlossene Demo-Module mit Primitiven (Box, Plane, Kugel), **ohne Art-Assets**. Jedes Modul liefert ein „Aha, so funktioniert das".
>
> **Verhältnis zum Rest:** Dies ist die Detailaufschlüsselung des BACKLOG-Punkts *„Demo-Szene für 60-Sek-Verkaufsvideo"* (Teil B → Release-Hygiene). **Wissen → [`CLAUDE.md`](CLAUDE.md) · Aufgaben → [`BACKLOG.md`](BACKLOG.md) · diese Datei → reine Showcase-Planung.** Keine durable Architektur-Erkenntnis hier ablegen — die gehört in CLAUDE.md.
>
> **Drei Realitäten, die die ganze Liste prägen:**
> 1. **Audio ist unsichtbar.** Ein Zuschauer hört Occlusion/Ducking nicht garantiert (Handy stumm, Scrubbing, laute Umgebung). Jedes Modul muss das Hörbare **sichtbar telegrafieren** (Caption, Cutoff-Meter, Raycast-Linie, Volume-Balken). Siehe Leitprinzip #3.
> 2. **Showcase = nur Wahrnehmbares.** Gezeigt wird ausschließlich, was der Nutzer **hört oder sieht**. Reine Performance-/Interna-Themen (Object Pooling, Zero-GC, CTS-Reuse, WallCheck-Last, VR-Raycast-Batching) sind **kein** zeigbares Einzel-Feature — sie wandern gebündelt in **einen** Stresstest, dessen „Aha" das **Ausbleiben** des Profiler-Spikes ist (Abschnitt 4).
> 3. **60 s reichen nicht für alles** (~20 Features ≈ 3 s/Feature = Reizüberflutung). Darum: **Modul-Baukasten** (ein Showcase pro Feature, einzeln als Store-GIF nutzbar) **+** ein kuratierter **60-s-Hero-Schnitt** aus den `[Hero]`-Modulen (Abschnitt 7). „Alle Features zeigen" passiert über den Baukasten; das 60-s-Video ist die Highlight-Rolle.

---

## 0. Leitprinzipien (gelten für jedes Modul)

1. **Primitive statt Art.** Box/Plane/Kugel + Farbe genügen. Der Zuschauer soll das *Feature* verstehen, nicht das Auto bewundern.
2. **Ein Modul = ein Feature = ein „Aha".** Module sind unabhängig baubar und einzeln aufnehmbar (GIF-tauglich). Erst am Ende zum 60-s-Schnitt kombinieren.
3. **Hörbares sichtbar machen.** Pflicht-Element pro Modul. Werkzeuge (Abschnitt 1, Viz-Helfer): On-Screen-Caption, Live-**Cutoff-Meter** (Hz), **Raycast-Linie** Emitter→Listener (grün/rot), **Volume-Balken** pro Kategorie, **GC/FPS-Overlay**. Diese Helfer sind Demo-Code (wie `TestScript.cs`), **nicht** Teil des ausgelieferten Pakets.
4. **Nur Wahrnehmbares wird ein Feature-Modul.** Was man weder hört noch sieht, ist kein Showcase-Modul: Performance → der eine Stresstest (Abschnitt 4); reine Korrektheit/Interna → über Copy/Tests/Doku (Abschnitt 5).
5. **Priorität ehrlich taggen:**
   - `[Hero]` — stark + aud-visuell klar → Pflicht im 60-s-Schnitt.
   - `[Support]` — gutes Einzel-GIF / Store-Screenshot, im Trailer höchstens kurzer Beat.
6. **Erst aktuelle Features (jetzt baubar), dann geplante (gated).** Geplante Module erst bauen, wenn das Feature steht (Backlog).

---

## 1. Gemeinsame Bühne (einmal bauen — alle Module nutzen sie)

Eine einzige Demo-Szene als Baukasten; pro Modul nur das jeweils Nötige aktivieren.

**Szene & Rig**
- [ ] Leere Demo-Szene `Showcase.unity` (getrennt von `TestScript`-Spielwiese).
- [ ] `AudioManagerDynamic` auf leerem GameObject + **produktives** `AudioSystemConfig`-Asset zugewiesen (⚠️ Abhängigkeit: das ausgelieferte Config-Asset fehlt noch im Repo — BACKLOG-Punkt „Ausgeliefertes `AudioSystemConfig`-Asset fehlt"; bis dahin `ConfigForTests` zum Bauen nutzen).
- [ ] Kamera-Rig mit **AudioListener** — wahlweise feste Cinematic-Kamera **oder** simpler First-Person-Controller (damit „hinter die Wand laufen" möglich ist; nur Primitiv-Capsule nötig).
- [ ] Boden-Plane + neutrales Licht + bewusst gewählte Hintergrundfarbe (Kontrast für Captions).

**Primitiv-Prefabs (farbcodiert)**
- [ ] **Emitter-Box** (würfel, Standard-Emitter für die meisten Module).
- [ ] **Bewegte Box** (Emitter + simples Links↔Rechts-`MoveBetween`-Script) → Follow/Doppler/Pan.
- [ ] **Wand-Prefabs pro Layer** (z. B. `WallThin`, `WallThick`, `WallGlass`) — sichtbar unterschiedlich eingefärbt, Layer korrekt gesetzt.
- [ ] **Zonen-Trigger** (unsichtbarer Box-Collider `isTrigger`) für Ambient-/Musik-Zonenwechsel.

**Demo-Audio-Clips (royalty-free Platzhalter genügen)**
- [ ] **Steady, höhenreicher Loop** (Alarm/Summer/Synth-Pad) — *der* Occlusion-Clip (Tiefpass wird so am deutlichsten hörbar).
- [ ] **Motor-Loop** (Follow + Doppler).
- [ ] **Footstep-Set** (mehrere Varianten) — Random-Variation + Surface-Reaktion.
- [ ] **Explosion/Impact** One-Shot (auch für den Stresstest, Abschnitt 4).
- [ ] **Ambient-Bett A** (z. B. Wald) + **Ambient-Bett B** (z. B. Höhle) — Crossfade/Zonen.
- [ ] **Musik-Track** (2D) — Ducking, Pause, Volume-Slider.
- [ ] **Dialog/Voiceline** — Ducking (und später VOIP).
- [ ] **Combat-Musik-Stem(s)** — adaptive Layer.
- [ ] **UI-Klick** — 2D + Pause-Ausnahme.

**Viz-/Debug-Helfer (Demo-Code, nicht ausgeliefert)**
- [ ] **Caption-Overlay** (großer Text unten/oben, pro Modul einblendbar).
- [ ] **Cutoff-Meter** — liest `filter.cutoffFrequency` (bzw. `AudioObject.TargetCutoff`) eines Slots und zeigt ihn als Balken/Zahl 22000→Min Hz.
- [ ] **Raycast-Linie** — zeichnet Emitter→Listener, färbt rot bei Wandtreffer (macht den Occlusion-Mechanismus sichtbar).
- [ ] **Volume-Balken pro `AudioCategory`** — reagiert live auf Slider/Ducking.
- [ ] **GC/FPS-Overlay** — Allocations + Framerate sichtbar (v. a. für den Stresstest, Abschnitt 4; alternativ Unity-Profiler-Fenster danebenlegen).
- [ ] **UI-Canvas** mit Lautstärke-Slidern (ein Slider pro Kategorie) + Buttons (Play/Stop/Pause/Crossfade) zum Triggern.

---

## 2. Showcase-Module — AKTUELLE Features (jetzt baubar)

> Template je Modul: **Aha** · **Bühne** · **Aktion** · **ADO/Config** · **API** · **Sichtbar machen**.

### A1 — 3D-Positional `[Support]`
- [ ] **gebaut**
- **Aha:** „Der Sound *sitzt* an der Box — links leiser links, distanzgedämpft."
- **Bühne:** eine Emitter-Box seitlich versetzt, Kamera fix.
- **Aktion:** Box spielt Loop; Kamera/Box bewegt sich näher/weiter → Lautstärke ändert sich.
- **ADO/Config:** `Spatial Blend = 1`, `Use Wall Check = false`, `Is One Shot = false`.
- **API:** `PlaySpatial(ado, boxTransform)`.
- **Sichtbar:** Distanzlinie + Lautstärke-Balken; Caption „3D — distance-attenuated".

### A2 — 2D / Non-Spatial `[Support]`
- [ ] **gebaut**
- **Aha:** „Überall gleich laut — UI/Musik, egal wo die Kamera steht."
- **Bühne:** Kamera fährt durch die Szene, Lautstärke bleibt konstant.
- **ADO/Config:** beliebig; `PlayNonSpatial` erzwingt 2D (ignoriert `Spatial Blend`).
- **API:** `PlayNonSpatial(ado)`.
- **Sichtbar:** Caption „2D — same everywhere", konstanter Volume-Balken trotz Kamerafahrt.

### A3 — Wall Check / Lightweight Occlusion `[Hero]` ⭐ (Flaggschiff/Moat)
- [ ] **gebaut**
- **Aha:** „Box hinter Wand → dumpf; heraustreten → gleitet weich zurück zu klar."
- **Bühne:** Emitter-Box mit Steady-Loop fest hinter einer Wand; Kamera/Spieler läuft seitlich hinter die Wand und wieder raus.
- **ADO/Config:** `Spatial Blend = 1`, `Use Wall Check = true`. Config: `Occlusion Smoothing Speed` moderat (weicher Glide sichtbar), Layer-Damping gesetzt.
- **API:** `PlaySpatial(ado, boxTransform)`.
- **Sichtbar:** **Pflicht** — Cutoff-Meter (22000↓) + Raycast-Linie (grün→rot bei Wand) + Caption „Behind wall → muffled (lightweight occlusion, no full spatializer)". Der weiche Rückweg ist *der* Money-Shot.

### A4 — Multi-Wall + Per-Layer-Damping `[Support]` (Erweiterung von A3)
- [ ] **gebaut**
- **Aha:** „Zwei Wände dämpfen stärker als eine; dicke Wand stärker als dünne — und Reihenfolge egal (multiplikativ)."
- **Bühne:** drei Emitter nebeneinander: (1) eine dünne Wand, (2) eine dicke Wand, (3) zwei Wände hintereinander.
- **ADO/Config:** `Use Wall Check = true`; Layer `WallThin`/`WallThick` mit unterschiedlichen `Wall Damping Factor`.
- **Sichtbar:** drei Cutoff-Meter nebeneinander → unterschiedliche Höhe; Caption „More/thicker walls → stronger damping".

### A5 — Follow Emitter (das „vorbeifahrende Auto") `[Hero]` ⭐
- [ ] **gebaut**
- **Aha:** „Box fährt links→rechts, der Sound fährt mit — ohne Parenting."
- **Bühne:** bewegte Box quert das Bild vor fixer Kamera (Patricks Auto-Beispiel).
- **Aktion:** Box mit Motor-Loop von links nach rechts; Sound wandert hörbar im Stereobild + Distanz.
- **ADO/Config:** `Spatial Blend = 1`, `Follow Emitter = true`, `Is One Shot = false`.
- **API:** `PlaySpatial(engineADO, movingBox)`.
- **Sichtbar:** Caption „Follow — sound tracks the emitter (no parenting)"; optional Trail/Linie der Box.
- **Bonus-Beat:** Box mitten im Sound zerstören → Sound stoppt + Slot frei (Follow-Cleanup). Caption „Emitter destroyed → sound stops cleanly".

### A6 — Handle + Stop (steuerbarer Loop) `[Support]`
- [ ] **gebaut**
- **Aha:** „Loop läuft — Knopfdruck stoppt *genau diesen* Sound."
- **Bühne:** Emitter-Box + UI-Button „Stop".
- **ADO/Config:** `Can Handle Audio Source = true`, `Is One Shot = false`.
- **API:** `h = PlaySpatial(ado, box)` … `Stop(h)`.
- **Sichtbar:** Button-Klick sichtbar, Volume-Balken fällt auf 0; Caption „Handle → stop on demand".

### A7 — Fade In / Fade Out `[Support]`
- [ ] **gebaut**
- **Aha:** „Sanft rein, sanft raus statt hartem Ein/Aus."
- **Bühne:** ein Loop + zwei Buttons (FadeIn/FadeOut).
- **API:** `FadeInSpatial(ado, box, dur)` / `FadeInNonSpatial(ado, dur)` / `FadeOut(h, dur)`.
- **Sichtbar:** Volume-Balken rampt sichtbar hoch/runter; Caption „Built-in fades".

### A8 — Crossfade (Ambient-Zonen) `[Hero]`
- [ ] **gebaut**
- **Aha:** „Spieler wechselt Zone → Ambient A blendet in Ambient B."
- **Bühne:** zwei Zonen-Trigger (Wald | Höhle); Kamera/Spieler läuft hindurch.
- **API:** `h = CrossfadeNonSpatial(fromHandle, toADO, dur)` (oder `CrossfadeSpatial`).
- **Sichtbar:** zwei Volume-Balken kreuzen sich (A↓, B↑); Caption „Crossfade between ambiences".

### A9 — Volume-Kategorien + Laufzeit-Slider `[Hero]`
- [ ] **gebaut**
- **Aha:** „Settings-Slider regelt eine ganze Sound-Kategorie live."
- **Bühne:** mehrere Sounds verschiedener Kategorien gleichzeitig + UI-Slider pro Kategorie (Music/SFX/Ambient).
- **ADO/Config:** Sounds verschiedener `Current Type`; `AudioSourceVolume`-Assets pro Kategorie.
- **API:** Laufzeit-Override des Kategorie-Volumes (über das Volume-Dictionary/Asset).
- **Sichtbar:** Slider ziehen → zugehöriger Volume-Balken folgt sofort; Caption „One slider = whole category".

### A10 — PauseAll / UnpauseAll + RespectsGlobalPause `[Support]`
- [ ] **gebaut**
- **Aha:** „Pause-Menü friert SFX/Ambient ein — UI/Musik laufen bewusst weiter."
- **Bühne:** mehrere Sounds; Button „Pause". Ein Sound mit `Respects Global Pause = false` (Menü-Musik) läuft weiter.
- **API:** `PauseAll()` / `UnpauseAll()`.
- **Sichtbar:** Volume-Balken einiger Sounds frieren ein, einer läuft weiter; Caption „PauseAll — except music/UI".

### A11 — timeScale-Entkopplung (Slow-Mo) `[Support]`
- [ ] **gebaut**
- **Aha:** „Bullet-Time verlangsamt das Bild — Audio bleibt echt-zeitig."
- **Bühne:** bewegte Box + `Time.timeScale = 0.2`; Fade/Occlusion laufen in realen Sekunden weiter.
- **API:** Demo setzt `timeScale`; Audio nutzt `unscaledTime` intern (kein API-Call nötig).
- **Sichtbar:** Caption „Slow-mo visuals, real-time audio — pause via PauseAll(), not timeScale". Hinweis: bewusst zeigen, dass `timeScale=0` NICHT pausiert.

### A12 — ScriptableObject-/No-Code-Workflow `[Support]`
- [ ] **gebaut**
- **Aha:** „Neuer Sound = ein Asset im Inspector, kein Code."
- **Bühne:** kurzer Editor-B-Roll: ADO anlegen, Clip reinziehen, einzeiliger Aufruf.
- **Sichtbar:** Bildschirmaufnahme des Inspectors + ein Code-Zeilen-Insert; Caption „Configure in the Inspector — one call to play".

---

## 3. Showcase-Module — GEPLANTE Features (erst bauen, wenn Feature steht)

> ⚠️ **Gated:** Jedes Modul hängt am jeweiligen BACKLOG-Feature (Teil B). Reihenfolge des Bauens = Reihenfolge der Feature-Fertigstellung. Hier nur der *Showcase-Plan*, nicht das Feature selbst.

### B1 — Mixer/Bus + Ducking `[Hero]` ⭐ (höchster Verkaufshebel)
- [ ] **gebaut** — *abhängig von:* BACKLOG „Mixer/Bus + Ducking"
- **Aha:** „Musik läuft — Dialog setzt ein → Musik duckt; Dialog endet → Musik kommt zurück."
- **Bühne:** 2D-Musik dauerhaft + getriggerte Voiceline.
- **Sichtbar:** Music-Volume-Balken senkt sich sichtbar während Dialog, Attack/Release-Hüllkurve erkennbar; Caption „Auto-ducking: dialog over music".

### B2 — Random Pitch/Volume-Variation `[Support]`
- [ ] **gebaut** — *abhängig von:* BACKLOG „Random Pitch/Volume-Variation"
- **Aha:** „Schnelle Footsteps ohne Maschinengewehr-Effekt."
- **Bühne:** A/B-Vergleich: links ohne Variation (monoton), rechts mit Variation (lebendig).
- **Sichtbar:** Split-Screen-Caption „Without / With variation"; ggf. Pitch-Wert-Anzeige.

### B3 — Adaptive/Interaktive Musik-Layer `[Hero]` ⭐ (größter „Wow", größter FMOD-Abstand)
- [ ] **gebaut** — *abhängig von:* BACKLOG „Adaptives/interaktives Musik-Layer"
- **Aha:** „Spieler betritt Kampfzone → Combat-Stem blendet auf den Basis-Track."
- **Bühne:** Zonen-Trigger „enter combat"; Basis-Musik läuft, Combat-Layer faded ein/aus.
- **Sichtbar:** zwei/drei Stem-Volume-Balken schichten sich übereinander; Caption „Adaptive music layers by game state".

### B4 — Layer-basierte reaktive Sounds (Oberfläche) `[Support]`
- [ ] **gebaut** — *abhängig von:* BACKLOG „Layer-basierte reaktive Geräusche"
- **Aha:** „Footstep klingt auf Holz anders als auf Metall."
- **Bühne:** Boden in Streifen unterschiedlicher Layer (Holz/Metall/Boden); Capsule läuft drüber.
- **Sichtbar:** Caption mit aktuell getroffenem Surface-Layer; unterschiedliche Clips hörbar.

### B5 — Doppler (mit Follow / vorbeifahrende Box) `[Support]→[Hero]`
- [ ] **gebaut** — *abhängig von:* BACKLOG „ADO Control Surface … `dopplerLevel` ⭐"
- **Aha:** „Vorbeifahrende Box: Tonhöhe steigt beim Annähern, fällt beim Vorbeifahren."
- **Bühne:** **direkte Erweiterung von A5** — dieselbe bewegte Box, jetzt mit Doppler. (Macht das Auto-Beispiel akustisch komplett.)
- **Sichtbar:** Caption „Doppler — pitch shifts as it passes"; optional Pitch-Kurve.

### B6 — Distance Rolloff (min/maxDistance + rolloffMode) `[Support]`
- [ ] **gebaut** — *abhängig von:* BACKLOG „ADO Control Surface … min/maxDistance, rolloffMode"
- **Aha:** „Weggehen → leiser nach einer einstellbaren Kurve."
- **Bühne:** Emitter fix, Kamera entfernt sich; Vergleich linear vs. logarithmisch.
- **Sichtbar:** Distanz-Wert + Volume-Balken-Kurve; Caption „Configurable distance rolloff".

### B7 — 2D-Pan (panStereo) `[Support]`
- [ ] **gebaut** — *abhängig von:* BACKLOG „ADO Control Surface … `panStereo`"
- **Aha:** „2D-Sound wandert L↔R im Stereobild."
- **Bühne:** Slider/Automation schwenkt `panStereo` eines `PlayNonSpatial`-Sounds.
- **Sichtbar:** L/R-Pan-Anzeige; Caption „Stereo pan for 2D".

### B8 — Weitere Control-Surface-Felder (Sammelclip) `[Support]`
- [ ] **gebaut** — *abhängig von:* BACKLOG „ADO Control Surface härten + erweitern"
- **Aha:** „Pitch, Loop, Priority, Spread, ReverbZoneMix — alles im ADO, kein Code."
- **Bühne:** kurzer Inspector-B-Roll + je ein Mini-Vorher/Nachher pro Feld (nur die hörbar sinnvollen: `pitch`, `spread`, `loop`).
- **Sichtbar:** Inspector-Aufnahme + Captions je Feld. (Bewusst NICHT: `spatialize`, `ignoreListenerPause` — siehe BACKLOG-Begründung.)

### B9 — Proximity-VOIP mit Wand-Occlusion `[Hero]` (Zukunft, hart gated)
- [ ] **gebaut** — *abhängig von:* BACKLOG „Multiplayer & VOIP" (großes Vorhaben, Reißbrett)
- **Aha:** „Mitspieler-Stimme hinter Wand wird gedämpft — Proximity-Voice mit echtem Occlusion."
- **Bühne:** zwei Avatare (Capsules), einer redet (Live-PCM/Stream), läuft hinter Wand.
- **Sichtbar:** Cutoff-Meter auf der Stimme; Caption „Proximity voice — occluded behind walls". **Hinweis:** braucht Networking + VOIP-Stream-Pfad → eigenes, späteres Showcase.

---

## 4. Performance — der eine Stresstest (Sonderfall)

> **Nicht zeigbar als Einzel-Feature.** Object Pooling, Zero-GC, CTS-Reuse, WallCheck-Last, VR-Raycast-Batching sieht und hört man nicht. Sie alle bündeln sich in **einem** Stresstest, dessen „Aha" das **Ausbleiben** des Spikes ist: viel Last drauf, Profiler/Framerate bleiben flach.

### S1 — Stresstest „flacher Profiler" `[Hero]` ⭐ (Qualitäts-Beweis)
- [ ] **gebaut**
- **Aha:** „Salve aus vielen gleichzeitigen Sounds (+ aktive Wall-Checks) — und der Profiler **spiked nicht**, die Framerate hält."
- **Bühne:** Knopf spawnt eine Dauer-Salve One-Shots (Explosion/Impact) aus vielen Emittern; ein Teil davon mit `Use Wall Check = true`, damit auch die Raycast-Last mitläuft.
- **ADO/Config:** One-Shot-ADO; Pool großzügig dimensioniert (`Number Of Audio Sources`).
- **Sichtbar:** **Pflicht** — GC-Alloc-/FPS-Overlay oder danebengelegtes Profiler-Fenster; flache Linie ist der Star. Caption „~30+ sounds & wall-checks — zero runtime GC, stable framerate".
- **Was hier zusammenläuft (alles ein flacher Graph, kein eigener Beat):**
  - **Object Pooling / Zero-GC** — vorallokierter Pool, keine Laufzeit-Instanziierung.
  - **CTS-Reuse im WallCheck** — sobald gebaut (BACKLOG): keine Token-Allocations pro `Play()`. Vorher als „bekannte Mini-Allocation" ehrlich im Hinterkopf behalten.
  - **WallCheck-Last bei vielen Quellen** — Intervall-Raycasts bleiben beherrschbar.
- **Zukunfts-Erweiterung (gated):** **VR via RaycastCommand** (BACKLOG) — derselbe Stresstest auf Mobile-VR/Quest, als **Vorher/Nachher-Chart** (Main-Thread-Raycasts → gebatchte Jobs). Kein „Aha"-Shot, sondern Benchmark-Balken; nur wenn VR Zielgruppe wird.

---

## 5. Nicht zeigbar — anders kommunizieren

Akustisch/visuell **nicht** ehrlich darstellbar (kein erfundenes „Aha") — über Store-Text, Doku-Badges und „ships with N passing tests" transportieren:

- [ ] **Generation-/Stale-Handle-Sicherheit** — alter Handle stoppt nach Slot-Reuse nicht den fremden Sound. (Korrektheits-/Trust-Feature → Copy + Test-Count.)
- [ ] **UniTask-/Coroutine-Fallback** — interner Mechanismus mit automatischem Umschalten. (Doku/Console-Log, kein Video-Beat.)
- [ ] **Occlusion-Spawn-Snap** (Mini, falls gebaut) — Detailpolitur, im Video nicht von A3 unterscheidbar.

> Performance-Interna stehen bewusst **nicht** hier, sondern in Abschnitt 4 — sie sind über den flachen Profiler eben *doch* sichtbar machbar.

---

## 6. Gesamt-Bedarf (Sammel-Checkliste zum Beschaffen/Bauen)

**Audio-Clips:** Steady-Höhen-Loop · Motor-Loop · Footstep-Set · Explosion/Impact · Ambient A · Ambient B · Musik-2D · Dialog/Voice · Combat-Stem(s) · UI-Klick.

**Primitive/Prefabs:** Emitter-Box · bewegte Box (+ MoveBetween) · Wand-Prefabs pro Layer · Zonen-Trigger · Spieler-Capsule (FP-Controller) · Boden-Plane.

**Viz-/Demo-Scripts (nicht ausgeliefert):** Caption-Overlay · Cutoff-Meter · Raycast-Linie · Volume-Balken/Kategorie · GC/FPS-Overlay · UI-Canvas (Slider + Buttons) · MoveBetween · ZoneTrigger · Stress-Spawner (Abschnitt 4).

**Offene Abhängigkeiten vor finalem Dreh:**
- [ ] Produktives `AudioSystemConfig`-Asset existiert (BACKLOG).
- [ ] `[Hero]`-Planfeatures B1/B3 fertig (Mixer/Ducking, adaptive Layer) — sonst Trailer ohne die zwei stärksten Beats.
- [ ] Aufnahme-Setup: Auflösung/FPS/Format, Hintergrundmusik-Bett fürs Trailer-Audio, Schrift-/Caption-Stil festgelegt.

---

## 7. Vorschlag: kuratierter 60-s-Hero-Schnitt (optional)

Nur die `[Hero]`-Module, in einer Story-Reihenfolge — Rest lebt als Einzel-GIFs auf der Store-Seite. Grobe Timings (zum Justieren):

| t (s) | Beat | Modul |
|---|---|---|
| 0–6 | Hook: vorbeifahrende Box, Sound wandert + Doppler | A5 + B5 |
| 6–16 | Occlusion: hinter Wand → dumpf → weich zurück (Cutoff-Meter!) | A3 (+A4 kurz) |
| 16–26 | Adaptive Musik: Kampfzone → Combat-Layer faded ein | B3 |
| 26–34 | Ducking: Dialog senkt Musik, kommt zurück | B1 |
| 34–42 | Crossfade: Ambient-Zonenwechsel | A8 |
| 42–50 | Volume-Slider live + PauseAll | A9 (+A10 kurz) |
| 50–58 | Stresstest: Salve Sounds, GC/FPS-Overlay bleibt flach | S1 |
| 58–60 | Outro-Card: „Clean. Tested. Lightweight." + Logo | — |

> Reihenfolge bewusst: **Hook (Bewegung/Doppler) → Moat (Occlusion) → Wow (adaptive Musik) → Praxis (Ducking/Crossfade/Slider) → Beweis (Stresstest)**. Die Test-/Architektur-Qualität ist kein Video-Beat, sondern die Outro-Botschaft + Store-Copy.
