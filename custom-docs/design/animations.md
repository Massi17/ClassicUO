# Animazioni custom — design

## Cosa abbiamo trovato

In UO ogni mobile ha un **Body** (ID numerico) che seleziona un set di animazioni. Gli ID sono divisi per categoria in `mobtypes.txt` (convenzione community, non un limite tecnico rigido): **0-199 mostri**, **200-399 animali**, **400+ umanoidi**. Ogni Body ha fino a `MAX_ACTIONS = 80` azioni (camminare, attaccare, morire, ecc. — confermato in `src/ClassicUO.Assets/AnimationsLoader.cs:21`), ciascuna con le direzioni e i fotogrammi.

**Workflow community (ServUO/RunUO) per un'animazione custom, confermato dal thread ["How to add custom animations to UO"](https://www.servuo.dev/threads/how-to-add-custom-animations-to-uo.310/)):**
1. **`bodyconv.def`** — associa il Body ID ai file `.mul`/`.idx` di destinazione (quale file anim2/anim3/ecc. e slot).
2. **`mobtypes.txt`** — definisce il tipo di animazione (MONSTER/ANIMAL/ecc.) per quel Body ID. **Se un ID non è qui, l'animazione non appare** — è il vincolo più citato nei thread di troubleshooting.
3. **`corpse.def`** (opzionale) — collega l'animazione della carcassa/morte.
4. **`body.def`** (a volte necessario) — mappa un Body a un altro con hue diverso.
5. **Pipeline di creazione dei fotogrammi:** BMP (fotogrammi singoli, spesso da FLC/GIF) → UOAnim (converte in UOP) → Michelangelo (assembla in file VD) → Mulpatcher (scrive nell'archivio `anim.mul`/`anim.idx` finale). **UOFiddler** serve per visualizzare/testare e per importare un file `.vd` in uno slot animazione esistente (menu "Animation Edit").

**Punto chiave, diverso da quanto trovato per l'art statica:** questo intero workflow **patcha direttamente gli archivi client** (`anim.mul`/`anim.idx` o i corrispondenti `.uop`), non usa un meccanismo "file sciolto con fallback" come quello già documentato in `asset-loading.md` per item art/gump/suoni/stringhe. **Verificato nel codice ClassicUO:** `AnimationsLoader.cs` non ha alcun metodo `LoadOurs()`/cartella "propria dello shard" equivalente a `ArtLoader`/`GumpsLoader`/`SoundsLoader` — non esiste ancora un modo di "droppare un file" per un'animazione custom in questa versione del client.

## Fattibilità

| Modifica | Fattibilità | Note |
|---|---|---|
| **Riusare un Body esistente** con logica/stat/loot diversi (es. un mostro custom che usa la grafica di un Drago già presente) | **Facile, già coperto** | Puro lavoro server-side (`BaseCreature` con `Body` esistente) — nessuna modifica client, vedi pattern già noto per creature in `dev-docs/content-patterns.md` |
| **Animazione custom su un Body ID libero** (slot vuoto nel range mostri/animali/umanoidi) | **Fattibile ma pesante** — richiede la pipeline di authoring completa (BMP→UOAnim→Michelangelo→Mulpatcher) e la modifica di `bodyconv.def`/`mobtypes.txt` **dentro l'installazione del client** distribuita ai giocatori | Non è un "drop file", è una patch agli archivi — va distribuita insieme al client custom (stesso discorso già fatto per `Clilocs.txt`/cartelle asset, ma qui il file di destinazione è l'archivio binario stesso, non un file sciolto separato) |
| **Aggiungere un meccanismo "drop file" per animazioni** (equivalente a `ArtLoader.LoadOurs()`) | **Non esiste oggi** — sarebbe una modifica al codice C# di ClassicUO (`AnimationsLoader.cs`), non solo authoring di dati | Da considerare come vero e proprio task di sviluppo lato client se si vogliono animazioni custom "a file sciolto" invece di patchare gli archivi |

**In sintesi:** la strada rapida per un mostro/mobile custom è riusare grafica esistente. Un'animazione mai vista prima è fattibile ma richiede strumenti esterni di terze parti (non più mantenuti attivamente da anni: Mulpatcher/UOAnim/Michelangelo sono tool storici della community RunUO) e patch dirette agli archivi client, non il flusso "file sciolto" più comodo già trovato per l'art statica.

## Fonti

- [How to add custom animations to UO](https://www.servuo.dev/threads/how-to-add-custom-animations-to-uo.310/) — procedimento completo, raggiunto (redirect da servuo.com a servuo.dev, entrambi funzionanti)
- [Add new monster animations with UOFiddler](https://www.servuo.dev/threads/add-new-monster-animations-with-uofiddler.16183/) — non ancora aperto in dettaglio, trovato via ricerca
- [UOFiddler — Ultima Online Client Editor](https://uofiddler.polserver.com/) — sito ufficiale del tool, non ancora esplorato in dettaglio
- [Understanding Bodyconv.def](https://www.servuo.dev/threads/understanding-bodyconv-def.14592/) — non ancora aperto in dettaglio
- Verifica diretta nel codice: `C:\Users\massi\Documents\GitHub\ClassicUO\src\ClassicUO.Assets\AnimationsLoader.cs` (nessun meccanismo "file sciolto" trovato)
