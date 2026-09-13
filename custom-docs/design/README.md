# Design notes — ClassicUO (client)

Stessa logica delle note di design nel repo server (ModernUO): appunti su come funziona il client *prima* di scriverci modifiche, per capire la fattibilità.

- [`asset-loading.md`](asset-loading.md) — come il client carica art/gump/stringhe, incluso il meccanismo per asset "propri" di uno shard
- [`skills-ui.md`](skills-ui.md) — skill gump, elenco skill, da dove vengono nomi/posizioni
- [`spells-ui.md`](spells-ui.md) — spellbook, icone spell, definizioni
- [`effects-ui.md`](effects-ui.md) — buff bar, icone di stato, effetti visivi lato client

Vedi anche `custom-docs/design/README.md` nel repo ModernUO — questi due insiemi di note vanno letti insieme quando si valuta una modifica che tocca sia server sia client (es. una skill nuova).
