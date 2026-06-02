# AudioTool — Dokumentation

---

## Was ist AudioTool?

**AudioTool** ist ein schlankes Audio-Management-Framework für Unity, das Entwicklern die vollständige Verwaltung von `AudioSource`-Objekten abnimmt. Statt `AudioSource`-Komponenten manuell zu instanziieren, zu konfigurieren und zu verwalten, reicht ein einziger Aufruf — den Rest erledigt das System.

---

## Vorteile

- **Kein manuelles AudioSource-Management** — Das Tool verwaltet einen vorallokierten Pool aus `AudioSource`-Objekten. Zur Laufzeit werden keine neuen Objekte instanziiert, was Garbage Collection und Performance-Spitzen vermeidet.

- **Automatische Wand-Okklusion (Wall Check)** — Sounds die hinter Wänden oder Hindernissen entstehen, werden automatisch mit einem `AudioLowPassFilter` gedämpft. Der Entwickler definiert nur welche Unity-Layer als Wände gelten — den Rest übernimmt das System per Raycast-Intervall.

- **Organisiertes Volumen-System** — Sounds werden kategorisiert (z.B. Ambient, SFX, Player). Jede Kategorie hat ein eigenes `AudioSourceVolume`-Asset dessen Wert zur Laufzeit überschrieben werden kann — ideal für Lautstärke-Slider in einem Einstellungsmenü.

- **Fire-and-Forget oder steuerbar** — Der Entwickler entscheidet pro Sound ob er einen `AudioHandle` zurückbekommen möchte um den Sound später manuell zu stoppen, oder ob der Sound einfach durchläuft.

- **Einfache API** — Sounds abspielen und stoppen geschieht über drei selbsterklärende statische Methoden. Kein Event-System, kein boilerplate Code.

- **ScriptableObject-getrieben** — Die gesamte Konfiguration erfolgt über Assets im Inspector. Kein Code nötig um neue Sounds einzubinden.

- **UniTask-basiert** — Die interne Async-Logik nutzt UniTask für minimalen Overhead. Eine Coroutine-Variante steht als Fallback zur Verfügung.

---

## Setup & Installation

### Voraussetzungen

- Unity 2022.3 oder höher
- **UniTask** — empfohlen, muss separat installiert werden:
  [https://github.com/Cysharp/UniTask](https://github.com/Cysharp/UniTask)
  *(Eine Coroutine-Variante steht als Fallback zur Verfügung, wird aber nicht empfohlen.)*

---

### Schritt 1 — AudioManagerDynamic in die Szene einbinden

Erstelle ein leeres GameObject in deiner Szene und füge die Komponente `AudioManagerDynamic` hinzu. Dieses Objekt ist der zentrale Einstiegspunkt des Tools und muss **einmal pro Szene** vorhanden sein.

---

### Schritt 2 — System Config zuweisen

Das mitgelieferte `AudioSystemConfig`-Asset ist bereits vollständig vorkonfiguriert. Weise es im Inspector des `AudioManagerDynamic`-GameObjects dem Feld **System Config** zu.

> Auf alle Felder der System Config wird später in einem eigenen Abschnitt detailliert eingegangen.

---

## Volumen-System einrichten

### Schritt 3 — AudioTypeProvider erweitern

Der `AudioTypeProvider` ist ein Enum der die verfügbaren Lautstärke-Kategorien definiert. Er befindet sich unter:
> `Assets/Scripts/AudioFramework/Core/AudioTypeProvider.cs`

Die vorhandenen Werte (`Ambient`, `Music`, `SFX`, `Player`, `BehindWall`) sind **ausschließlich Beispielwerte** und sollen an das eigene Projekt angepasst werden. Neue Kategorien können einfach am Ende des Enums hinzugefügt werden:

```csharp
public enum AudioTypeProvider
{
    Ambient = 1,
    Music,
    SFX,
    Player,
    BehindWall,
    // Eigene Kategorien hier hinzufügen:
    Dialogue,
    UI,
    // ...
}
```

> **Hinweis:** Werden nachträglich Enum-Werte verändert, müssen die betroffenen `AudioDataObject`-Assets entsprechend angepasst werden. `AudioSourceVolume`-Assets sind nur dann betroffen, wenn die Reihenfolge der Enum-Werte geändert wird.

---

### Schritt 4 — AudioSourceVolume Assets anlegen

Das Tool verwaltet Lautstärken über sogenannte `AudioSourceVolume`-Assets. Jedes Asset repräsentiert eine Lautstärke-Kategorie aus dem `AudioTypeProvider`.

Erstelle für jede gewünschte Kategorie ein neues Asset über:
> **Rechtsklick im Project-Fenster → Create → Scriptable Objects → AudioSourceVolume**

Weise jedem Asset im Inspector folgende Werte zu:

| Feld | Beschreibung |
|---|---|
| **Current Audio Type** | Die Kategorie dieses Assets — muss mit dem `AudioTypeProvider`-Wert im zugehörigen `AudioDataObject` übereinstimmen. |
| **Volume** | Der Standardlautstärkewert (0.0 – 1.0). Dieser Wert kann zur Laufzeit überschrieben werden, z.B. durch einen Einstellungs-Slider. |

---

### Schritt 5 — AudioVolumesTransferObject befüllen

Das `AudioVolumesTransferObject` ist bereits im mitgelieferten `AudioSystemConfig`-Asset eingetragen und muss nicht neu angelegt werden.

Klicke im Inspector auf das `AudioVolumesTransferObject` und drücke den Button **Populate Array**. Das System sucht automatisch alle vorhandenen `AudioSourceVolume`-Assets im Projekt und trägt sie ein.

> **Wichtig:** Dieser Schritt muss jedes Mal wiederholt werden, wenn neue `AudioSourceVolume`-Assets hinzugefügt werden.
