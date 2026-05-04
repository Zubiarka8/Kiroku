-- Turso / libSQL garapenerako soilik.
-- Kiroku.db zaharraren eskema (Erabiltzaileak, BidaiaTxostenak, etab.) aplikazioaren Erabiltzaileak taularekin bateraezin bada,
-- exekutatu hau Turso kontsolan edo SQL editorean, eta gero abiarazi aplikazioa (Erabiltzaileak sortuko du automatikoki).
-- KONTUZ: taula hauek eta haien datuak betiko ezabatzen dira.

PRAGMA foreign_keys = OFF;

DROP TABLE IF EXISTS GastuLerroak;
DROP TABLE IF EXISTS BidaiaTxostenak;
DROP TABLE IF EXISTS GastuKontzeptuak;
DROP TABLE IF EXISTS AuditoretzaLoga;
DROP TABLE IF EXISTS Erabiltzaileak;

PRAGMA foreign_keys = ON;
