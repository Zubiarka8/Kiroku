-- Turso / libSQL garapenerako soilik.
-- Taula hauek eta haien datuak betiko ezabatzen dira. KONTUZ.

PRAGMA foreign_keys = OFF;

DROP TABLE IF EXISTS GastuLerroak;
DROP TABLE IF EXISTS BidaiaTxostenak;
DROP TABLE IF EXISTS GastuKontzeptuak;
DROP TABLE IF EXISTS AuditoretzaLoga;
DROP TABLE IF EXISTS Erabiltzaileak;

PRAGMA foreign_keys = ON;

CREATE TABLE Erabiltzaileak (
    ErabiltzaileId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    Izena TEXT NOT NULL,
    Abizena TEXT NOT NULL,
    Abizena2 TEXT NOT NULL DEFAULT '',
    DNI TEXT NOT NULL UNIQUE DEFAULT '',
    Email TEXT NOT NULL UNIQUE,
    Kargoa TEXT NOT NULL DEFAULT '',
    Sektorea INTEGER NOT NULL DEFAULT 0,
    KargoarenIdentifikatzailea INTEGER NOT NULL DEFAULT 0,
    Rola INTEGER NOT NULL,
    SorkuntzaData TEXT NOT NULL DEFAULT '',
    Pasahitza TEXT NOT NULL DEFAULT '',
    Aktiboa INTEGER NOT NULL DEFAULT 1,
    SaioHasieraSaiakerak INTEGER NOT NULL DEFAULT 0,
    SaioaBlokeoaAmaieraUtc TEXT
);

CREATE TABLE GastuKontzeptuak (
    KategoriaId INTEGER PRIMARY KEY NOT NULL,
    Izena TEXT NOT NULL DEFAULT '',
    Deskribapena TEXT NOT NULL DEFAULT '',
    IbilgailuaBeharrezkoa INTEGER NOT NULL DEFAULT 0,
    Estatusa TEXT NOT NULL DEFAULT '',
    GastuKontzeptuId INTEGER NOT NULL DEFAULT 0
);

CREATE TABLE BidaiaTxostenak (
    TxostenId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    ErabiltzaileId INTEGER NOT NULL,
    LangileDNI TEXT NOT NULL DEFAULT '',
    Saila TEXT NOT NULL DEFAULT '',
    Helmuga TEXT NOT NULL DEFAULT '',
    BidaiaHelburua TEXT NOT NULL DEFAULT '',
    HasieraData TEXT NOT NULL DEFAULT '',
    AmaieraData TEXT NOT NULL DEFAULT '',
    PertsonaKopurua INTEGER NOT NULL DEFAULT 0,
    JasoAurrerakina INTEGER NOT NULL DEFAULT 0,
    Egoera TEXT NOT NULL DEFAULT '',
    AdminOharra TEXT,
    AdminDNI TEXT,
    EmpresaIbilgailua INTEGER NOT NULL DEFAULT 0,
    MonetaKodea TEXT NOT NULL DEFAULT '',
    SorkuntzaData TEXT NOT NULL DEFAULT '',
    AzkenEguneraketa TEXT NOT NULL DEFAULT '',
    DataAprobazioa TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (ErabiltzaileId) REFERENCES Erabiltzaileak(ErabiltzaileId) ON DELETE RESTRICT,
    FOREIGN KEY (LangileDNI) REFERENCES Erabiltzaileak(DNI) ON DELETE RESTRICT ON UPDATE CASCADE,
    FOREIGN KEY (AdminDNI) REFERENCES Erabiltzaileak(DNI) ON DELETE RESTRICT ON UPDATE CASCADE
);

CREATE TABLE GastuLerroak (
    GastuId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    TxostenId INTEGER NOT NULL DEFAULT 0,
    KategoriaId INTEGER NOT NULL DEFAULT 0,
    GastuData TEXT NOT NULL DEFAULT '',
    GarraioBidea TEXT NOT NULL DEFAULT '',
    Zenbatekoa_Guztira REAL NOT NULL DEFAULT 0,
    Kilometroak REAL NOT NULL DEFAULT 0,
    TicketArgazkiBidea TEXT NOT NULL DEFAULT '',
    Oharrak TEXT NOT NULL DEFAULT '',
    KontzeptuId INTEGER NOT NULL DEFAULT 0,
    IbilgailuaBeharrezkoa INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (TxostenId) REFERENCES BidaiaTxostenak(TxostenId) ON DELETE CASCADE,
    FOREIGN KEY (KategoriaId) REFERENCES GastuKontzeptuak(KategoriaId) ON DELETE RESTRICT
);

CREATE TABLE AuditoretzaLoga (
    LogId INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    DiruSarreraId INTEGER,
    ErabiltzaileId INTEGER NOT NULL DEFAULT 0,
    Ekintza TEXT NOT NULL DEFAULT '',
    DataOrdua TEXT NOT NULL DEFAULT '',
    Deskribapena TEXT NOT NULL DEFAULT '',
    IP_Helbidea TEXT NOT NULL DEFAULT '',
    FOREIGN KEY (ErabiltzaileId) REFERENCES Erabiltzaileak(ErabiltzaileId) ON DELETE RESTRICT
);
