# CLAUDE.md — Kiroku

## Project Overview

**Kiroku** — Corporate expense management app built with .NET MAUI for Android (phone + tablet adaptive).

- Employees submit expense tickets (meals, fuel, hotel, transport, tolls, parking, other).
- Admins approve or reject employee tickets, and create their own expenses (auto-approved).
- Employees see only their own tickets — never other users' data.

**Roles (`ErabiltzaileRola` enum):**
- `Langilea = 0` (Employee): submits tickets, tracks their own status only.
- `Administratzailea = 1` (Admin): approves/rejects tickets, creates own expenses (auto-approved), manages employees.
- `ZuzendariNagusia = 2` (CEO): read-only dashboard access — sees the admin home (statistics) and own settings. Cannot manage employees, approve/reject tickets, or see movements.

---

## Technology Stack

| Concern | Technology |
|---|---|
| Language | C# 13 / .NET 9 |
| UI | .NET MAUI + XAML |
| Local DB | SQLite via sqlite-net-pcl + SQLCipher |
| Online DB | Turso (libSQL) — drop-in swap when ready |
| MVVM | CommunityToolkit.Mvvm |
| DI | Microsoft.Extensions.DependencyInjection |
| Images | MediaPicker + Cloudinary (URL stored in DB) |
| Auth | SecureStorage + SHA256 + salt (local) / JWT (online) |

---

## Basque Language Rule — Critical

ALL code identifiers must be in Basque (Euskara). No exceptions for:
- Class, method, variable, property, interface, enum names
- XAML `x:Name` and binding paths
- SQLite table and column names, file names
- All comments (`//`, `/* */`, `///`)

**Allowed in English:** .NET/MAUI API calls, NuGet packages, XAML attribute names, standard C# keywords, known interfaces (`INotifyPropertyChanged`, `ICommand`...).

**Confirmed names — use these exactly:**

```
AuditoretzaLoga       Erabiltzailea         GastuLerroa
BidaiaTxostena        GastuKontzeptua       ErabiltzaileId
Ekintza               DataOrdua             Deskribapena
IP_Helbidea           Administratzailea     Langilea
OnartuEgin()          UkatuEgin()           ZenbatekoGarbia
BidaliData            TxartelEgoera         ArgazkiUrl
ArgazkiIgotzeZerbitzua ZuzendariNagusia
```

---

## Project Structure

```
Kirokuu/                         ← git root / solution root
  Kirokuu.sln
  Kirokuu/                       ← MAUI project
    DatuBasea/
      Ereduak/
        AuditoretzaLoga.cs
        BidaiaTxostena.cs
        Erabiltzailea.cs
        ErabiltzaileRola.cs
        GastuKontzeptua.cs
        GastuLerroa.cs
      SortzeAginduak/
        TursoGarapenBerrezarpena.sql
    Pages/
    ViewModels/
    Zerbitzuak/
    ZerbitzuakSaioa/
      AutorizazioZerbitzua.cs
      DatuBaseaZerbitzua.cs
      DatuBaseaZerbitzuaAdministratzaileEkintzak.cs
      DatuBaseaZerbitzuaLangileEkintzak.cs
      ShellFitxaEraikitzailea.cs
  Kirokuu.Tests/
  Kirokuu.Zerbitzuak/
```

Never create files outside this structure without asking first.

---

## Architecture Rules

**MVVM — non-negotiable:**
- ViewModels inherit `ObservableObject` (CommunityToolkit.Mvvm)
- Properties: `[ObservableProperty]`, Commands: `[RelayCommand]`
- Code-behind: ONLY `InitializeComponent()` — zero business logic in Views
- Services injected via DI, never `new()`

**DI registration (MauiProgram.cs):**
- Singleton: `DatuBaseaZerbitzua`, `AutorizazioZerbitzua`, `ArgazkiIgotzeZerbitzua`, `EncryptionZerbitzua`
- Transient: ViewModels

---

## Role-Based Navigation

Three roles have completely separate Shell tab sets. Never mix tabs across roles.

**Admin (`Administratzailea`) tabs:**
`Hasiera` | `Erabiltzaileak` | `Mugimenduak` | `Ezarpenak`

**CEO (`ZuzendariNagusia`) tabs:**
`Hasiera` | `Ezarpenak`

**Employee (`Langilea`) tabs:**
`Hasiera` | `Nire txartelak` | `Txartelak (Kanban)` | `Ezarpenak`

Navigation built in `ShellFitxaEraikitzailea.KargatuAsync()` — checks role on every login.

**Permission helpers (`AutorizazioZerbitzua`):**
- `DaAdministratzaileaAsync()` → true only for `Administratzailea`
- `DaZuzendariNagusiaAsync()` → true only for `ZuzendariNagusia`
- `DaNagusikoEstadistikaSarbideaAsync()` → true for `Administratzailea` OR `ZuzendariNagusia`

Admin's own tickets are created as `TxartelEgoera.Onartua` — no approval workflow.
Employee settings: Izena, Abizena, Abizena2, DNI fields are **read-only** (`IsEnabled=false`) — only admins can edit them.

---

## Database Rules

- Single connection (singleton) in `DatuBaseaZerbitzua`
- Always async: `CreateTableAsync`, `InsertAsync`, `QueryAsync`
- NEVER access DB on the UI thread
- Parameterized queries only — never string concatenation in SQL
- Indexes required on: `ErabiltzaileId`, `TxartelEgoera`, `DNI` (UNIQUE)
- `PRAGMA foreign_keys = ON` set per-connection in `EgiaztatuSqliteKonezioaAsync` (SQLite does not persist this setting)

**FK constraints (SQLite + Turso):**

```
Erabiltzaileak
  PK  ErabiltzaileId
  UQ  DNI

BidaiaTxostenak
  FK  ErabiltzaileId  → Erabiltzaileak(ErabiltzaileId)  ON DELETE RESTRICT
  FK  LangileDNI      → Erabiltzaileak(DNI)              ON DELETE RESTRICT  ON UPDATE CASCADE
  FK  AdminDNI        → Erabiltzaileak(DNI)              ON DELETE RESTRICT  ON UPDATE CASCADE  [nullable]

GastuLerroak
  FK  TxostenId       → BidaiaTxostenak(TxostenId)       ON DELETE CASCADE
  FK  KategoriaId     → GastuKontzeptuak(KategoriaId)    ON DELETE RESTRICT

AuditoretzaLoga
  FK  ErabiltzaileId  → Erabiltzaileak(ErabiltzaileId)   ON DELETE RESTRICT
```

`sqlite-net-pcl` ORM does NOT generate FK DDL — use raw SQL (`CREATE TABLE IF NOT EXISTS`) for all FK tables. ORM (`CreateTableAsync`) is only used for `Erabiltzaileak` and `GastuKontzeptuak`.

**Turso migration (when ready):** replace `sqlite-net-pcl` → `LibSQL.Client`, `SQLiteAsyncConnection` → `LibSQLConnection`. Connection string from env vars only, never hardcoded.

---

## Data Models (current schema)

### `Erabiltzaileak`
| Column | Type | Notes |
|---|---|---|
| ErabiltzaileId | INTEGER PK AUTOINCREMENT | |
| Izena | TEXT NOT NULL | |
| Abizena | TEXT NOT NULL | |
| Abizena2 | TEXT NOT NULL | |
| DNI | TEXT NOT NULL UNIQUE | FK target |
| Email (Posta) | TEXT NOT NULL UNIQUE | |
| Kargoa | TEXT NOT NULL | |
| Sektorea | INTEGER | |
| KargoarenIdentifikatzailea | INTEGER | |
| Rola | INTEGER | 0=Langilea, 1=Admin, 2=CEO |
| Aktiboa | INTEGER DEFAULT 1 | |
| SorkuntzaData | TEXT NOT NULL | ISO 8601 |
| Pasahitza | TEXT NOT NULL | SHA256+salt |
| SaioHasieraSaiakerak | INTEGER | |
| SaioaBlokeoaAmaieraUtc | TEXT nullable | |

### `BidaiaTxostenak`
| Column | Type | Notes |
|---|---|---|
| TxostenId | INTEGER PK AUTOINCREMENT | |
| ErabiltzaileId | INTEGER FK | → Erabiltzaileak |
| LangileDNI | TEXT NOT NULL FK | → Erabiltzaileak(DNI), CASCADE on update |
| AdminDNI | TEXT nullable FK | → Erabiltzaileak(DNI), set when approved/rejected |
| EmpresaIbilgailua | INTEGER NOT NULL DEFAULT 0 | 1 if company vehicle used |
| Saila | TEXT NOT NULL | |
| Helmuga | TEXT NOT NULL | category name |
| BidaiaHelburua | TEXT NOT NULL | description |
| HasieraData | TEXT NOT NULL | ISO 8601 |
| AmaieraData | TEXT NOT NULL | ISO 8601 |
| PertsonaKopurua | INTEGER | |
| JasoAurrerakina | INTEGER DEFAULT 0 | |
| Egoera | TEXT NOT NULL | Zain / Onartua / Ukatua |
| AdminOharra | TEXT nullable | |
| MonetaKodea | TEXT NOT NULL DEFAULT 'EUR' | |
| SorkuntzaData | TEXT NOT NULL | ISO 8601 UTC |
| AzkenEguneraketa | TEXT NOT NULL | ISO 8601 UTC |
| DataAprobazioa | TEXT NOT NULL | ISO 8601 or empty |

### `GastuLerroak`
| Column | Type | Notes |
|---|---|---|
| GastuId | INTEGER PK AUTOINCREMENT | |
| TxostenId | INTEGER FK | → BidaiaTxostenak, CASCADE delete |
| KategoriaId | INTEGER FK | → GastuKontzeptuak |
| KontzeptuId | INTEGER | denormalized copy, no FK |
| GastuData | TEXT NOT NULL | ISO 8601 |
| GarraioBidea | TEXT NOT NULL | transport mode text |
| Zenbatekoa_Guztira | REAL | |
| Kilometroak | REAL | |
| TicketArgazkiBidea | TEXT NOT NULL | Cloudinary HTTPS URL or empty |
| Oharrak | TEXT NOT NULL | |
| IbilgailuaBeharrezkoa | INTEGER NOT NULL DEFAULT 0 | mirrors GastuKontzeptuak flag at time of creation |

### `GastuKontzeptuak`
| Column | Type | Notes |
|---|---|---|
| KategoriaId | INTEGER PK | |
| Izena | TEXT NOT NULL | category name |
| Deskribapena | TEXT NOT NULL | |
| IbilgailuaBeharrezkoa | INTEGER NOT NULL | 1 = requires vehicle selection |
| Estatusa | TEXT NOT NULL | |
| GastuKontzeptuId | INTEGER | |

### `AuditoretzaLoga`
| Column | Type | Notes |
|---|---|---|
| LogId | INTEGER PK AUTOINCREMENT | |
| ErabiltzaileId | INTEGER FK | → Erabiltzaileak, RESTRICT |
| Ekintza | TEXT NOT NULL | |
| DataOrdua | TEXT NOT NULL | ISO 8601 UTC |
| IP_Helbidea | TEXT | |

---

## Security Rules

- Passwords: SHA256 + unique salt per user — never plain text
- Tokens/session: `SecureStorage` only
- Auto-logout after inactivity; lock after 5 failed login attempts
- **Data isolation:** every query fetching user data MUST include `WHERE ErabiltzaileId = [currentUserId]`
- Role verified on every navigation — check BEFORE loading data, fail fast
- SQLite encrypted via SQLCipher — key derived from device value + app secret, stored in SecureStorage, never hardcoded
- HTTPS only — no HTTP allowed
- Never log tokens, request bodies, or user data
- Audit every approve/reject in `AuditoretzaLoga` — logs are read-only
- External secrets (Turso URL/token, Cloudinary keys) in `.env` only — `.env` must be in `.gitignore`

---

## Photo Upload Architecture

Photos are **never** stored as blobs in SQLite. The DB stores the URL only (`GastuLerroa.TicketArgazkia`, `string`).

Flow:
1. User picks photo via `MediaPicker`
2. Show local thumbnail immediately (`LokalArgazkiBidea`)
3. On submit: `ArgazkiIgotzeZerbitzua` uploads to Cloudinary
4. Store returned HTTPS URL in `TicketArgazkia`
5. If upload fails: show error Toast, allow retry — never silently save empty string

Rules: JPEG/PNG only, max 10 MB, 30s timeout, inject `IHttpClientFactory` (never `new HttpClient()`).

---

## Error Handling

Catch specific exceptions — never a generic catch-all as the only handler. Pattern:

```csharp
catch (SQLiteException sqlEx) when (sqlEx.Result == SQLite3.Result.Constraint)
    { ErroreMezua = "Datu bikoiztua: sarrera hau dagoeneko existitzen da."; }
catch (SQLiteException sqlEx)
    { ErroreMezua = "Datu-base errorea: ezin izan da gorde. Saiatu berriro."; LogErrorea(sqlEx); }
catch (HttpRequestException httpEx)
    { ErroreMezua = "Sare errorea: konexioa egiaztatu eta saiatu berriro."; LogErrorea(httpEx); }
catch (TaskCanceledException)
    { ErroreMezua = "Eskaerak denbora muga gainditu du. Saiatu berriro."; }
catch (UnauthorizedAccessException)
    { ErroreMezua = "Baimena ukatu da. Ezarpenetan baimena eman."; }
catch (Exception ex)
    { ErroreMezua = "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan."; LogErrorea(ex); }
```

- Try/catch only in ViewModels and Services — never in Views
- Every catch block must set `ErroreMezua` or show a Toast — never silent
- Every catch block must call `LogErrorea()` — never empty
- ViewModel must expose: `IsKargatzean (bool)`, `ErroreMezua (string?)`

---

## UX Rules

- Every screen has one clear primary action
- Loading states: always show a spinner/skeleton for async ops
- Empty states: always show a friendly Basque message
- Confirmation dialogs for destructive actions
- Toast/Snackbar feedback after every user action
- Status badges color-coded: Zain → amber | Onartua → green | Ukatua → red
- Lists sorted by `DataOrdua DESC`, pull-to-refresh on all lists
- Phone: StackLayout vertical, min 44px touch targets
- Tablet: Grid 2-column master-detail via `OnIdiom`
- Role/sector/cargo labels displayed in Spanish (e.g. "Administrador", "Empleado", "Finanzas")

---

## Expense Types and Statuses

```
GastuMota (KategoriaIzenak): Bazkaria | Gasolina | Garraio publikoa | Hotela |
                              Peajea | Aparkalekua | Bidaia | Materialak | Bestelakoa

TxartelEgoera: Zain | Onartua | Ukatua

GarraioBidea: set when GastuKontzeptua.IbilgailuaBeharDu = 1
              values from GarraioBideaBalioak.AukeraEstadioak
```

---

## Absolute Prohibitions

- No business logic in code-behind
- No `new()` for services — use DI
- No plain-text passwords
- No DB queries on the UI thread
- No English identifiers (see exceptions in §Basque rule)
- No empty catch blocks
- No generic-only catch
- No `Thread.Sleep` — use `Task.Delay`
- No DB queries in ViewModel constructors — use `OnAppearing`/`LoadAsync`
- No photo blobs in SQLite — URL only
- No hardcoded secrets or encryption keys
- No decorative comments, ASCII art, or border symbols (`===`, `---`) in code
- No modifications outside agreed scope without asking first
- No `AdminDNI = string.Empty` — must be `null` before admin decision (empty string violates FK)

---

## Change Protocol

Before every non-trivial change:

1. **Plan:** state which files to modify, what change, why, and which files to leave untouched.
2. **Confirm:** for large or multi-file changes, present the plan and wait for approval.
3. **Execute:** only what was stated. If something unexpected appears — stop and report.
4. **Report:** summarize what changed, what did not, and any follow-up needed.

**Root cause first:** never apply a quick patch. Identify and explain the root cause, then fix it structurally. Mark any unavoidable workaround as temporary and provide the real fix plan.
