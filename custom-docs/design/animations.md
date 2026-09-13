# Animazioni custom — design

## Cosa abbiamo trovato

In UO ogni mobile ha un **Body** (ID numerico) che seleziona un set di animazioni. **La categoria di un Body ID (MONSTER/ANIMAL/HUMAN/...) è sempre una keyword esplicita scritta accanto a quell'ID in `mobtypes.txt` — non esiste alcun range numerico che la determini** (verificato con controllo incrociato: ID come 1425-1428 sono dichiarati ANIMAL/MONSTER senza seguire nessun pattern di range). Ogni Body ha fino a `MAX_ACTIONS = 80` azioni (camminare, attaccare, morire, ecc. — confermato in `src/ClassicUO.Assets/AnimationsLoader.cs:21`), ciascuna con le direzioni e i fotogrammi.

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
| **Animazione custom su un Body ID libero** (slot vuoto, categoria dichiarata esplicitamente in `mobtypes.txt`) | **Fattibile ma pesante** — richiede la pipeline di authoring completa (BMP→UOAnim→Michelangelo→Mulpatcher) e la modifica di `bodyconv.def`/`mobtypes.txt` **dentro l'installazione del client** distribuita ai giocatori | Non è un "drop file", è una patch agli archivi — va distribuita insieme al client custom (stesso discorso già fatto per `Clilocs.txt`/cartelle asset, ma qui il file di destinazione è l'archivio binario stesso, non un file sciolto separato) |
| **Aggiungere un meccanismo "drop file" per animazioni** (equivalente a `ArtLoader.LoadOurs()`) | **Non esiste oggi** — sarebbe una modifica al codice C# di ClassicUO (`AnimationsLoader.cs`), non solo authoring di dati | Da considerare come vero e proprio task di sviluppo lato client se si vogliono animazioni custom "a file sciolto" invece di patchare gli archivi |

**In sintesi:** la strada rapida per un mostro/mobile custom è riusare grafica esistente. Un'animazione mai vista prima è fattibile ma richiede strumenti esterni di terze parti (non più mantenuti attivamente da anni: Mulpatcher/UOAnim/Michelangelo sono tool storici della community RunUO) e patch dirette agli archivi client, non il flusso "file sciolto" più comodo già trovato per l'art statica.

## Fonti

- [How to add custom animations to UO](https://www.servuo.dev/threads/how-to-add-custom-animations-to-uo.310/) — procedimento completo, raggiunto (redirect da servuo.com a servuo.dev, entrambi funzionanti)
- [Add new monster animations with UOFiddler](https://www.servuo.dev/threads/add-new-monster-animations-with-uofiddler.16183/) — non ancora aperto in dettaglio, trovato via ricerca
- [UOFiddler — Ultima Online Client Editor](https://uofiddler.polserver.com/) — sito ufficiale del tool, non ancora esplorato in dettaglio
- [Understanding Bodyconv.def](https://www.servuo.dev/threads/understanding-bodyconv-def.14592/) — non ancora aperto in dettaglio
- Verifica diretta nel codice: `C:\Users\massi\Documents\GitHub\ClassicUO\src\ClassicUO.Assets\AnimationsLoader.cs` (nessun meccanismo "file sciolto" trovato)

## Verifica esterna aggiuntiva (2026-09-13)

**Correzione applicata (già corretta sopra in "Cosa abbiamo trovato"):** i range numerici "0-199 mostri, 200-399 animali, 400+ umanoidi" scritti in una prima versione di questo file **non erano una regola tecnica né una convenzione community reale** — erano solo gli ID di esempio del singolo tutorial citato. Ricerca incrociata su altri thread ServUO mostra categorie assegnate a ID ben più alti e senza alcun pattern numerico (es. ID 1425/1426/1427 dichiarati `ANIMAL`, ID 1428 `MONSTER` per "JackintheBox").

**`bodyconv.def`, formato confermato con un esempio concreto** (fonte: thread dedicato "Understanding Bodyconv.def"):
```
NewBodyID  anim2.mul  anim3.mul  anim4.mul  anim5.mul
359        -1         -1         205        -1        # Ridable Drake
```
`-1` = animazione non in quel file; un numero = slot in quel file specifico. Se l'animazione resta nel file base `anim.mul`, tutti e quattro i flag sono `-1`. Nota dalla fonte: configurazione testata su client 5.0, incertezza esplicita sulla compatibilità con client 7.0.23.1+ — un altro segnale di quanto sia fragile/datato questo meccanismo sulle versioni client moderne.

**Stato reale dei tool, verificato oggi:**
- **UOFiddler:** confermato **attivamente mantenuto** — versione attuale 4.20.0, requisito .NET 10.0 (stesso target di ModernUO/ClassicUO). Ha davvero una funzione "Animation Edit" per rivedere/modificare animazioni frame-by-frame. **Ma non è un sostituto completo della pipeline storica:** il workflow trovato (Animazioni → Settings → tab Animate Frames → esporta un frame come BMP → modifichi con un editor immagini → Animation Edit → sostituisci quel frame) serve a **rimpiazzare singoli frame di uno slot animazione già esistente**, non a costruire da zero un set completo di animazioni (tutte le azioni × direzioni × frame) per un Body nuovo. Per quello resta probabilmente necessaria la pipeline più pesante. Dettaglio in più: perché uno slot compaia nella lista di UOFiddler serve aggiungerlo anche ad `animationlist.xml` (cartella profilo UOFiddler, `%APPDATA%\UOFiddler`) — un file di configurazione del tool, non del client/gioco.
- **Mulpatcher:** il sito di download citato nei thread (`varan.uodev.de`) ha risposto **HTTP 403** al fetch automatico in questa sessione — **stato non verificabile** (potrebbe essere ancora online ma bloccare richieste automatiche, o essere caduto). Un thread 2026 lo cita ancora attivamente, con un limite noto: non gestisce bene client oltre la versione 7.0.59.
- **UOAnim e Michelangelo:** **nessun sito di download indipendente trovato** — esistono solo come menzioni all'interno di vecchi tutorial che li usano in sequenza. Conferma quanto già scritto: sono strumenti storici, difficili da reperire oggi, non più mantenuti in autonomia.

**Non trovato:** una vera alternativa moderna a tutta la pipeline (BMP→UOAnim→Michelangelo→Mulpatcher) per costruire un'animazione multi-frame nuova da zero — UOFiddler copre bene la modifica/sostituzione ma non la creazione ex-novo di un intero set. Se questa strada interessa davvero in futuro, vale la pena chiedere direttamente sul Discord/forum ServUO qual è il workflow 2026 più aggiornato, perché la documentazione trovata online è in gran parte datata (thread da anni fa, incertezza sulle versioni client recenti).
