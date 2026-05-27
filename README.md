# Story Engine – Motor de Poveste Interactivă
## Proiect POO/PCLP3 – C# / Windows Forms / .NET 8

---

## 📁 Structura soluției

```
StoryEngine.sln
├── Story.Model/               ← Clase de date (POCO)
│   ├── StoryDefinition.cs
│   ├── StoryBlock.cs
│   ├── DecisionDefinition.cs
│   ├── StatePropertyDefinition.cs
│   ├── ConditionDefinition.cs    (+ ComparisonCondition, AndCondition, OrCondition)
│   ├── EffectDefinition.cs
│   └── ValidationResult.cs
│
├── Story.Engine/              ← Logica jocului
│   ├── GameState.cs           ← Starea runtime a jocului
│   ├── GameEngine.cs          ← Orchestrator principal
│   ├── ConditionEvaluator.cs  ← Evaluare recursivă AST
│   ├── EffectApplicator.cs    ← Aplicare efecte + redirect
│   └── StoryValidator.cs      ← Validare completă poveste
│
├── Story.Persistence/         ← Citire/scriere date
│   ├── StoryRepository.cs     ← ZIP + JSON
│   ├── ImageRepository.cs     ← Cache imagini
│   └── SaveStateRepository.cs ← Save/Load joc
│
├── Story.Player.WinForms/     ← Aplicația de citire
│   ├── Program.cs
│   └── Forms/
│       └── MainPlayerForm.cs
│
├── Story.Editor.WinForms/     ← Aplicația de editare
│   ├── Program.cs
│   ├── EditorContext.cs
│   ├── Forms/
│   │   └── MainEditorForm.cs
│   └── Dialogs/
│       ├── NewStoryDialog.cs
│       ├── DecisionDialog.cs
│       └── ConditionEditorDialog.cs
│
└── SampleStory/
    ├── story.json             ← Exemplu complet
    └── marea_evadare.zip      ← Pachet gata de folosit
```

---

## 🛠️ Cerințe

- **Visual Studio Community 2022** (v17.x sau mai nou)
- **.NET 8 SDK** (inclus în VS2022)
- Windows 10/11

---

## 🚀 Pași pentru deschidere în Visual Studio

1. Deschide Visual Studio Community 2022
2. Click **"Open a project or solution"**
3. Navighează la folderul `StoryEngine/`
4. Selectează `StoryEngine.sln`
5. VS va restaura automat pachetele NuGet (nu sunt necesare pachete externe)

---

## ▶️ Compilare și rulare

### Compilare întregii soluții:
```
Build → Build Solution (Ctrl+Shift+B)
```

### Rulare Player (aplicația de citire):
- Click dreapta pe `Story.Player.WinForms` în Solution Explorer
- **Set as Startup Project**
- F5 sau Ctrl+F5

### Rulare Editor (aplicația de editare):
- Click dreapta pe `Story.Editor.WinForms` în Solution Explorer
- **Set as Startup Project**
- F5 sau Ctrl+F5

---

## 📦 Pachete NuGet necesare

**Niciunul extern!** Proiectul folosește doar:
- `System.Text.Json` (inclus în .NET 8)
- `System.IO.Compression` (inclus în .NET 8)
- `System.Drawing` / `System.Windows.Forms` (incluse în .NET 8 Windows)

---

## 🎮 Cum folosești aplicația Player

1. Pornești `Story.Player.WinForms`
2. **Fișier → Deschide poveste** → selectezi `SampleStory/marea_evadare.zip`
3. Povestea se încarcă automat cu HUD-ul și blocul de start
4. Dai click pe butoanele de decizie pentru a avansa
5. **Fișier → Salvează starea** pentru a salva progresul
6. **Fișier → Restart** pentru a relua povestea

---

## ✏️ Cum folosești aplicația Editor

1. Pornești `Story.Editor.WinForms`
2. **Fișier → Poveste nouă** sau **Deschide...**
3. În **TreeView** (stânga) selectezi ce vrei să editezi:
   - `📖 Titlu` → editare metadate
   - `⚙ Proprietăți` → adaugă/editează proprietăți
   - `📦 Blocuri` → adaugă/editează blocuri
4. La un bloc, în panoul central apar deciziile (DataGridView)
   - **+ Adaugă decizie** → deschide dialogul complet
   - **✎ Editează** sau dublu-click pe o decizie → editare
5. **Poveste → Validare** verifică integritatea
6. **Fișier → Salvează** generează ZIP-ul final

---

## 📐 Arhitectura straturilor

```
┌─────────────────────────────────────────────┐
│         Story.Player.WinForms               │
│         Story.Editor.WinForms               │
│           (UI / Windows Forms)              │
├─────────────────────────────────────────────┤
│              Story.Engine                   │
│  GameEngine, ConditionEvaluator,            │
│  EffectApplicator, StoryValidator           │
├─────────────────────────────────────────────┤
│            Story.Persistence                │
│  StoryRepository (ZIP+JSON),                │
│  ImageRepository, SaveStateRepository       │
├─────────────────────────────────────────────┤
│              Story.Model                    │
│  StoryDefinition, StoryBlock,               │
│  DecisionDefinition, StatePropertyDef,      │
│  ConditionDefinition, EffectDefinition      │
└─────────────────────────────────────────────┘
```

---

## 📄 Formatul story.json

```json
{
  "title": "Titlul Poveștii",
  "author": "Autor",
  "startBlock": "intro.start",
  "version": "1.0",
  "properties": [
    {
      "key": "player.life",
      "hudLabel": "Viață",
      "min": 0, "max": 100, "initial": 100,
      "visibleInHud": true,
      "hudOrder": 1,
      "onMinBlock": "ending.death"
    }
  ],
  "blocks": [
    {
      "id": "intro.start",
      "text": "Textul narativ...",
      "isFinal": false,
      "backgroundImage": "images/forest.jpg",
      "decisions": [
        {
          "text": "Mergi înainte",
          "targetBlock": "next.block",
          "icon": "images/icon_walk.png",
          "condition": {
            "type": "COMPARISON",
            "property": "player.energy",
            "operator": ">=",
            "value": 10
          },
          "effects": [
            { "type": "ADD", "property": "player.energy", "value": -10 }
          ]
        }
      ]
    }
  ]
}
```

---

## ⚠️ Notă despre Microsoft.VisualBasic

`MainEditorForm.cs` folosește `Microsoft.VisualBasic.Interaction.InputBox` pentru dialoguri simple.
Dacă apare eroare de compilare, adaugă în `Story.Editor.WinForms.csproj`:

```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

Sau înlocuiește `InputBox` cu un dialog custom simplu:
```csharp
// În loc de:
var id = Microsoft.VisualBasic.Interaction.InputBox("...", "...", "...");

// Folosește:
var id = ShowInputDialog("ID bloc nou:", "Bloc nou", "bloc.nou");
```

Adaugă metoda helper în `MainEditorForm`:
```csharp
private static string ShowInputDialog(string prompt, string title, string defaultValue)
{
    var form = new Form { Text = title, Size = new Size(340, 130), StartPosition = FormStartPosition.CenterParent };
    var lbl = new Label { Text = prompt, Location = new Point(12, 12), AutoSize = true };
    var txt = new TextBox { Text = defaultValue, Location = new Point(12, 34), Width = 300 };
    var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(160, 62) };
    form.Controls.AddRange(new Control[] { lbl, txt, ok });
    form.AcceptButton = ok;
    return form.ShowDialog() == DialogResult.OK ? txt.Text : "";
}
```
