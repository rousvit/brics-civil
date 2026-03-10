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
| `VW_CLASSES` | Otevře paletu na záložce Třídy |
| `VW_CLASS_NEW` | Vytvoří novou třídu s definicí barvy, čáry, tloušťky |
| `VW_CLASS_ASSIGN` | Přiřadí třídu vybraným objektům |
| `VW_CLASS_UPDATE` | Aktualizuje vlastnosti třídy |
| `VW_CLASS_LIST` | Vypíše seznam tříd |

## Plovoucí / dokovatelné okno

Plugin používá `PaletteSet` – stejnou technologii jako nativní BricsCAD panely (Vlastnosti, Vrstvy, Průzkumník):

- **Plovoucí okno** – volně přesouvatelné po obrazovce, změna velikosti
- **Dokování** – přetažením k levému/pravému okraji se okno připne
- **Auto-hide** – tlačítko špendlíku pro automatické skrývání
- **Záložky** – panel obsahuje dvě záložky: *Vrstvy* a *Třídy*
- **Persistence** – BricsCAD si pamatuje pozici a velikost okna mezi relacemi

Otevření: příkaz `VW_LAYERS` nebo `VW_CLASSES`.

---

## Podrobný návod k nasazení (krok za krokem)

### Krok 1: Požadavky

| Požadavek | Verze | Odkaz |
|-----------|-------|-------|
| .NET 8.0 SDK | 8.0+ | https://dotnet.microsoft.com/download |
| BricsCAD | V25+ (Pro/Platinum) | https://www.bricsys.com |
| Visual Studio nebo VS Code | volitelné | pro editaci kódu |

> **Důležité:** BricsCAD Lite nepodporuje .NET API. Potřebujete edici **Pro** nebo **Platinum**.

### Krok 2: Stažení zdrojového kódu

```bash
git clone <URL-repozitáře> brics-civil
cd brics-civil
```

### Krok 3: Nastavení cesty k BricsCAD

Plugin potřebuje reference na BricsCAD DLL soubory. Cestu nastavíte jedním z těchto způsobů:

**Varianta A – Proměnná prostředí (doporučeno):**
```cmd
set BricsCADPath=C:\Program Files\Bricsys\BricsCAD V25
```

**Varianta B – Soubor `Directory.Build.props`:**
Vytvořte soubor `Directory.Build.props` vedle `.csproj`:
```xml
<Project>
  <PropertyGroup>
    <BricsCADPath>C:\Program Files\Bricsys\BricsCAD V25</BricsCADPath>
  </PropertyGroup>
</Project>
```

**Varianta C – Parametr při buildu:**
```cmd
dotnet build /p:BricsCADPath="C:\Program Files\Bricsys\BricsCAD V25"
```

### Krok 4: Sestavení pluginu

```cmd
REM Debug build (pro vývoj)
dotnet build

REM Release build (pro nasazení)
dotnet build -c Release
```

Výstupní DLL se vytvoří v:
```
bin\Release\net8.0-windows\BricsLayerPlugin.dll
```

### Krok 5: Ruční načtení do BricsCAD (jednorázově)

1. Spusťte **BricsCAD**
2. Do příkazové řádky zadejte: `NETLOAD`
3. V dialogu vyberte soubor `BricsLayerPlugin.dll`
4. V příkazové řádce se zobrazí:
   ```
   === BricsLayerPlugin (Vectorworks styl) načten ===
   ```
5. Zadejte `VW_LAYERS` pro otevření plovoucího okna

### Krok 6: Automatické načítání při startu BricsCAD

Aby se plugin načítal automaticky při každém spuštění BricsCAD:

**Varianta A – Instalační skript (nejjednodušší):**

```cmd
REM Spusťte jako správce
install\install.bat
```

Skript:
1. Sestaví plugin v Release režimu
2. Zkopíruje DLL do `C:\BricsPlugins\`
3. Zaregistruje plugin do Windows registru pro auto-load

**Varianta B – Ruční registrace:**

1. Zkopírujte `BricsLayerPlugin.dll` do trvalé složky, např. `C:\BricsPlugins\`
2. Dvakrát klikněte na `install\autoload.reg` (upravte cestu uvnitř)
3. Potvrďte import do registru

**Varianta C – Přes BricsCAD APPLOAD:**

1. V BricsCAD zadejte příkaz `APPLOAD`
2. Klikněte na **Startup Suite** (pravý dolní roh)
3. Přidejte `BricsLayerPlugin.dll`
4. Plugin se bude načítat automaticky

### Krok 7: Ověření instalace

Po (re)startu BricsCAD:

```
Příkazová řádka: VW_LAYER_LIST
```

Mělo by se zobrazit:
```
--- Vrstvy (Vectorworks styl) ---
  0: 0 [On]
--- Celkem: 1 ---
```

### Krok 8: Odinstalace

```cmd
install\uninstall.bat
```

Nebo ručně:
1. Odstraňte registrový klíč: `HKCU\Software\Bricsys\BricsCAD\V25\en_US\Applications\BricsLayerPlugin`
2. Smažte soubory z `C:\BricsPlugins\`

---

## Řešení problémů

| Problém | Řešení |
|---------|--------|
| `NETLOAD` hlásí chybu | Ověřte, že máte BricsCAD Pro/Platinum (ne Lite) |
| "Could not load file or assembly" | Zkontrolujte, že DLL odpovídá verzi .NET v BricsCAD |
| Plugin se nenačte automaticky | Ověřte registrový klíč a cestu k DLL |
| Chyba "BrxMgd.dll not found" při buildu | Nastavte správnou `BricsCADPath` |
| Okno se nezobrazí po `VW_LAYERS` | BricsCAD musí běžet v režimu s GUI (ne command-line) |

---

## Struktura projektu

```
brics-civil/
├── BricsLayerPlugin.csproj     # Projektový soubor
├── src/
│   ├── Models/
│   │   ├── VwLayer.cs          # Datový model vrstvy
│   │   ├── VwLayerState.cs     # Enum stavů vrstvy (On/Off/Grayed)
│   │   └── VwClass.cs          # Datový model třídy
│   ├── Managers/
│   │   ├── LayerManager.cs     # Správa vrstev, pořadí, draw-order
│   │   └── ClassManager.cs     # Správa tříd, XData, vizuální vlastnosti
│   ├── Commands/
│   │   ├── LayerCommands.cs    # BricsCAD příkazy pro vrstvy
│   │   └── ClassCommands.cs    # BricsCAD příkazy pro třídy
│   ├── UI/
│   │   ├── LayerPaletteHost.cs           # PaletteSet – plovoucí/dokovatelné okno
│   │   ├── LayerPaletteControl.xaml/.cs  # WPF záložka Vrstvy
│   │   ├── ClassPaletteControl.xaml/.cs  # WPF záložka Třídy
│   │   └── InputDialog.xaml/.cs          # Vstupní dialog
│   ├── Utils/
│   │   └── LayerSerializer.cs  # JSON persistence konfigurace
│   └── PluginApp.cs            # Vstupní bod pluginu (IExtensionApplication)
└── install/
    ├── install.bat             # Instalační skript
    ├── uninstall.bat           # Odinstalační skript
    └── autoload.reg            # Registrový soubor pro auto-load
```

## Jak to funguje

### Draw-order synchronizace
Plugin využívá BricsCAD `DrawOrderTable` pro řazení objektů. Při změně pořadí vrstev se všechny objekty na vrstvách přeřadí v draw-order tabulce tak, aby odpovídaly novému pořadí.

### Grayed stav
Stav "Grayed" je implementován nastavením barvy vrstvy na šedou (ACI 8). Objekty zůstávají viditelné, ale jsou vizuálně odlišené.

### Třídy přes XData
Přiřazení třídy k objektu je uloženo jako XData (Extended Data) s názvem aplikace `VWCLASS`. Při změně vlastností třídy se automaticky aktualizují všechny přiřazené objekty.

### Plovoucí okno (PaletteSet)
Plugin používá `Bricscad.Windows.PaletteSet` – stejné API jako nativní BricsCAD panely. Díky unikátnímu GUID si BricsCAD automaticky pamatuje pozici, velikost a stav dokování mezi relacemi. Okno obsahuje dvě WPF záložky hostované přes `AddVisual()`.
