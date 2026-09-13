# Skill UI — design (ClassicUO)

## Come funziona in ClassicUO (riferimento per la fattibilità)

**File chiave:**
- `src/ClassicUO.Assets/SkillsLoader.cs` — carica l'elenco skill di default da `skills.mul`/`Skills.idx` (i file dati client stock) in una `List<SkillEntry>` (nome + flag "hasAction"/clickable). L'enum `SkillEntry.HardCodedName` (righe 90-150) **non è usato per popolare la lista** — è solo un riferimento storico/di comodo per indicizzare le skill note, non una fonte dati.
- `src/ClassicUO.Client/Network/PacketHandlers.cs:1921` (`UpdateSkills`, packet `0x3A`) — **qui la scoperta chiave**: quando il server manda il sottotipo `0xFE` (righe 1932-1953), il client **butta via** l'elenco skill caricato da `skills.mul` e lo **ricostruisce da zero con i dati ricevuti dal server** (`Client.Game.UO.FileManager.Skills.Skills.Clear()` poi un nuovo `SkillEntry` per ognuna, con nome letto come stringa ASCII dal pacchetto).
- `src/ClassicUO.Client/Game/Data/Skill.cs` — rappresentazione runtime per personaggio (valore, cap, lock); non impone alcun elenco fisso, è generica per indice.
- `src/ClassicUO.Client/Game/UI/Gumps/StandardSkillsGump.cs` / `SkillGumpAdvanced.cs` — le due varianti di finestra skill (scelta da opzione utente), disegnano la lista scorrendo `World.Player.Skills` — nessun limite hardcoded sul numero di voci per la vista standard.
- `src/ClassicUO.Client/Game/Managers/SkillsGroupManager.cs:19` — la vista "Advanced" (gruppi personalizzati di skill) usa un array `byte[60]` per gruppo con `0xFF` come sentinella "vuoto": **limite reale** — un indice skill deve stare in `0-254` (byte) e ogni singolo gruppo utente può contenere al massimo 60 skill (non l'elenco totale, solo per raggruppamento).

**Conclusione sulla fattibilità (la più importante di tutto ciò che abbiamo documentato finora):**

L'elenco skill **non è hardcoded nel client** in modo bloccante — è già pensato per essere **inviato dal server** (packet `0x3A` tipo `0xFE`). Questo significa che, lavorando anche lato server (ModernUO, che è già data-driven per le skill, vedi `custom-docs/design/skills.md` nel repo server), una skill completamente nuova può arrivare al client **senza ricompilare/modificare ClassicUO**, a patto che:
1. Il server implementi l'invio del packet `0x3A` tipo `0xFE` con l'elenco completo aggiornato (verificare/aggiungere lato ModernUO se non già presente).
2. L'indice della nuova skill resti sotto 255 (vincolo `byte` della vista Advanced) — praticamente mai un problema salvo elenchi enormi.
3. Non serva un'icona/asset dedicato nella UI oltre al nome testuale — la vista standard è puramente testuale, quindi anche questo non è un blocco.

Se invece si vuole un'icona o un trattamento grafico speciale per la nuova skill, quello sì richiederebbe una modifica al client (asset nuovo + eventuale mapping) — ma è lavoro pianificabile, non un muro architetturale.

## Verifica esterna (2026-09-13)

**⚠️ Discrepanza trovata — da riverificare nel codice prima di fare affidamento su questo dettaglio.** La documentazione storica del protocollo UO (PenUltima Online/POL, fonte di riferimento molto dettagliata e usata da più emulatori: [docs.polserver.com](https://docs.polserver.com/packets/index.php?Packet=0x3A)) elenca per il packet `0x3A` i type byte `0x00` (lista completa senza cap), `0x02` (lista completa con cap), `0xDF` (update singolo con cap), `0xFF` (update singolo) — **`0xFE` non compare in questa documentazione**. Anche il packet guide RustUO (moderno, [rustuo.org/packets](https://www.rustuo.org/packets/)) non lo menziona.

Questo non significa necessariamente che l'affermazione precedente fosse sbagliata — potrebbe essere un'estensione specifica di questa versione/fork di ClassicUO, non parte del protocollo classico documentato altrove, quindi assente dalle fonti storiche per definizione. Ma **non ho trovato nessuna conferma esterna indipendente** del comportamento "0xFE sostituisce l'intero elenco skill con nomi dal server" descritto in `PacketHandlers.cs:1932`. **Prima di pianificare lavoro reale su questo (es. il nuovo metodo server-side `SendSkillsList` suggerito in `skills.md` nel repo ModernUO), ri-leggere con attenzione quella riga di codice per confermare che l'interpretazione del byte `0xFE` sia corretta e non un errore di lettura.**

**Risolto (2026-09-13, ricontrollo diretto):** riletto `PacketHandlers.cs:1932` — `if (type == 0xFE)` esiste davvero, con esattamente il comportamento descritto sopra (svuota e ricostruisce l'elenco skill dai dati del server). **Il codice è confermato corretto**; l'assenza da POL/RustUO è quindi un limite di quelle fonti (documentazione di protocollo più vecchia/incompleta, non aggiornata su questa estensione), non un errore nostro. Il dettaglio resta affidabile come base per lavoro reale.

## Decisioni custom

Nessuna decisione ancora presa.
