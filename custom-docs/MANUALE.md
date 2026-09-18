# Manuale — come lavorare su questo fork

Guida pratica: dove mettere il codice, come tracciare le modifiche, come comportarsi con git. Stesso schema adottato nel repo sorella `ModernUO` (`custom-docs/MANUALE.md` lì), adattato perché ClassicUO è un client/engine — non ha un equivalente della cartella `Custom/` di ModernUO dove il codice nuovo è per definizione isolato dall'upstream.

## 0. Prima di scrivere codice: ragiona sulla "teoria"

Per sistemi nuovi o complessi (animazioni custom, asset loading, UI di skill/spell/effetti...), non si parte dal codice. Si ragiona prima in [`custom-docs/design/`](design/README.md): un file per sistema. Solo quando la teoria è stabile si passa all'implementazione. Dimmi "voglio ragionare su [sistema]" per iniziare — e controlla se esiste già una nota corrispondente nel repo sorella `ModernUO/custom-docs/design/`, perché molte feature toccano entrambi.

## 1. Nuovo file vs modifica di un file esistente

**Chiediti sempre: sto aggiungendo un file nuovo, o modificando qualcosa che esiste già?**

- **File nuovo** → nessun tracciamento necessario, non può collidere con un aggiornamento upstream.
- **Modifichi un file esistente** (qualunque cartella sotto `src/`) → **sì, va loggato**, vedi punto 2. Se non sei sicuro se un file sia "esistente": `git log --follow -- <percorso>` — se ha commit precedenti ai nostri, lo è.

Non c'è una cartella "sicura" equivalente al `Custom/` di ModernUO: ClassicUO è un engine, non contenuto dati, quindi anche una feature nuova spesso tocca file esistenti (es. `GameController.cs`, `PacketHandlers.cs`). Questo è normale qui, non un errore.

## 2. Ogni volta che modifichi un file esistente

Prima di committare, aggiungi una riga in [`custom-docs/CUSTOM_CHANGES.md`](CUSTOM_CHANGES.md):

```
| src/ClassicUO.Client/Percorso/File.cs | Motivo breve della modifica | 2026-09-14 |
```

Serve a capire, ad ogni sync con l'upstream `ClassicUO/ClassicUO`, quali aggiornamenti in arrivo rischiano di scontrarsi con le nostre modifiche.

## 3. Prima di ogni commit

1. **Build:** `dotnet build` dalla root del repo — deve passare pulito.
2. **Test:** `dotnet test` (copre `Assets/`, `Game/`, `IO/`, `Utility/`) — se hai toccato logica non banale, aggiungi/aggiorna test.
3. Rispetta le convenzioni già documentate in `CLAUDE.md`.
4. Se hai modificato un file esistente, controlla di aver aggiornato `CUSTOM_CHANGES.md`.

## 4. Git — regole semplici

- Mai `rebase`, `push --force`, o riscrivere la storia su `main`.
- Commit piccoli e descrittivi.
- Il push su `origin` (il tuo fork) si conferma sempre prima di farlo.
- Non toccare direttamente il remote `upstream` — è in sola lettura, serve solo per i `fetch`. **Mai aprire PR verso `ClassicUO/ClassicUO`.**

## 5. Quando vuoi sincronizzare con l'upstream ClassicUO

Dimmi "controlliamo gli aggiornamenti upstream". Poi:

1. `fetch` dall'upstream.
2. Riassunto in italiano di cosa è cambiato.
3. Segnalazione se qualche modifica upstream rischia di scontrarsi con qualcosa in `CUSTOM_CHANGES.md`.
4. Decisione insieme su se/come fare il merge.

## 6. Lavori non ancora conclusi

Un lavoro non è "finito" solo perché il codice compila — lo è quando l'hai verificato (compreso l'uso reale nel client, non solo il build) e me lo confermi. Finché non succede, resta tracciato in [`custom-docs/LAVORI_IN_CORSO.md`](LAVORI_IN_CORSO.md). Consultalo a inizio sessione.

## 7. Riprendere il lavoro in una nuova sessione

Non serve ripetere il contesto: le decisioni durevoli sono in memoria persistente e in questo repo (`CLAUDE.md`, `custom-docs/`). Basta aprire Claude Code dentro `C:\Users\massi\Documents\GitHub\ClassicUO` e dire cosa vuoi fare.

## Checklist rapida prima di ogni commit

- [ ] Se ho modificato un file esistente, ho aggiunto la riga in `CUSTOM_CHANGES.md`?
- [ ] `dotnet build` passa?
- [ ] `dotnet test` passa (e ho aggiunto test se serviva)?
- [ ] Il commit message spiega il *perché*, non solo il *cosa*?
