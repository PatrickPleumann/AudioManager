# AudioTool — Dokumentation

---

## Was ist AudioTool?

**AudioTool** ist ein schlankes Audio-Management-Framework für Unity, das Entwicklern die vollständige Verwaltung von `AudioSource`-Objekten abnimmt. Statt `AudioSource`-Komponenten manuell zu instanziieren, zu konfigurieren und zu verwalten, reicht ein einziger Aufruf — den Rest erledigt das System.

---

## Vorteile

- **Kein manuelles AudioSource-Management** — Das Tool verwaltet einen vorallokierten Pool aus `AudioSource`-Objekten. Zur Laufzeit werden keine neuen Objekte instanziiert, was Garbage Collection und Performance-Spitzen vermeidet.

- **Automatische Wand-Okklusion (Wall Check)** — Sounds die hinter Wänden oder Hindernissen entstehen, werden automatisch mit einem `AudioLowPassFilter` gedämpft. Mehrere Wände auf dem Weg zum Spieler dämpfen den Sound stärker als eine einzelne — konfigurierbar pro Unity-Layer.

- **Organisiertes Volumen-System** — Sounds werden kategorisiert (z.B. Ambient, SFX, Player). Jede Kategorie hat ein eigenes `AudioSourceVolume`-Asset dessen Wert zur Laufzeit überschrieben werden kann — ideal für Lautstärke-Slider in einem Einstellungsmenü.

- **Fire-and-Forget oder steuerbar** — Der Entwickler entscheidet pro Sound ob er einen `AudioHandle` zurückbekommen möchte um den Sound später manuell zu stoppen, oder ob der Sound einfach durchläuft.

- **Einfache API** — Sounds abspielen und stoppen geschieht über selbsterklärende statische Methoden. Kein Event-System, kein Boilerplate-Code.

- **ScriptableObject-getrieben** — Die gesamte Konfiguration erfolgt über Assets im Inspector. Kein Code nötig um neue Sounds einzubinden.

- **UniTask-basiert** — Die interne Async-Logik nutzt UniTask für minimalen Overhead. Eine Coroutine-Variante steht als Fallback zur Verfügung.

---

## Setup & Installation

### Voraussetzungen

- Unity 6 oder höher
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

## AudioSystemConfig — Referenz

Das `AudioSystemConfig`-Asset ist die zentrale Konfigurationsdatei des Tools. Alle Werte können im Inspector angepasst werden.

---

### General Values

| Feld | Beschreibung | Empfehlung |
|---|---|---|
| **Numbers Of Audio Sources** | Anzahl der vorallokierten `AudioSource`-Objekte im Pool. Je mehr Sounds gleichzeitig abgespielt werden sollen, desto höher sollte dieser Wert sein. Zu viele Objekte erhöhen den Speicherverbrauch. | 20–50 für die meisten Projekte |
| **Default Cutoff Freq Value** | Der Standard-Frequenzwert des `AudioLowPassFilter` wenn kein Wandkontakt besteht. Bei diesem Wert klingt der Sound normal und ungefiltert. | 5000 – 5007 |
| **Min Cutoff Freq Value** | Die untere Grenze der Cutoff Frequency. Die Frequenz wird nie unter diesen Wert gesenkt — egal wie viele Wände sich zwischen Sound und Spieler befinden. Auf 10 setzen wenn Sounds bei vielen Wänden komplett unhörbar werden sollen. | 10 – 1000 |

---

### Wall Check Interval

| Feld | Beschreibung | Empfehlung |
|---|---|---|
| **Time Interval Between Position Checks** | Der Abstand in Sekunden zwischen zwei Wall-Check-Raycasts. Kleinere Werte reagieren schneller aber kosten mehr Performance. | 0.1 – 0.25 |

---

### Cutoff Frequencies Per Layer

Eine Liste von Unity-Layern die als Wände gelten, jeweils mit einem Reduktionswert. Für jeden Layer-Treffer des Raycasts wird die Cutoff Frequency um den definierten Wert reduziert. Mehrere Treffer werden addiert.

| Feld | Beschreibung |
|---|---|
| **Single Layer** | Der Unity-Layer der als Wand gilt. |
| **Cutoff Frequency Value** | Der Wert um den die Cutoff Frequency pro Treffer reduziert wird. |

---

### References

| Feld | Beschreibung |
|---|---|
| **Transfer Object** | Das mitgelieferte `AudioVolumesTransferObject`-Asset. Enthält alle `AudioSourceVolume`-Assets die vom System verwaltet werden. |
| **Audio GameObject Prefab** | Das mitgelieferte `3DAudioSourceObject`-Prefab. Wird für jeden Pool-Slot instanziiert. Dieses Feld nicht verändern. |

---

## Performance Tipps

### Pool-Größe

**Numbers Of Audio Sources** hat direkten Einfluss auf den Speicherverbrauch. Alle Pool-Objekte werden beim Start der Szene instanziiert — auch wenn sie nie genutzt werden. Setze den Wert so niedrig wie möglich, aber hoch genug um alle gleichzeitig erwarteten Sounds abdecken zu können.

> Faustregel: Lieber mit einem niedrigen Wert starten und bei Bedarf erhöhen, als von Anfang an zu hoch ansetzen.

---

### Wall Check Intervall

**Time Interval Between Position Checks** bestimmt wie oft pro Sekunde ein Raycast abgefeuert wird. Für das menschliche Gehör ist der Unterschied zwischen 0.1 und 0.25 Sekunden kaum wahrnehmbar — aber der Performance-Unterschied bei vielen gleichzeitigen Wall-Check-Sounds ist spürbar.

> Faustregel: 0.25 ist ein guter Standardwert. Nur auf kleinere Werte gehen wenn eine sehr schnelle Reaktion auf Wandkontakt nötig ist.

---

### Set Caller As Parent

**Set Caller As Parent** im ADO sollte nur aktiviert werden wenn der Sound sich wirklich mit einem Objekt mitbewegen muss — z.B. ein fahrendes Fahrzeug. Für kurze Sounds wie Schüsse oder Explosionen ist diese Option unnötig und kostet Performance.

> Faustregel: Bei kurzen Sounds immer deaktiviert lassen.

---

### Use Wall Check

**Use Wall Check** sollte nur aktiviert werden wenn der Sound wirklich hinter einer Wand entstehen kann. Für Sounds die immer in offenen Bereichen abgespielt werden — z.B. UI-Sounds oder Musik — ist der Wall Check unnötige Raycast-Last.

> Faustregel: Im Zweifel deaktiviert lassen und nur gezielt aktivieren.

---

### Anzahl gleichzeitiger Wall Checks

Jeder aktive Sound mit **Use Wall Check** feuert in regelmäßigen Intervallen einen Raycast. Bei vielen gleichzeitig spielenden Sounds mit Wall Check summiert sich das schnell. Es lohnt sich zu überlegen welche Sounds wirklich einen Wall Check benötigen und welche nicht.

> Faustregel: Wall Check nur für Sounds aktivieren die sich in der Nähe von Wänden befinden können — z.B. Gegner-Voicelines, aber nicht Footsteps des Spielers.

---

## Volumen-System einrichten

### Schritt 3 — AudioTypeProvider anpassen

Der `AudioTypeProvider` ist ein Enum der die verfügbaren Lautstärke-Kategorien definiert. Er befindet sich unter:
> `AudioFramework/Core/AudioTypeProvider.cs`

Die vorhandenen Werte sind **ausschließlich Beispielwerte** und sollen vollständig durch eigene Kategorien ersetzt werden:

```csharp
public enum AudioTypeProvider
{
    // Beispielwerte - bitte vollständig durch eigene Kategorien ersetzen:
    Ambient = 1,
    Music,
    SFX,
    // ...
}
```

> **Hinweis:** Einmal angelegte Enum-Werte sollten nicht in ihrer Reihenfolge verändert werden, da dies die betroffenen `AudioDataObject`- und `AudioSourceVolume`-Assets beeinflusst. Neue Werte können jederzeit bedenkenlos am Ende hinzugefügt werden. Der erste Enum-Wert muss immer explizit auf `= 1` gesetzt werden.

---

### Schritt 4 — AudioSourceVolume Assets anlegen

Das Tool verwaltet Lautstärken über `AudioSourceVolume`-Assets. Jedes Asset repräsentiert eine Lautstärke-Kategorie aus dem `AudioTypeProvider`.

Erstelle für jede gewünschte Kategorie ein neues Asset über:
> **Rechtsklick im Project-Fenster → Create → Scriptable Objects → AudioSourceVolume**

| Feld | Beschreibung |
|---|---|
| **Current Audio Type** | Die Kategorie dieses Assets — muss mit dem `AudioTypeProvider`-Wert im zugehörigen `AudioDataObject` übereinstimmen. |
| **Volume** | Der Standardlautstärkewert (0.0 – 1.0). Dieser Wert kann zur Laufzeit überschrieben werden, z.B. durch einen Einstellungs-Slider. |

---

### Schritt 5 — AudioVolumesTransferObject befüllen

Das `AudioVolumesTransferObject` ist bereits im mitgelieferten `AudioSystemConfig`-Asset eingetragen und muss nicht neu angelegt werden.

Klicke im Inspector auf das `AudioVolumesTransferObject` und drücke den Button **Populate Array**. Das System sucht automatisch alle vorhandenen `AudioSourceVolume`-Assets im Projekt und trägt sie ein.

> **Wichtig:** Dieser Schritt muss jedes Mal wiederholt werden, wenn neue `AudioSourceVolume`-Assets hinzugefügt werden.

---

## Wall Check konfigurieren

### Schritt 6 — Unity Layer definieren

Der Wall Check nutzt Unity-Layer um zu bestimmen welche Objekte als Wände gelten. Definiere zunächst die gewünschten Layer in Unity:
> **Edit → Project Settings → Tags and Layers**

Beispiele für sinnvolle Layer-Namen: `WallThick`, `WallThin`, `WallGlass`.

---

### Schritt 7 — Cutoff Frequencies Per Layer konfigurieren

Öffne das `AudioSystemConfig`-Asset im Inspector. Unter **Cutoff Frequencies Per Layer** kannst du für jeden Wand-Layer einen Reduktionswert definieren.

Klicke auf **+** um einen neuen Eintrag hinzuzufügen und weise ihm folgende Werte zu:

| Feld | Beschreibung |
|---|---|
| **Single Layer** | Der Unity-Layer der als Wand gilt. |
| **Cutoff Frequency Value** | Der Wert um den die Cutoff Frequency reduziert wird wenn dieser Layer vom Raycast getroffen wird. |

**Beispiel:**

| Layer | Reduktion | Ergebnis bei Default 5000 |
|---|---|---|
| WallThin | 500 | 4500 |
| WallThick | 2000 | 3000 |
| WallThin + WallThick | 500 + 2000 | 2500 |

---

### Schritt 8 — Minimalwert konfigurieren

Das Feld **Min Cutoff Freq Value** im `AudioSystemConfig`-Asset definiert die untere Grenze der Cutoff Frequency. Die Frequenz wird nie unter diesen Wert gesenkt, egal wie viele Wände sich zwischen Sound und Spieler befinden.

| Szenario | Empfohlener Wert |
|---|---|
| Sound soll immer hörbar bleiben | 500 – 1000 |
| Sound soll bei vielen Wänden unhörbar werden | 10 (Unitys absolutes Minimum) |

> **Hinweis:** Unity's `AudioLowPassFilter` akzeptiert als Minimalwert 10 Hz. Ein Wert von 10 macht den Sound für den Spieler praktisch unhörbar.

---

## AudioDataObject (ADO)

Das `AudioDataObject` (kurz: ADO) ist das zentrale Konfigurationsobjekt für jeden Sound. Erstelle ein neues ADO über:
> **Rechtsklick im Project-Fenster → Create → Scriptable Objects → AudioDataObject**

| Feld | Beschreibung |
|---|---|
| **Current Clips** | Die AudioClips die für diesen Sound verwendet werden. Ein ADO repräsentiert immer genau eine Klang-Kategorie — z.B. mehrere Footstep-Varianten oder eine einzelne Explosion. Bei mehreren Clips wählt das System bei jedem Abspielen zufällig einen aus. Ein einzelner Clip ist völlig ausreichend. |
| **Current Type** | Die Lautstärke-Kategorie dieses Sounds — muss einem vorhandenen `AudioTypeProvider`-Wert entsprechen. |
| **Caller Transform** | Die Position in der Welt an der der Sound abgespielt wird. Wird zur Laufzeit per Code gesetzt. |
| **Set Caller As Parent** | Wenn aktiv, wird das Audio-Objekt an den `CallerTransform` gehängt und bewegt sich mit ihm — z.B. für einen fahrenden Wagen. Für kurze Sounds deaktiviert lassen. |
| **Is One Shot** | Wenn aktiv, wird der Sound einmalig abgespielt und gibt die AudioSource danach automatisch frei. |
| **Can Handle Audio Source** | Wenn aktiv, gibt `Play()` einen `AudioHandle` zurück mit dem der Sound manuell gestoppt werden kann. |
| **Use Wall Check** | Wenn aktiv, prüft das System in regelmäßigen Intervallen ob sich eine Wand zwischen Sound und Spieler befindet und dämpft den Sound entsprechend. |

---

## API Referenz

### Sound abspielen

```csharp
AudioHandle handle = AudioManagerDynamic.Play(myAudioDataObject);
```

Spielt den Sound des übergebenen `AudioDataObject` ab. Gibt einen `AudioHandle` zurück wenn **Can Handle Audio Source** im ADO aktiviert ist, andernfalls einen ungültigen Handle.

> **Wichtig:** Der `CallerTransform` muss vor dem Aufruf gesetzt sein — er definiert die Position des Sounds in der Welt.

```csharp
// Beispiel: Sound an einer bestimmten Position abspielen
myAudioDataObject.CallerTransform = transform;
AudioHandle handle = AudioManagerDynamic.Play(myAudioDataObject);
```

---

### Sound stoppen

```csharp
AudioManagerDynamic.Stop(handle);
```

Stoppt den Sound des übergebenen `AudioHandle`. Nur verfügbar wenn **Can Handle Audio Source** im ADO aktiviert ist.

---

### Alle Sounds pausieren / fortsetzen

```csharp
AudioManagerDynamic.PauseAll();
AudioManagerDynamic.UnpauseAll();
```

Pausiert oder setzt alle aktuell spielenden Sounds fort — z.B. beim Öffnen eines Pause-Menüs.

---

## Beispiel: Explosion

In diesem Beispiel wird ein einmaliger Explosions-Sound an einer bestimmten Position abgespielt. Der Sound ist nicht steuerbar — er spielt einmal durch und gibt die AudioSource danach automatisch frei.

**1. ADO konfigurieren**

Erstelle ein neues `AudioDataObject` und konfiguriere es im Inspector:

| Feld | Wert |
|---|---|
| **Current Clips** | ExplosionClip |
| **Current Type** | SFX |
| **Set Caller As Parent** | false |
| **Is One Shot** | true |
| **Can Handle Audio Source** | false |
| **Use Wall Check** | true |

**2. Sound per Code abspielen**

```csharp
public class ExplosionHandler : MonoBehaviour
{
    [SerializeField] private AudioDataObject explosionADO;

    public void Explode()
    {
        explosionADO.CallerTransform = transform;
        AudioManagerDynamic.Play(explosionADO);
    }
}
```

---

## Beispiel: Motor-Loop mit Stop

In diesem Beispiel wird ein Motor-Sound geloopt der manuell gestoppt werden kann — z.B. wenn das Fahrzeug abschaltet.

**1. ADO konfigurieren**

| Feld | Wert |
|---|---|
| **Current Clips** | EngineLoopClip |
| **Current Type** | Ambient |
| **Set Caller As Parent** | true |
| **Is One Shot** | false |
| **Can Handle Audio Source** | true |
| **Use Wall Check** | false |

**2. Sound per Code abspielen und stoppen**

```csharp
public class VehicleEngine : MonoBehaviour
{
    [SerializeField] private AudioDataObject engineADO;
    private AudioHandle engineHandle;

    public void StartEngine()
    {
        engineADO.CallerTransform = transform;
        engineHandle = AudioManagerDynamic.Play(engineADO);
    }

    public void StopEngine()
    {
        AudioManagerDynamic.Stop(engineHandle);
    }
}
```
