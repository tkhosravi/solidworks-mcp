# SolidWorks MCP Server

Serveur [Model Context Protocol](https://modelcontextprotocol.io) pour **SolidWorks 2025 SP5.0**, écrit en **C# / .NET 8**.
Il se connecte à une instance SolidWorks en cours d'exécution via **COM** et expose plus de 40 outils : documents, fonctions, esquisses, géométrie 3D, configurations, propriétés personnalisées, assemblages, mises en plan, cotes, équations, matériaux et exports.

```
Claude Desktop ⇄ (stdio/MCP) ⇄ SolidWorksMCP.exe ⇄ (COM) ⇄ SolidWorks 2025
```

---

## Sommaire

1. [Prérequis](#prérequis)
2. [Installation](#installation)
3. [Configuration dans Claude Desktop](#configuration-dans-claude-desktop)
4. [Variables d'environnement](#variables-denvironnement)
5. [Exemples de prompts](#exemples-de-prompts)
6. [Liste des outils](#liste-des-outils)
7. [Architecture](#architecture)
8. [Tests unitaires](#tests-unitaires)
9. [Dépannage](#dépannage)

---

## Prérequis

| Prérequis | Détail |
|---|---|
| OS | Windows 10/11 (COM est exclusif à Windows) |
| .NET | **[SDK .NET 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) ou plus récent — à installer sur la machine** (voir note ci-dessous) |
| SolidWorks | 2025 SP5.0 — **doit être lancé avant le serveur** |
| Claude Desktop | [claude.ai/download](https://claude.ai/download) |

> **Le SDK .NET est indispensable pour compiler le serveur.** Téléchargez-le sur [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) (édition « SDK », pas seulement « Runtime ») et installez-le avant de lancer `dotnet build`. Vérifiez l'installation avec `dotnet --version` (vous devez obtenir `8.x` ou plus).
>
> *Faut-il aussi .NET sur la machine qui exécute le serveur ?* Cela dépend du mode de publication :
> - **Publication framework-dependent** (par défaut) : le `.exe` a besoin du **runtime .NET 8** présent sur la machine. Comme le SDK contient le runtime, si vous compilez et exécutez sur la même machine, vous n'avez rien de plus à installer.
> - **Publication self-contained** : le runtime est embarqué dans le `.exe` — la machine cible n'a alors besoin **d'aucune installation .NET** (voir l'étape 3 de l'[installation](#3-publier-un-exécutable-autonome)).

> Par défaut, le serveur s'attache à l'instance SolidWorks **déjà ouverte** (via la Running Object Table). Il peut aussi démarrer SolidWorks automatiquement — voir [Variables d'environnement](#variables-denvironnement).

## Installation

### 1. Cloner et compiler

```powershell
git clone <url-du-repo> C:\solidworks-mcp
cd C:\solidworks-mcp
dotnet build
```

### 2. Lancer les tests (optionnel mais recommandé)

```powershell
dotnet test
```

### 3. Publier un exécutable autonome

**Option A — framework-dependent** (`.exe` léger, nécessite le runtime .NET 8 sur la machine) :

```powershell
dotnet publish src\SolidWorksMCP -c Release -o C:\solidworks-mcp\publish
```

**Option B — self-contained** (`.exe` autonome, **aucune installation .NET requise** pour l'exécuter — idéal pour déployer sur une machine sans SDK) :

```powershell
dotnet publish src\SolidWorksMCP -c Release -r win-x64 --self-contained true -o C:\solidworks-mcp\publish
```

Dans les deux cas, vous obtenez `C:\solidworks-mcp\publish\SolidWorksMCP.exe`.

### 4. Vérification rapide

Lancez SolidWorks, puis dans un terminal :

```powershell
C:\solidworks-mcp\publish\SolidWorksMCP.exe
```

Le serveur attend des messages MCP sur stdin — si aucune erreur ne s'affiche sur stderr, il est prêt. `Ctrl+C` pour quitter.

## Configuration dans Claude Desktop

1. Ouvrez le fichier de configuration :
   - Windows : `%APPDATA%\Claude\claude_desktop_config.json`
   - (ou via Claude Desktop : **Settings → Developer → Edit Config**)

2. Ajoutez le serveur :

```json
{
  "mcpServers": {
    "solidworks": {
      "command": "C:\\solidworks-mcp\\publish\\SolidWorksMCP.exe",
      "args": [],
      "env": {
        "SOLIDWORKS_AUTO_START": "true"
      }
    }
  }
}
```

Le bloc `env` est optionnel — voir [Variables d'environnement](#variables-denvironnement) pour toutes les options.

> Alternative sans publication (compilation à la volée, démarrage plus lent) :
> ```json
> {
>   "mcpServers": {
>     "solidworks": {
>       "command": "dotnet",
>       "args": ["run", "--project", "C:\\solidworks-mcp\\src\\SolidWorksMCP"]
>     }
>   }
> }
> ```

3. Redémarrez Claude Desktop. L'icône 🔨 doit lister les outils `solidworks`.

4. **Lancez SolidWorks** (sauf si `SOLIDWORKS_AUTO_START=true`), puis testez dans Claude : *« Vérifie la connexion à SolidWorks »*.

## Variables d'environnement

Toute la configuration passe par des variables d'environnement — aucun fichier du projet à modifier, ni avant ni après compilation. Définissez-les dans le bloc `env` de `claude_desktop_config.json` (recommandé) ou au niveau système.

| Variable | Défaut | Description |
|---|---|---|
| `SOLIDWORKS_VERSION` | `2025` | Année de version SolidWorks ciblée. Pilote le ProgID COM (`2025` → `SldWorks.Application.33`, `2024` → `.32`…). Un fallback générique est toujours tenté ensuite. |
| `SOLIDWORKS_EXE_PATH` | `C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\SLDWORKS.exe` | Chemin complet de `SLDWORKS.exe`. Utilisé uniquement pour le démarrage automatique. |
| `SOLIDWORKS_AUTO_START` | `false` | `true` : si SolidWorks n'est pas ouvert, le serveur le lance et attend qu'il soit prêt. `false` : le serveur exige que SolidWorks soit déjà ouvert. |
| `SOLIDWORKS_START_TIMEOUT` | `90` | Temps d'attente maximal (secondes, ≥ 5) après le lancement automatique. |

Exemple complet (installation SolidWorks sur un autre disque, démarrage auto) :

```json
{
  "mcpServers": {
    "solidworks": {
      "command": "C:\\solidworks-mcp\\publish\\SolidWorksMCP.exe",
      "args": [],
      "env": {
        "SOLIDWORKS_VERSION": "2025",
        "SOLIDWORKS_EXE_PATH": "D:\\CAO\\SOLIDWORKS Corp\\SOLIDWORKS\\SLDWORKS.exe",
        "SOLIDWORKS_AUTO_START": "true",
        "SOLIDWORKS_START_TIMEOUT": "180"
      }
    }
  }
}
```

> Une valeur invalide (année impossible, chemin ne pointant pas vers un `.exe`…) fait échouer le démarrage du serveur **avec un message explicite** dans les logs MCP (`%APPDATA%\Claude\logs\`), plutôt que d'échouer silencieusement au premier appel d'outil.

## Exemples de prompts

Avec SolidWorks ouvert et une pièce active, essayez dans Claude Desktop :

**Découverte / lecture :**
> Vérifie la connexion à SolidWorks et liste les documents ouverts.

> Montre-moi l'arbre de création de la pièce active et ses propriétés de masse.

> Quelles sont les configurations de ce modèle ? Liste aussi les équations et variables globales.

**Modélisation de zéro :**
> Crée une nouvelle pièce. Dessine une esquisse sur le plan de face avec un rectangle de 80×40 mm centré sur l'origine, puis extrude-le de 15 mm.

> Dans la pièce active, ouvre une esquisse sur la face de dessus, dessine un cercle de rayon 5 mm au centre et fais un enlèvement de matière débouchant.

**Modification paramétrique :**
> Change la cote D1@Esquisse1 à 25 mm et reconstruis le modèle.

> Ajoute une variable globale "Epaisseur" = 3mm et lie la cote D1@Boss-Extrude1 à cette variable.

> Supprime (suppress) la fonction Fillet1, reconstruis, et donne-moi la nouvelle masse.

**Données produit :**
> Affecte le matériau "AISI 304" à cette pièce, puis remplis les propriétés personnalisées : Description = "Support moteur", Auteur = "Tony".

**Assemblages :**
> Liste les composants de l'assemblage actif avec leur état (résolu/supprimé), puis les contraintes.

**Export :**
> Exporte la pièce active en STEP et en STL dans D:\exports, et fais une capture d'écran en vue isométrique dans D:\exports\apercu.png.

**Workflow complet :**
> Ouvre C:\CAO\support.SLDPRT, passe en configuration "Variante-B", mets la cote D2@Esquisse3 à 12 mm, reconstruis, exporte en PDF et en Parasolid, puis sauvegarde et ferme le document.

## Liste des outils

### Connexion & système
| Outil | Description |
|---|---|
| `CheckConnection` | Vérifie le lien COM et la version de SolidWorks |
| `RunMacro` | Exécute une macro VBA (.swp) — échappatoire pour le reste de l'API |

### Documents
| Outil | Description |
|---|---|
| `ListOpenDocuments` | Documents ouverts (titre, chemin, type, modifié) |
| `GetActiveDocument` | Détails du document actif |
| `OpenDocument` | Ouvre un .sldprt / .sldasm / .slddrw |
| `NewDocument` | Nouvelle pièce / assemblage / mise en plan (modèles par défaut) |
| `SaveActiveDocument` | Enregistre le document actif |
| `CloseDocument` | Ferme un document par titre |
| `ActivateDocument` | Met un document ouvert au premier plan |

### Fonctions (features)
| Outil | Description |
|---|---|
| `ListFeatures` | Arbre de création complet |
| `SetFeatureSuppression` | Supprime / rétablit une fonction |
| `RenameFeature` | Renomme une fonction |
| `DeleteFeature` | Supprime définitivement une fonction |
| `GetMassProperties` | Masse, volume, surface, centre de gravité |

### Esquisses (coordonnées en mm)
| Outil | Description |
|---|---|
| `InsertSketchOnPlane` | Ouvre une esquisse sur un plan / face plane |
| `ExitSketch` | Valide et quitte l'esquisse |
| `SketchLine` / `SketchCircle` / `SketchRectangle` / `SketchArc` | Géométrie 2D |

### Géométrie 3D (dimensions en mm / degrés)
| Outil | Description |
|---|---|
| `CreateExtrusion` | Bossage extrudé (borgne, avec dépouille optionnelle) |
| `CreateCutExtrusion` | Enlèvement de matière (borgne ou débouchant) |
| `CreateRevolve` | Révolution autour d'un axe / ligne de construction |
| `CreateFillet` | Congé sur les arêtes sélectionnées |
| `CreateChamfer` | Chanfrein sur les arêtes sélectionnées |

### Cotes & équations
| Outil | Description |
|---|---|
| `GetDimension` / `SetDimension` | Lecture / écriture d'une cote (`D1@Esquisse1`) |
| `ListEquations` / `AddEquation` / `SetEquation` / `DeleteEquation` | Équations et variables globales |

### Configurations
| Outil | Description |
|---|---|
| `ListConfigurations` | Liste avec configuration active |
| `ActivateConfiguration` / `CreateConfiguration` / `DeleteConfiguration` | Gestion des configurations |

### Propriétés personnalisées
| Outil | Description |
|---|---|
| `ListCustomProperties` | Propriétés du fichier ou d'une configuration |
| `SetCustomProperty` / `DeleteCustomProperty` | Écriture / suppression |

### Assemblages
| Outil | Description |
|---|---|
| `ListComponents` | Composants (état, configuration, chemin) |
| `SetComponentSuppression` | Supprime / résout un composant |
| `InsertComponent` | Insère une pièce/assemblage à des coordonnées données |
| `ListMates` | Liste des contraintes |

### Mises en plan
| Outil | Description |
|---|---|
| `ListSheets` | Feuilles et vues |
| `ActivateSheet` | Active une feuille |
| `CreateStandardViews` | Vues standard d'un modèle sur la feuille active |

### Matériaux, vues & export
| Outil | Description |
|---|---|
| `GetMaterial` / `SetMaterial` | Matériau de la pièce |
| `ZoomToFit` / `SetStandardView` / `Rebuild` | Affichage et reconstruction |
| `ExportActiveDocument` | step, iges, stl, 3mf, x_t, sat, obj, vrml, pdf, dxf, dwg, png, jpg, tif, edrawings |
| `ListExportFormats` | Formats supportés |
| `CaptureScreenshot` | Capture PNG de la vue courante |

## Architecture

```
solidworks-mcp/
├── SolidWorksMCP.slnx
├── src/
│   ├── SolidWorksMCP.Core/            ← logique pure, sans COM (testable partout)
│   │   ├── SwConstants.cs             ← sous-ensemble des enums swconst
│   │   ├── ServerConfig.cs            ← lecture des variables d'environnement
│   │   ├── ProgIdHelper.cs            ← ProgID versionné (2025 → .33)
│   │   ├── ExportFormatHelper.cs      ← validation format/type de document
│   │   ├── UnitsHelper.cs             ← mm ↔ m, degrés ↔ radians
│   │   ├── DimensionNameHelper.cs     ← normalisation "D1@Esquisse1"
│   │   └── Models/Records.cs          ← records JSON renvoyés aux outils
│   └── SolidWorksMCP/                 ← serveur MCP (net8.0-windows)
│       ├── Program.cs                 ← hôte + transport stdio
│       ├── ToolRunner.cs              ← gestion d'erreurs COM uniforme
│       ├── Services/SolidWorksConnectionService.cs  ← attache COM (ROT)
│       └── Tools/                     ← 12 classes d'outils
└── tests/
    └── SolidWorksMCP.Core.Tests/      ← 59 tests xUnit
```

Choix de conception :

- **COM late-binding (`dynamic`)** : le serveur compile sans les assemblies d'interop SolidWorks. Pour du typage fort, référencez `SolidWorks.Interop.sldworks.dll` depuis `C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist\`.
- **`Marshal.GetActiveObject` n'existe plus en .NET 8** : la connexion passe par un P/Invoke direct de `CLSIDFromProgID` (ole32) + `GetActiveObject` (oleaut32).
- **Unités à la frontière** : l'API SolidWorks travaille en mètres/radians ; tous les outils acceptent des mm et des degrés et convertissent via `UnitsHelper`.
- **stdout réservé au protocole MCP** : tous les logs partent sur stderr.

## Tests unitaires

La logique métier (validation des formats d'export, conversion d'unités, ProgID, parsing des noms de cotes, configuration par variables d'environnement, sérialisation des modèles) vit dans `SolidWorksMCP.Core` — sans dépendance COM — et est couverte par xUnit.

**Les tests tournent sur n'importe quel OS, y compris macOS et Linux** (seule l'exécution du serveur contre SolidWorks exige Windows) :

```bash
dotnet test
```

Un workflow GitHub Actions ([.github/workflows/ci.yml](.github/workflows/ci.yml)) compile la solution et exécute les tests à chaque push / pull request.

Les appels COM eux-mêmes ne sont pas testables sans SolidWorks ; pour un test d'intégration manuel, utilisez les prompts de la section [Exemples](#exemples-de-prompts) avec une pièce de test.

## Dépannage

| Symptôme | Cause / solution |
|---|---|
| `SolidWorks is not running` | Lancez SolidWorks avant d'utiliser un outil, ou activez `SOLIDWORKS_AUTO_START=true` pour que le serveur le démarre lui-même. |
| Les outils n'apparaissent pas dans Claude Desktop | Vérifiez le chemin dans `claude_desktop_config.json` (antislashs doublés `\\`), puis redémarrez complètement Claude Desktop. Les logs MCP sont dans `%APPDATA%\Claude\logs\`. |
| Erreur COM `0x800401E3` (MK_E_UNAVAILABLE) | SolidWorks n'est pas enregistré dans la ROT — démarrez SolidWorks normalement (pas en tant qu'administrateur si Claude ne l'est pas, et vice-versa : **les deux processus doivent avoir le même niveau d'élévation**). |
| `RPC_E_CALL_REJECTED` (0x80010001) | SolidWorks est occupé (reconstruction, boîte de dialogue ouverte). Fermez les dialogues et réessayez. |
| Export DXF échoue sur une pièce | Le DXF direct ne marche que sur les pièces de tôlerie ; passez par une mise en plan sinon. |
| Noms de fonctions en français | Les noms d'entités suivent la langue de l'interface SW : `Plan de face` au lieu de `Front Plane`, `Esquisse1` au lieu de `Sketch1`. Utilisez `ListFeatures` pour voir les vrais noms. |
