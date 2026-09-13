# Asset loading — design (ClassicUO)

## Come funziona in ClassicUO (riferimento per la fattibilità)

**Architettura generale:** ogni tipo di asset ha un loader dedicato in `src/ClassicUO.Assets/` (es. `ArtLoader.cs`, `GumpsLoader.cs`, `SoundsLoader.cs`, `ClilocLoader.cs`, `SkillsLoader.cs`, `HuesLoader.cs`, ecc.), tutti derivati da `UOFileLoader`. Ognuno apre gli archivi client ufficiali (`.uop`/`.mul` + `.idx`, via `UOFileManager.GetUOFilePath`) in `Load()`.

**Meccanismo "asset propri dello shard" — già presente, molto rilevante per la fattibilità.** Questa versione del client (commit recenti, settembre 2026) aggiunge per quattro tipi di asset la possibilità di caricare file "propri" da cartelle accanto al client, letti **prima** dell'archivio e con precedenza su di esso, senza mai toccare i file `.uop`/`.mul` originali:

| Asset | Cartella (sotto `FileManager.BasePath`) | Formato file | Come si aggancia |
|---|---|---|---|
| Art statiche (item) | `Art/Statics/<id>.art` | Stesso formato run-length dell'archivio (header con size + righe compresse) | `ArtLoader.cs:82-83`, metodo `Gather`/`LoadOurs()` (righe 64-109) |
| Art terreno (land tile) | `Art/Land/<id>.art` | 1.012 pixel raw di un diamante | `ArtLoader.cs:85-86` — può solo **sostituire** land esistente, non aggiungerne (tutti i 16.384 ID land sono già occupati) |
| Gump art (icone UI, bottoni) | `Gumps/<id>.gump` | Stesso formato run-length compresso dell'archivio | `GumpsLoader.cs:130-138` (`LoadOurs()`) |
| Suoni | `Sounds/<id>.wav` | WAV puro (verificato: deve essere 22.050 Hz, mono, 16-bit — altrimenti rifiutato con log, niente fallback silenzioso su formato sbagliato) | `SoundsLoader.cs:214` |
| Stringhe (cliloc) | `Clilocs.txt` (file, non cartella) nella cartella del client | Testo semplice: `<numero><tab o spazi><testo>`, righe che iniziano per `#` sono commenti | `ClilocLoader.cs:90-119` (`ReadOurs()`) |

**Come si aggiunge un asset nuovo in pratica (nessuna modifica al codice del client richiesta per il caso comune):**
1. Scegli un ID libero nel range giusto (vedi tabella range sotto).
2. Metti il file nella cartella giusta con quel ID come nome file.
3. Il client lo indicizza una volta sola al boot (`Directory.EnumerateFiles`, non un check ad ogni frame) e lo usa al posto/in aggiunta all'archivio.
4. Un file rotto/malformato produce un warning nel log e ripiega sull'archivio invece di rompere il rendering (`ArtLoader`/`GumpsLoader`) — eccetto i suoni, dove un WAV non conforme è semplicemente rifiutato.

**Range di ID liberi (il punto più importante per la fattibilità):**
- **Item art (statics):** l'archivio usa 39.516 dei 81.920 ID possibili, il più alto è 62.763 → **~19.000 ID liberi sopra il massimo usato**, nessun conflitto.
- **Land art:** **zero ID liberi** — tutti i 16.384 esistono già; un file custom può solo *sostituire* una texture di terreno esistente, mai aggiungerne una nuova.
- **Gump art:** `gump.def` (il meccanismo stock) può solo aggiungere alias, mai sovrascrivere un ID già occupato; il meccanismo a cartella invece **può sia aggiungere sia sovrascrivere**.
- **Suoni:** l'archivio si ferma a ID 1.666, il client supporta fino a 0xFFFF (65.535) → **~63.000 ID liberi**.
- **Stringhe (cliloc):** il client ufficiale si ferma a 3.011.032 → **tutto sopra 3.100.000 è libero** (i cliloc esistenti si possono anche sovraccrivere, dato che il dizionario è "last-write-wins": `Clilocs.txt` viene letto dopo `Cliloc.enu` e dopo il file di lingua).

**Cosa NON è ancora coperto da questo meccanismo:** non risultano equivalenti per texture (`TexmapsLoader`), animazioni dei mobile (`AnimationsLoader`), font, o mappe — solo art (item/land), gump, suoni e stringhe. Se serve aggiungere una nuova texture o una nuova animazione, andrebbe verificato/implementato ex-novo (probabile modifica a `Projects/Server` di questo repo, da loggare in `custom-docs/CUSTOM_CHANGES.md`).

**Insidie note:**
- `FileManager.BasePath` è la cartella di installazione del client configurata dall'utente — le cartelle `Art/`, `Gumps/`, `Sounds/` e il file `Clilocs.txt` vanno distribuiti insieme al client custom (o scritti dall'installer/launcher), non sono assets del server ModernUO.
- L'indicizzazione avviene una sola volta al boot: aggiungere/modificare un file richiede un riavvio del client per essere visto (nessun hot-reload).
- Per land art, servono comunque ID esistenti da sostituire (retexture), non è possibile "inventare" terreno nuovo con questo meccanismo.

## Decisioni custom

Nessuna decisione ancora presa.
