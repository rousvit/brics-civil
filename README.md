# BricsLayerPlugin – Vrstvy ve stylu Vectorworks pro BricsCAD

Plugin pro BricsCAD, který implementuje systém vrstev a tříd ve stylu Vectorworks.

## Koncept

### Vrstvy (Layers)
- Vrstvy fungují jako **překryvné folie** – zobrazují se nad sebou dle pořadí
- Pořadí se mění pouze posunem vrstvy nahoru/dolů
- Každá vrstva má tři stavy:
  - **On** – viditelná a editovatelná
  - **Off** – neviditelná
  - **Grayed** – viditelná, ale zašedlá (nelze editovat)
- Stavy platí pro všechny objekty na vrstvě

### Třídy (Classes)
- Třída definuje **vizuální vlastnosti** objektů:
  - Barva (ACI index)
  - Styl čáry (Continuous, Dashed, …)
  - Tloušťka čáry (mm)
- Objektu se přiřadí třída a ta mu nastaví vzhled
- Změna třídy se automaticky projeví na všech přiřazených objektech

## Příkazy

### Vrstvy
| Příkaz | Popis |
|--------|-------|
| `VW_LAYERS` | Otevře paletu pro správu vrstev |
| `VW_LAYER_NEW` | Vytvoří novou vrstvu |
| `VW_LAYER_STATE` | Nastaví stav vrstvy (On/Off/Grayed) |
| `VW_LAYER_UP` | Posune vrstvu nahoru v pořadí |
| `VW_LAYER_DOWN` | Posune vrstvu dolů v pořadí |
| `VW_LAYER_SYNC` | Synchronizuje draw-order s pořadím vrstev |
| `VW_LAYER_LIST` | Vypíše seznam vrstev |

### Třídy
| Příkaz | Popis |
|--------|-------|
| `VW_CLASS_NEW` | Vytvoří novou třídu s definicí barvy, čáry, tloušťky |
| `VW_CLASS_ASSIGN` | Přiřadí třídu vybraným objektům |
| `VW_CLASS_UPDATE` | Aktualizuje vlastnosti třídy |
| `VW_CLASS_LIST` | Vypíše seznam tříd |

## Sestavení

### Požadavky
- .NET 8.0 SDK
- BricsCAD V25 (nebo novější)

### Build
```bash
# Nastavit cestu k BricsCAD (Windows)
set BricsCADPath=C:\Program Files\Bricsys\BricsCAD V25

# Sestavit
dotnet build
```

### Načtení do BricsCAD
1. Sestavte plugin (`dotnet build`)
2. V BricsCAD zadejte příkaz `NETLOAD`
3. Vyberte `BricsLayerPlugin.dll` z výstupní složky
4. Plugin se automaticky inicializuje a vypíše dostupné příkazy

## Struktura projektu

```
src/
├── Models/
│   ├── VwLayer.cs          # Datový model vrstvy
│   ├── VwLayerState.cs     # Enum stavů vrstvy
│   └── VwClass.cs          # Datový model třídy
├── Managers/
│   ├── LayerManager.cs     # Správa vrstev, pořadí, draw-order
│   └── ClassManager.cs     # Správa tříd, XData, vizuální vlastnosti
├── Commands/
│   ├── LayerCommands.cs    # BricsCAD příkazy pro vrstvy
│   └── ClassCommands.cs    # BricsCAD příkazy pro třídy
├── UI/
│   ├── LayerPaletteHost.cs # BricsCAD PaletteSet host
│   ├── LayerPaletteControl.xaml/.cs  # WPF paleta vrstev
│   └── InputDialog.xaml/.cs          # Vstupní dialog
├── Utils/
│   └── LayerSerializer.cs  # JSON persistence konfigurace
└── PluginApp.cs            # Vstupní bod pluginu
```

## Jak to funguje

### Draw-order synchronizace
Plugin využívá BricsCAD `DrawOrderTable` pro řazení objektů. Při změně pořadí vrstev se všechny objekty na vrstvách přeřadí v draw-order tabulce tak, aby odpovídaly novému pořadí.

### Grayed stav
Stav "Grayed" je implementován nastavením barvy vrstvy na šedou (ACI 8). Objekty zůstávají viditelné, ale jsou vizuálně odlišené.

### Třídy přes XData
Přiřazení třídy k objektu je uloženo jako XData (Extended Data) s názvem aplikace `VWCLASS`. Při změně vlastností třídy se automaticky aktualizují všechny přiřazené objekty.
