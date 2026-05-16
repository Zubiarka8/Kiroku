CREATE TABLE IF NOT EXISTS GastuKontzeptuak (
    KategoriaId INTEGER PRIMARY KEY NOT NULL,
    Izena TEXT NOT NULL DEFAULT '',
    Deskribapena TEXT NOT NULL DEFAULT '',
    IbilgailuaBeharrezkoa INTEGER NOT NULL DEFAULT 0,
    Estatusa TEXT NOT NULL DEFAULT '',
    GastuKontzeptuId INTEGER NOT NULL DEFAULT 0
);
