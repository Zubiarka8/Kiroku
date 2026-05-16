INSERT OR IGNORE INTO GastuKontzeptuak
  (KategoriaId, Izena, Deskribapena, IbilgailuaBeharrezkoa, Estatusa, GastuKontzeptuId)
VALUES
  (1, 'Bazkaria',         'Jangela eta bazkari gastuak',   0, 'Aktibo', 1),
  (2, 'Gasolina',         'Erregai gastuak',               1, 'Aktibo', 2),
  (3, 'Garraio publikoa', 'Autobus, metro eta trena',      0, 'Aktibo', 3),
  (4, 'Hotela',           'Ostatua eta gau-pasak',         0, 'Aktibo', 4),
  (5, 'Peajea',           'Autobide eta tunelak',          1, 'Aktibo', 5),
  (6, 'Aparkalekua',      'Aparkagune gastuak',            1, 'Aktibo', 6),
  (7, 'Bidaia',           'Hegazkin eta garraio nagusiak', 0, 'Aktibo', 7),
  (8, 'Materialak',       'Bulego eta lan materialak',     0, 'Aktibo', 8),
  (9, 'Bestelakoa',       'Sailkatu gabeko gastuak',       0, 'Aktibo', 9);
