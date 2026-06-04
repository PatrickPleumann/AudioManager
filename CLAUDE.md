# AudioTool — CLAUDE.md

Dieses Dokument gibt Claude in einem neuen Chat den vollständigen Kontext über das Projekt.

---

## Was ist dieses Projekt?

**AudioTool** ist ein Unity Audio-Management-Framework das als Unity Asset Store Plugin veröffentlicht werden soll. Zielgruppe sind Indie-Entwickler und kleine Teams die kein Audio-Budget haben und sich nicht in FMOD/Wwise einarbeiten wollen.

Das Tool nimmt dem Entwickler die vollständige Verwaltung von `AudioSource`-Objekten ab. Ein einziger Aufruf reicht — den Rest erledigt das System.

**Wichtig:** Der Entwickler Patrick arbeitet mit Unity 6 und JetBrains Rider. Die Sprache im Chat ist Deutsch.

---

## Architektur

```
AudioManagerDynamic (MonoBehaviour — Singleton, Einstiegspunkt)
├── AudioPoolAcquisitionService   → Pool aus AudioObject[] (AudioSource + LowPassFilter)
├── AudioPlaybackService          → Dispatching & Stop-Logik
├── AudioUniTaskWallCheckService  → Raycast-Loop per UniTask (empfohlen)
├── AudioCoroutineWallCheckService → Raycast-Loop per Coroutine (Fallback)
├── AudioPauseService             → Pause/Unpause aller Pool-Slots
└── AudioManagerDictionaryProvider → Volume- & LayerMask-Dictionaries
```

### Wichtige Klassen & Dateien

| Datei | Zweck |
|---|---|
| `AudioManagerDynamic.cs` | Singleton, öffentliche API |
| `AudioDataObject.cs` | ScriptableObject — Konfiguration pro Sound (ADO) |
| `AudioSystemConfig.cs` | ScriptableObject — zentrale System-Konfiguration |
| `AudioSourceVolume.cs` | ScriptableObject — Lautstärke pro Kategorie |
| `AudioVolumesTransferObject.cs` | Bündelt alle AudioSourceVolume-Assets |
| `AudioTypeProvider.cs` | Enum — Lautstärke-Kategorien (Beispielwerte, muss angepasst werden) |
| `AudioHandle.cs` | Readonly struct — Referenz auf einen Pool-Slot zum Stoppen |
| `AudioObject.cs` | Struct — ein Pool-Slot (GameObject, AudioSource, Filter, BusyUntilTime) |
| `IAudioWallCheckService.cs` | Interface — Strategy Pattern für WallCheck |
| `IGetPoolIndex.cs` | Interface — bewusst noch nicht vollständig genutzt (Lightweight Pool geplant) |

---

## Öffentliche API

```csharp
AudioHandle handle = AudioManagerDynamic.Play(myADO);  // Sound abspielen
AudioManagerDynamic.Stop(handle);                       // Sound stoppen (nur wenn canHandleAudioSource == true)
AudioManagerDynamic.PauseAll();                         // Alle Sounds pausieren
AudioManagerDynamic.UnpauseAll();                       // Alle Sounds fortsetzen
```

---

## Wichtige Designentscheidungen

### Pool
- Festes `AudioObject[]`-Array, vorab instanziiert — kein GC zur Laufzeit
- `BusyUntilTime` als Zeitstempel-Trick für OneShot-Slots
- Pool-Suche O(n) von Index 0 — bewusst einfach gehalten

### Wall Check
- Nutzt `Physics.RaycastNonAlloc` mit `RaycastHit[8]`-Buffer (max. 8 Wände)
- Layer-basierte **Minuenden** — jeder getroffene Layer reduziert die Cutoff Frequency um einen konfigurierbaren Wert
- `MinCutoffFreqValue` als untere Grenze (10 = praktisch unhörbar)
- WallCheck nur wenn `IsCurrentlyActive()` → kein Raycast bei pausierten Sounds
- `ShouldContinueLoop()` unterscheidet OneShot (BusyUntilTime) und Loop (isPlaying)

### Token-Management
- `CancellationTokenSource[]` liegt **ausschließlich** im jeweiligen WallCheck-Service
- `AudioManagerDynamic` kennt keine Tokens — vollständige Interface-Abstraktion

### AudioDataObject (ADO)
- `canHandleAudioSource` — steuert ob `Play()` einen validen `AudioHandle` zurückgibt
- `UseWallCheck` — wird in `AudioPlaybackService` geprüft bevor `StartWallCheckLoop` aufgerufen wird
- `CallerTransform` — muss vor `Play()` per Code gesetzt werden

### AudioVolumesTransferObject
- Kein `CreateAssetMenu` — es darf nur eine Instanz geben
- Wird per `Populate Array`-Button im Inspector befüllt (Editor-only, kein `Awake()`)

### Singleton-Schutz
- Mehrere Instanzen von `AudioManagerDynamic` werden erkannt und zerstört mit Warning

---

## Offene Punkte / Geplante Features

> **Test-Pflicht (nicht verhandelbar):** Jedes neue Feature wird test-first gebaut — kein Test wird grün, bevor wir ihn rot gesehen haben. Tests sind Teil jedes Features, kein separater Punkt. Bestandscode wird später sukzessive nachgetestet (gesonderte Aufgabe unten).

### Vor Release (1.0) — fest eingeplant

- **✅ Fade-Familie (fertig & getestet)** — `AudioFadeService` (LateUpdate-getrieben, analog `AudioFollowService`). Public API: `FadeInNonSpatial` / `FadeOut` / `CrossfadeNonSpatial` + spatiale Varianten `FadeInSpatial` / `CrossfadeSpatial`. Crossfade = Komposition aus FadeOut + FadeIn. 30 EditMode-Tests, pause-aware, alle Reset-Punkte geschlossen.
- **Mixer/Bus + Ducking** — Lautstärke-Kategorien an Unity-AudioMixer-Groups koppeln, Laufzeit-Volume (Settings-Menü) und Ducking (z. B. Dialog senkt Musik). Baut auf dem bestehenden `AudioCategory`/VolumeDictionary-System auf. Höchster Verkaufshebel.
- **Random Pitch/Volume-Variation** — pro ADO optionale Streuung gegen den „Maschinengewehr-Effekt" bei wiederholten One-Shots (Footsteps, Hülsen).
- **Adaptives/interaktives Musik-Layer** — mehrere Musik-Stems, die je nach Spielzustand ein-/ausgeblendet werden (nutzt die Fade-Familie). Größter Abstand zu FMOD/MasterAudio, größter „Wow"-Effekt — und der größte Brocken.
- **Tooltips** — alle Inspector-Felder sollen vor Veröffentlichung vollständige Tooltips bekommen (insb. `IsOneShot`, `canHandleAudioSource`, `UseWallCheck`)
- **`IGetPoolIndex` entfernen** — zweckloser Platzhalter (Lightweight-Pool nicht mehr geplant, Rationale via per-Sound-Flags gelöst)
- **Null-Einträge in `CurrentClips`** validieren — Editor-Warnung statt lautlosem Sound
- **PDF-Dokumentation** — `.md`-Dateien existieren, Konvertierung zu PDF noch offen
- **Ordnerstruktur für Asset Store** — noch nicht definiert

### Weitere geplante Features (Priorität offen — können auch noch vor 1.0 rein)

- **Layer-basierte reaktive Geräusche** — Sounds sollen je nach getroffenem Layer unterschiedlich sein. Beispiel: Footstep auf Holz vs. auf Boden vs. auf Metall. Vermutlich Mapping von Layer/Material → AudioClip(-Gruppe), analog zum bestehenden Layer→Cutoff-Dictionary-Ansatz.
- **Multiplayer & VOIP** — networked Sound-Positionen + Proximity-Voice mit Wall-Occlusion (VOIP = Live-PCM-Stream → braucht „bring-your-own-AudioSource"-Pfad).
- **VR-Optimierung via RaycastCommand** — aktuell feuern Wall-Checks `Physics.Raycast` einzeln auf dem Main Thread. Für mobiles VR (Quest) ist das bei vielen gleichzeitigen Quellen kritisch. Umstieg auf `RaycastCommand` (gebatcht, jobified über Burst/Job System) verteilt die Raycasts über mehrere Threads. Konkreter nächster Schritt wenn VR als Zielgruppe genannt werden soll.
  - Architektur ist sonst schon VR-tauglich (kein GC zur Laufzeit, Pooling, UniTask, ein AudioListener)
  - Wichtig bei Positionierung: Wall-Check ist **lightweight occlusion** (simpler Low-Pass), KEIN voller Spatializer wie Steam Audio/Oculus. Das ist ein Verkaufsargument, kein Nachteil — klar so kommunizieren.
- **CTS-Reuse im WallCheck** — pro `Play()` wird aktuell eine CancellationTokenSource alloziert ("Zero GC" stimmt deshalb noch nicht ganz).

### Gesonderte Aufgabe (nach den ersten Features)

- **Bestandscode nachtesten** — bestehende Methoden (wo sinnvoll möglich) sukzessive mit Unit Tests abdecken; ggf. kleine Refactorings für Testbarkeit (pure Logik aus Unity-Abhängigkeiten herausziehen).

---

## Was NICHT angefasst werden soll ohne Rücksprache

- `IGetPoolIndex` — bewusst unvollständig, wird für Lightweight Pool gebraucht
- `AudioCoroutineWallCheckService` — Fallback, nur parallel zu UniTask-Version anpassen
- `TestScript.cs` — nur zum Testen, kein Produktionscode

---

## Workflow mit Patrick

- Patrick liest jeden Dokumentationsabschnitt gegen bevor er eingefügt wird
- Keine Änderungen ohne explizites Go von Patrick
- Ehrliche Einschätzungen erwünscht — kein "AI-Ja-Sager"
- Sprache: Deutsch im Chat, Code auf Englisch
- Vor jeder größeren Änderung: erklären was geändert wird und warum, dann warten
