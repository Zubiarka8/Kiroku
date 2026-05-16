CREATE TABLE IF NOT EXISTS AuditoretzaLoga (
    LogId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    TxostenId INTEGER,
    ErabiltzaileId INTEGER NOT NULL DEFAULT 0,
    LangileId INTEGER,
    Ekintza TEXT NOT NULL DEFAULT '',
    DataOrdua TEXT NOT NULL DEFAULT '',
    Deskribapena TEXT NOT NULL DEFAULT '',
    IP_Helbidea TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (ErabiltzaileId) REFERENCES Erabiltzaileak(ErabiltzaileId) ON DELETE RESTRICT,
    FOREIGN KEY (LangileId) REFERENCES Erabiltzaileak(ErabiltzaileId) ON DELETE SET NULL,
    FOREIGN KEY (TxostenId) REFERENCES BidaiaTxostenak(TxostenId) ON DELETE SET NULL
);
