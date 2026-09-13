# Effetti UI — design (ClassicUO)

## Come funziona in ClassicUO (riferimento per la fattibilità)

Due sistemi separati, nessuna dipendenza reciproca.

### 1. Buff bar (icone di stato)

- `Game/UI/Gumps/BuffGump.cs` — la gump che mostra le icone; renderizza da `World.Player.BuffIcons` (dizionario `BuffIconType → BuffIcon` in `Game/GameObjects/PlayerMobile.cs:18,36`).
- `Network/PacketHandlers.cs:5482` (`BuffDebuff`) — handler del packet buff/debuff (0x03E9+ o 0x0466+, due range storici). Legge l'ID buff dal server, lo mappa a `iconID` (offset numerico) e chiama `world.Player.AddBuff(ic, BuffTable.Table[iconID], timer, text)`.
- `Game/Data/BuffTable.cs` — **il punto chiave**: `BuffIconType` è un enum client-side (solo per leggibilità/tooltip: il cast `(BuffIconType)p.ReadUInt16BE()` in C# funziona anche per valori senza nome nell'enum, quindi un ID sconosciuto non causa errori). `BuffTable.Table[iconID]` è l'array che mappa l'ID buff numerico alla **grafica gump** da mostrare come icona.
- **`BuffTable.Load()` (riga 206) legge `Data/Client/buff.txt` se esiste**, prima di usare l'array hardcoded `_defaultTable` come fallback. Il file è un elenco di ID grafici in ordine di indice, con supporto commenti (`#`/`;`).

**Fattibilità:** aggiungere/estendere le icone di buff **non richiede toccare il codice sorgente né ricompilare** — basta editare `Data/Client/buff.txt` per aggiungere una riga (l'ID grafico gump) alla posizione corrispondente al nuovo `iconID` del server. L'unico vincolo reale è che la grafica gump referenziata debba esistere come asset renderizzabile (vedi sotto per come aggiungerne una nuova senza patchare gli archivi originali).

### 2. Effetti visivi/particellari (GraphicEffect)

- `Network/PacketHandlers.cs:2435` (`GraphicEffect`), packet 0x70/0xC0/0xC7 — legge dal server un `graphic` (ushort, **ID di art/animazione grezzo, non un enum**), hue, blend mode, sorgente/target, velocità/durata, e chiama `world.SpawnEffect(...)`.
- Nessuna tabella di mappatura lato client: il server decide liberamente quale ID grafico mostrare (esattamente come fa `Server.Effects` in ModernUO, che passa un `itemID` diretto).

**Fattibilità:** un effetto visivo con un ID di art **già esistente** funziona già oggi, gratis. Un effetto con un ID **mai esistito** richiede che quell'asset grafico/animazione esista nel client — vedi sotto.

### 3. Asset "propri" dello shard, senza patchare gli archivi originali (scoperta rilevante, commit molto recenti nell'upstream, 2026-09-05/06)

Il client supporta ora il caricamento di asset custom da **file sciolti in cartelle**, con fallback all'archivio originale (MUL/UOP) se il file non esiste — nessuna necessità di ricompilare né di riscrivere gli archivi originali (che sarebbero enormi da ridistribuire per un solo asset):

| Tipo asset | Percorso file sciolto | Formato |
|---|---|---|
| Gump art (icone, bottoni, sfondi gump) | `<client>/Gumps/<id>.gump` | stesso formato run-length dell'archivio, larghezza/altezza in testa |
| Item art (statics) | `<client>/Art/Statics/<itemId>.art` | stesso formato run-length dell'archivio |
| Terreno (land art) | `<client>/Art/Land/<landId>.art` | 1012 pixel grezzi (diamante) |
| Suoni | file sciolti (vedi commit `dbc401cc5`) | — |
| Stringhe (nomi, context menu, cliloc custom) | file di testo semplice (vedi commit `4d41c8652`) | testo semplice, aggira il limite dei cliloc fissi del client ufficiale |
| Icone buff | `Data/Client/buff.txt` | elenco ID grafici, uno per riga |

Questo conferma in modo diretto la nota generale sul "client custom" in `custom-docs/design/README.md` del repo ModernUO: quasi ogni limite "il client ufficiale non lo supporta" diventa **lavoro pianificabile** (aggiungere un file asset) invece di un blocco architetturale — e in più, in questo caso specifico, **senza nemmeno bisogno di ricompilare il client**, solo di distribuire i file sciolti insieme allo shard.

**Insidie note:**
- Questi meccanismi sono molto recenti nell'upstream ClassicUO (settimane, non anni) — verificare ad ogni sync upstream che continuino a esistere/funzionare come documentato qui.
- Il fallback silenzioso ("se il file manca o è malformato, torna all'archivio originale con solo un warning") significa che un errore di formattazione in un asset custom non blocca l'avvio, ma può risultare in un asset mancante/sbagliato senza errore evidente — utile saperlo in fase di debug.
- Per `BuffTable`, un `iconID` calcolato che superi la lunghezza dell'array (`iconID < BuffTable.Table.Length`) viene **ignorato silenziosamente** (l'`if` a riga 5500 salta tutto il blocco) — quindi il file `buff.txt` deve avere abbastanza righe da coprire l'indice del nuovo buff, non solo "una riga in più a caso".

## Verifica esterna (2026-09-13)

**Nessuna traccia esterna trovata specificamente per `Data/Client/buff.txt`** — probabile feature troppo recente/di nicchia per comparire in ricerche pubbliche indicizzate (stesso caso di `FullIndexSetModifySpell` in `spells-ui.md`). Confermato però indirettamente il concetto di base: un thread ServUO su come identificare le buff icon conferma che "il numero di BuffIcon viene tradotto dal client in un'immagine gump, il riferimento è nel client, non nel server" — coerente con l'architettura già documentata sopra (`BuffTable.Table[iconID]`). Nessuna discrepanza trovata rispetto a quanto già scritto, ma il meccanismo del file esterno resta verificato solo internamente (lettura del codice), non confermato da fonti terze.

## Decisioni custom

Nessuna decisione ancora presa.
