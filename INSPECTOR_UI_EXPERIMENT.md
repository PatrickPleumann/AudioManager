# Zwischenprojekt: AudioDataObject-Inspector (Experiment)

> **Status:** Experiment, nicht Teil des Kanons. Diese Datei ist bewusst temporär —
> sie darf gelöscht werden, sobald entschieden ist, ob die UI bleibt.
> Solange sie existiert, ist sie die einzige Heimat dieses Wissens.

---

## 1. Backup: So sah der Inspector VORHER aus

**Es gab keinen Custom Editor für `AudioDataObject`.** Unity hat den Default-Inspector aus den
öffentlichen Feldern generiert. Der Zustand ist damit vollständig durch den Feld-Aufbau von
[`AudioDataObject.cs`](Assets/Scripts/AudioFramework/Data/AudioDataObject.cs) beschrieben — und der wurde
**nicht angefasst**.

Reihenfolge und Darstellung im alten Default-Inspector (von oben nach unten):

| # | Feld | Default-Darstellung | Trenner |
|---|---|---|---|
| 1 | `CurrentClips` (`AudioClip[]`) | aufklappbare Reorderable-List, Tooltip aus `[Tooltip]` | `[Space]` danach |
| 2 | `CurrentType` (`AudioCategory`) | Enum-Popup, Tooltip | `[Space]` danach |
| 3 | `SpatialBlend` (`float`) | Slider 0–1 via `[Range]`, Tooltip | `[Space]` danach |
| 4 | `FollowEmitter` (`bool`) | Checkbox, Tooltip, `[FormerlySerializedAs("SetCallerAsParent")]` | — |
| 5 | `IsOneShot` (`bool`) | Checkbox, kein Tooltip | — |
| 6 | `CanHandleAudioSource` (`bool`) | Checkbox, kein Tooltip | — |
| 7 | `UseWallCheck` (`bool`) | Checkbox, kein Tooltip | `[Space]` danach |
| 8 | `RespectsGlobalPause` (`bool`, default `true`) | Checkbox, Tooltip, `[FormerlySerializedAs("CanBePaused")]` | — |

Darunter Unitys Standard-Fußzeile (Asset-Labels / AssetBundle-Zeile). Keine Buttons, keine Validierung,
keine Vorschau, keine Gruppierung — alles flach untereinander.

## 2. Wie man diesen Zustand exakt wiederherstellt

Das Experiment hat **keine bestehende Datei verändert** — es sind ausschließlich neue Dateien
dazugekommen. Der Rückweg ist deshalb reines Löschen:

```bash
git clean -nd Assets/Scripts/AudioFramework/Editor
```

Erst mit `-nd` ansehen, dann mit `-fd` löschen. Betroffen sind nur:

- `Assets/Scripts/AudioFramework/Editor/AudioDataObjectEditor.cs` — Layout des Inspectors
- `Assets/Scripts/AudioFramework/Editor/AudioDataObjectInspectorModel.cs` — Befunde, Zusammenfassung, Snippet
- `Assets/Scripts/AudioFramework/Editor/AudioInspectorSkin.cs` — Palette, Styles, Zeichen-Primitive
- `Assets/Scripts/AudioFramework/Editor/AudioClipPreviewPlayer.cs` — Vorschau via `UnityEditor.AudioUtil`
- `Assets/Scripts/AudioFramework/Editor/CategoryVolumeLocator.cs` — Volume-Auflösung aus dem Projekt
- die zugehörigen `.meta`-Dateien (erzeugt Unity beim Import)

Sind die Dateien weg, fällt Unity automatisch auf den Default-Inspector aus Tabelle 1 zurück.
`git status` war vor dem Experiment sauber (`master`, `ebc8764`) — die Historie ist die zweite Absicherung.

## 3. Was die neue UI tut (und was nicht)

- **Kein Runtime-Code angefasst.** Alles liegt im Editor-Assembly (`AudioFramework.Editor`, `includePlatforms: Editor`)
  und wandert nicht in den Build.
- **Keine Feld-Semantik geändert.** Die UI schreibt über `SerializedProperty` in exakt dieselben Felder;
  Undo/Redo, Multi-Object-Editing und Prefab-Overrides laufen über Unitys Standardweg.
- **UI-Sprache ist Englisch**, wie der übrige Code — das Tool geht in den Asset Store.

## 4. Wenn `AudioDataObject` neue Felder bekommt

Der Inspector ist **additiv gebaut**: er zeichnet keine feste Feldliste, sondern *beansprucht* Properties.

- Jede Property wird in `OnEnable` über `Claim(nameof(...))` geholt. `Claim` merkt sich den Namen.
- Am Ende läuft `DrawUnclaimedSection` über **alle** sichtbaren Felder des Assets und zeichnet jedes, das
  nicht beansprucht wurde, im Abschnitt **„Not laid out yet"** mit Unitys Default-Control.

Daraus folgt das Verhalten, das die Sorge auflöst:

| Fall | Was passiert |
|---|---|
| **Feld hinzugefügt** | Erscheint automatisch unten in „Not laid out yet" — sichtbar und editierbar, ohne dass eine Zeile am Editor geändert wird. |
| **Feld dort belassen** | Funktioniert dauerhaft. Der Abschnitt ist kein Fehler, sondern eine Warteschlange. |
| **Feld schön einsortieren** | Zwei Zeilen: `Claim(nameof(...))` in `OnEnable` + ein Aufruf im passenden Abschnitt (`DrawOptionRow` für `bool`, `DrawPropertyRow` für alles andere). Es verschwindet dadurch von selbst aus „Not laid out yet" — die Liste kann gar nicht mit dem Layout auseinanderlaufen, weil beide aus derselben Quelle stammen. |
| **Feld entfernt** | `nameof(AudioDataObject.X)` **kompiliert nicht mehr**. Bewusst so: der Compiler zeigt exakt die zwei Zeilen, die weg müssen. Ein lautes Signal ist hier besser als eine still verschwindende Zeile. |
| **Feld umbenannt** | Rider-Rename fasst `nameof` mit an — nichts zu tun. |

Nicht automatisch mitwachsend sind die **Prosa-Zusammenfassung** und die **Befunde** (`AudioDataObjectInspectorModel`).
Ein neues Feld taucht dort erst auf, wenn es in `AudioDataObjectSnapshot` aufgenommen und in `Describe` /
`Validate` verwendet wird. Das ist Absicht: ein Satz über ein Feld lässt sich nicht generisch erzeugen.

## 5. Offen, falls die UI bleibt

Die Entscheidungslogik (Validierungs-Befunde, Zusammenfassungssatz, Code-Snippet) liegt bewusst in einer
eigenen, Unity-GUI-freien Klasse (`AudioDataObjectInspectorModel`). Sie ist damit **testbar geschnitten**,
aber noch **nicht test-gedeckt** — das Experiment lief außerhalb des TDD-Loops. Wird die UI übernommen,
gehört sie unter die normale Disziplin aus [`TESTING.md`](TESTING.md) (red first) und der Punkt in
[`BACKLOG.md`](BACKLOG.md).
