# Lavori in corso

Traccia i lavori **non ancora ufficialmente terminati** — un lavoro esce da questa lista solo quando è stato verificato (build **e** provato nel client reale) e l'utente lo conferma concluso, non quando il codice è scritto/compila.

Formato per ogni voce: cosa è stato fatto, cosa manca per chiuderlo, stato attuale.

---

## Magic Shield (rendering HP-bar lato client)

- **Cosa è stato fatto:** implementate tutte e 5 le task del piano `custom-docs/plans/2026-09-16-magic-shield-classicuo-plan.md` — nuovo campo `Entity.MagicShield`, parsing del sub-comando `0xBF`/`0x4D53` in `PacketHandlers.cs`, formula condivisa `BaseHealthBarGump.CalculateShieldSegments` e rendering del segmento viola "scudo" su tutte e tre le superfici di disegno (linea HP overhead, health bar "modern line-skin", health bar "classic gump-art"). Passata anche una revisione finale di tutto il branch che ha corretto 4 problemi (un bug di clamp nella formula, la mancata sincronizzazione di `_shieldBar.IsVisible` quando un mobile esce/rientra dal range, una giustificazione errata nel piano su `Entity.HitsPercentage`, e i due tracking doc di questo repo mai aggiornati). Build pulita e suite di unit test verde (195/195).
- **Cosa manca per chiuderlo:** la sezione "Manual verification" del piano non è ancora stata eseguita — serve lanciare il client contro un'istanza ModernUO con la controparte server-side, applicare uno scudo con il comando GM `[MagicShield`, e controllare tutte e 3 le superfici di rendering, incluso il caso di un mobile che esce e rientra dal range con scudo attivo e il caso di overheal con scudo attivo.
- **Stato attuale:** implementato e testato a livello di unit test, ma **non ancora confermato funzionante in un client reale** — resta in questa lista finché la verifica manuale non viene fatta e confermata.

---

(nessun altro lavoro in sospeso al momento)
