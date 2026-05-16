"""Kiroku — seed de 7 usuarios demo en Turso.

Inserta los 7 usuarios definidos en ../ejercicios.md (seccion 1) en la
tabla Erabiltzaileak. Replica el formato de pasahitza usado por
PasahitzaZerbitzua.cs:  gatzaBase64 + '|' + sha256(UTF8(pasahitza + gatzaBase64))Base64.

Asigna directamente el Rola final (0=Langilea, 1=Administratzailea,
2=ZuzendariNagusia), evitando el paso manual de promocion en libSQL Studio.

Idempotente: usa INSERT OR IGNORE sobre el indice unico Email.

Solo stdlib. Lee credenciales de ../.env (TURSO_DATABASE_URL, TURSO_AUTH_TOKEN).
"""

from __future__ import annotations

import base64
import hashlib
import json
import os
import secrets
import sys
import urllib.error
import urllib.request
from datetime import datetime, timezone
from pathlib import Path

PASAHITZA_KOMUNA = "adminadmin"

ERABILTZAILEAK: list[dict[str, object]] = [
    {
        "Izena": "Aitor",     "Abizena": "Etxebarria", "Abizena2": "Goikoetxea",
        "DNI": "12345678Z",   "Email": "pfinantzak@gmail.com",
        "Sektorea": "Finantzak", "Kargoa": "Kontularia",
        "Rola": 0,
    },
    {
        "Izena": "Maialen",   "Abizena": "Aguirre",    "Abizena2": "Zabala",
        "DNI": "23456789D",   "Email": "pmarketina@gmail.com",
        "Sektorea": "Marketina", "Kargoa": "Komunitate kudeatzailea",
        "Rola": 0,
    },
    {
        "Izena": "Iker",      "Abizena": "Mendizabal", "Abizena2": "Urrutia",
        "DNI": "34567890V",   "Email": "psalmentak@gmail.com",
        "Sektorea": "Salmentak", "Kargoa": "Komertziala",
        "Rola": 0,
    },
    {
        "Izena": "Nahia",     "Abizena": "Otxoa",      "Abizena2": "Beristain",
        "DNI": "45678901G",   "Email": "afinantzak@gmail.com",
        "Sektorea": "Finantzak", "Kargoa": "Finantza analista",
        "Rola": 1,
    },
    {
        "Izena": "Garazi",    "Abizena": "Aldekoa",    "Abizena2": "Iturralde",
        "DNI": "56789012B",   "Email": "apmarketina@gmail.com",
        "Sektorea": "Marketina", "Kargoa": "SEO espezialista",
        "Rola": 1,
    },
    {
        "Izena": "Eneko",     "Abizena": "Bidegain",   "Abizena2": "Aranguren",
        "DNI": "67890123B",   "Email": "apsalmentak@gmail.com",
        "Sektorea": "Salmentak", "Kargoa": "Salmenta kudeatzailea",
        "Rola": 1,
    },
    {
        "Izena": "Olatz",     "Abizena": "Urresti",    "Abizena2": "Lizundia",
        "DNI": "78901234X",   "Email": "ceo@gmail.com",
        "Sektorea": "Finantzak", "Kargoa": "Sistema administratzailea",
        "Rola": 2,
    },
]


def kargatu_env(env_bidea: Path) -> dict[str, str]:
    """Lee .env minimal sin dependencias externas."""
    if not env_bidea.is_file():
        raise SystemExit(f"Ez da .env aurkitu: {env_bidea}")
    aldagaiak: dict[str, str] = {}
    for lerroa in env_bidea.read_text(encoding="utf-8").splitlines():
        garbia = lerroa.strip()
        if not garbia or garbia.startswith("#") or "=" not in garbia:
            continue
        gakoa, _, balioa = garbia.partition("=")
        aldagaiak[gakoa.strip()] = balioa.strip().strip('"').strip("'")
    return aldagaiak


def sortu_pasahitza_katea(pasahitza: str) -> str:
    """Sortu 'gatzaBase64|hashBase64' bezelaxe PasahitzaZerbitzua.cs-k."""
    gatza_byteak = secrets.token_bytes(16)
    gatza_b64 = base64.b64encode(gatza_byteak).decode("ascii")
    sarrera = (pasahitza + gatza_b64).encode("utf-8")
    hash_b64 = base64.b64encode(hashlib.sha256(sarrera).digest()).decode("ascii")
    if "|" in gatza_b64 or "|" in hash_b64:
        raise ValueError("Pasahitzaren barruko banaketa karakterea ez da onartzen.")
    return f"{gatza_b64}|{hash_b64}"


def turso_https_url(libsql_url: str) -> str:
    """libsql://host -> https://host."""
    if libsql_url.startswith("libsql://"):
        return "https://" + libsql_url[len("libsql://"):]
    if libsql_url.startswith("https://"):
        return libsql_url
    raise SystemExit(f"TURSO_DATABASE_URL formatu ezezaguna: {libsql_url}")


def lotura_arg(balioa: object) -> dict[str, object]:
    """Itzuli libSQL HTTP pipeline-k espero duen 'args' elementua."""
    if balioa is None:
        return {"type": "null"}
    if isinstance(balioa, bool):
        return {"type": "integer", "value": str(int(balioa))}
    if isinstance(balioa, int):
        return {"type": "integer", "value": str(balioa)}
    if isinstance(balioa, float):
        return {"type": "float", "value": balioa}
    return {"type": "text", "value": str(balioa)}


def exekutatu_sql(base_url: str, token: str, sql: str, args: list[object]) -> dict:
    eskaera_gorputza = {
        "requests": [
            {
                "type": "execute",
                "stmt": {
                    "sql": sql,
                    "args": [lotura_arg(a) for a in args],
                },
            },
            {"type": "close"},
        ]
    }
    eskaera = urllib.request.Request(
        url=f"{base_url}/v2/pipeline",
        data=json.dumps(eskaera_gorputza).encode("utf-8"),
        method="POST",
        headers={
            "Authorization": f"Bearer {token}",
            "Content-Type": "application/json",
        },
    )
    try:
        with urllib.request.urlopen(eskaera, timeout=30) as erantzuna:
            return json.loads(erantzuna.read().decode("utf-8"))
    except urllib.error.HTTPError as ex:
        gorputza = ex.read().decode("utf-8", errors="replace")
        raise SystemExit(f"Turso HTTP {ex.code}: {gorputza}") from ex


def txertatu_erabiltzailea(base_url: str, token: str, e: dict[str, object]) -> bool:
    pasahitza_katea = sortu_pasahitza_katea(PASAHITZA_KOMUNA)
    orain_iso = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.%fZ")
    sql = (
        "INSERT INTO Erabiltzaileak ("
        "Izena, Abizena, Abizena2, DNI, Email, Kargoa, Sektorea, Rola, "
        "SorkuntzaData, Pasahitza, Aktiboa, SaioHasieraSaiakerak, SaioaBlokeoaAmaieraUtc"
        ") VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?) "
        "ON CONFLICT(Email) DO NOTHING;"
    )
    args = [
        e["Izena"], e["Abizena"], e["Abizena2"], e["DNI"],
        e["Email"], e["Kargoa"], e["Sektorea"], e["Rola"],
        orain_iso, pasahitza_katea, 1, 0, None,
    ]
    emaitza = exekutatu_sql(base_url, token, sql, args)
    # libSQL devuelve affected_row_count dentro de result.affected_row_count
    saiakerak = emaitza.get("results", [])
    affected = 0
    if saiakerak:
        response = saiakerak[0].get("response", {})
        result = response.get("result", {})
        affected = int(result.get("affected_row_count", 0))
    return affected > 0


def nagusia() -> int:
    env_bidea = Path(__file__).resolve().parent.parent / ".env"
    aldagaiak = kargatu_env(env_bidea)
    libsql_url = aldagaiak.get("TURSO_DATABASE_URL") or os.environ.get("TURSO_DATABASE_URL")
    token = aldagaiak.get("TURSO_AUTH_TOKEN") or os.environ.get("TURSO_AUTH_TOKEN")
    if not libsql_url or not token:
        raise SystemExit("TURSO_DATABASE_URL eta TURSO_AUTH_TOKEN behar dira (.env edo ingurune-aldagaiak).")

    base_url = turso_https_url(libsql_url)
    print(f"Turso endpoint: {base_url}")

    txertatuak = 0
    aurretik_zeudenak = 0
    for e in ERABILTZAILEAK:
        try:
            sortua = txertatu_erabiltzailea(base_url, token, e)
        except SystemExit:
            raise
        except Exception as ex:  # noqa: BLE001
            print(f"[!] {e['Email']} txertatzean errorea: {ex}", file=sys.stderr)
            continue
        if sortua:
            txertatuak += 1
            print(f"[+] Sortua: {e['Email']:<28}  Rola={e['Rola']}")
        else:
            aurretik_zeudenak += 1
            print(f"[=] Lehendik dago: {e['Email']}")

    print()
    print(f"Berriak sortuta: {txertatuak}")
    print(f"Lehendik zeudenak: {aurretik_zeudenak}")
    print(f"Guztira maneiatuak: {len(ERABILTZAILEAK)}")
    return 0


if __name__ == "__main__":
    sys.exit(nagusia())
