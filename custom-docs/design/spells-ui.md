# Spell UI — design (ClassicUO)

## Come funziona in ClassicUO (riferimento per la fattibilità)

**File chiave** (`src/ClassicUO.Client/Game/Data/`):
- `SpellDefinition.cs` — classe dati: nome, ID numerico globale (`fullidx`), icona gump (`GumpIconID`/`GumpIconSmallID`, ID di asset grafico), mana cost, min skill, mantra (`PowerWords`), tithing cost, target type, reagenti.
- `SpellsMagery.cs`, `SpellsNecromancy.cs`, `SpellsChivalry.cs`, `SpellsBushido.cs`, `SpellsNinjitsu.cs`, `SpellsSpellweaving.cs`, `SpellsMysticism.cs`, `SpellsMastery.cs` — una classe statica per scuola, ciascuna con un `Dictionary<int, SpellDefinition>` **hardcoded** (indice locale 1..N → dati spell).
- `Game/UI/Gumps/SpellbookGump.cs` — il libro: legge dalle classi sopra via `SpellsMagery.GetSpell(...)` ecc., con un blocco `switch (_spellBookType)` ripetuto più volte per layout/pagine/icone specifiche di ogni scuola.

**ID globali (`fullidx`) e fasce per scuola** — dispatch in `SpellDefinition.FullIndexGetSpell`/`FullIndexSetModifySpell`: Magery 1-99, Necromancy 100-199, Chivalry 200-299, Bushido 300-499, Ninjitsu fino a 599, Spellweaving fino a 677, Mysticism 678-699, Mastery ≥700 (fino a 799, limite fisso hardcoded). Queste fasce lato client devono corrispondere esattamente agli spellID che manda il server: coerente con quanto già documentato lato ModernUO (`custom-docs/design/spells.md` nel repo server) — stesso schema di numerazione, quindi i due lati sono già allineabili.

**Punto di estensione più importante:** `SpellDefinition.FullIndexSetModifySpell(fullidx, id, iconid, smalliconid, minskill, manacost, tithing, name, words, target, regs)` — metodo pubblico statico che **registra o sovrascrive** una definizione spell per qualunque `fullidx` (1-799) **a runtime**, senza toccare i dizionari hardcoded per-scuola. Nello stock client è chiamato solo da `UseSpellButtonGump.cs` (per la UI di assegnazione hotbar) — non è collegato a nessun file di config esterno, ma è già lì, pubblico e pronto per essere richiamato da codice nostro (es. un piccolo bootstrap custom eseguito all'avvio) per definire spell nuovi senza modificare i file esistenti.

**Fattibilità:**

| Modifica | Fattibilità | Note |
|---|---|---|
| Nuovo spell con ID libero dentro la fascia di una scuola esistente, riusando un'icona già presente | **Facile** — una chiamata a `FullIndexSetModifySpell` da un file nuovo, zero file esistenti toccati | L'ID deve combaciare con quello che manda il server (ModernUO) |
| Nuovo spell con icona/grafica mai vista | **Facile lato codice**, serve anche l'asset grafico nuovo (vedi `asset-loading.md`) | Stesso meccanismo, solo l'icona è un ID che deve risolvere a un'immagine reale |
| Nuova scuola di magia interamente nuova (nuovo `SpellBookType`) | **Media-difficile** — non basta il dizionario dati: `SpellbookGump.cs` ha `switch` hardcoded per scuola (layout pagine, icone libro, casi speciali tipo "solo Magery ha pagina X") che vanno estesi | Serve modificare `SpellbookGump.cs` (file esistente, da loggare) oltre a definire i dati; il tetto `fullidx <= 799` limita lo spazio ID disponibile per una fascia tutta nuova |

**Insidia principale:** gli ID spell sono un contratto condiviso tra client e server — qualunque spell custom richiede far combaciare esattamente `fullidx` lato ClassicUO e lo spellID inviato da ModernUO (vedi vincolo bitmask a 64 bit già documentato lato server).

## Verifica esterna (2026-09-13)

**Nessuna conferma esterna diretta trovata per `SpellDefinition.FullIndexSetModifySpell(...)`** — né in forum ServUO, né in discussioni GitHub pubbliche. La ricerca ha però confermato indirettamente il contesto: fonti esterne concordano che in ClassicUO "i valori sono hardcoded" e le definizioni spell vivono in classi come `SpellsMagery.cs` con la classe `SpellDefinition` — coerente con quanto già verificato leggendo il codice. Il metodo specifico per registrare/sovrascrivere uno spell a runtime resta però un dettaglio di implementazione troppo di nicchia per comparire in discussioni pubbliche indicizzate — non è un segnale negativo, è plausibile che sia corretto ma semplicemente non documentato altrove. Nessuna discrepanza trovata, ma nemmeno conferma indipendente: prenderlo come "verificato solo nel codice", non come "prassi nota alla community".

## Decisioni custom

Nessuna decisione ancora presa.
