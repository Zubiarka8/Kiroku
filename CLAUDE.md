# CLAUDE.md — Kiroku

## Project Overview

**Kiroku** — Corporate expense management app built with .NET MAUI for Android (phone + tablet adaptive).

- Employees submit expense tickets (meals, fuel, hotel, transport, tolls, parking, other).
- Admins approve or reject employee tickets, and create their own expenses (auto-approved).
- Employees see only their own tickets — never other users' data.

**Roles:**
- `Administratzailea` (Admin): approves/rejects tickets, creates own expenses (auto-approved), manages employees.
- `Langilea` (Employee): submits tickets, tracks their own status only.

---

## Technology Stack

| Concern | Technology |
|---|---|
| Language | C# 13 / .NET 9 |
| UI | .NET MAUI + XAML |
| Local DB | SQLite via sqlite-net-pcl |
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
ArgazkiIgotzeZerbitzua
```

---

## Project Structure

Follow this exact structure. Never create files outside it without asking first.

```
Proiektua/
  DatuBasea/
    Ereduak/
      AuditoretzaLoga.cs
      BidaiaTxostena.cs
      Erabiltzailea.cs
      GastuKontzeptua.cs
      GastuLerroa.cs
    SortzeAginduak/
      AuditoretzaLogaSortuSql.cs
      BidaiaTxostenakSortuSql.cs
      ErabiltzaileakSortuSql.cs
      GastuKontzeptuakSortuSql.cs
      GastuLerroakSortuSql.cs
    DatuBaseaEraikitzailea.cs
  DatuEreduak/
  EskemaIkuspegia/
    EskemaEreduak.cs
    EskemaIkuspegiModeloa.cs
  Bihurgailuak/
```

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

Admin and Employee have **completely separate Shell tab sets**. Never show admin tabs to employees or vice versa.

**Admin tabs:** Txartel Guztiak | Nire Gastuak | Langile Zerrenda | Ezarpenak

**Employee tabs:** Nire Txartelak | Txartel Berria | Ezarpenak

Admin's own tickets are created as `TxartelEgoera.Onartua` — no approval workflow.

---

## Database Rules

- Single connection (singleton) in `DatuBaseaZerbitzua`
- Always async: `CreateTableAsync`, `InsertAsync`, `QueryAsync`
- NEVER access DB on the UI thread
- Parameterized queries only — never string concatenation in SQL
- Indexes required on: `ErabiltzaileId`, `TxartelEgoera`

**Turso migration (when ready):** replace `sqlite-net-pcl` → `LibSQL.Client`, `SQLiteAsyncConnection` → `LibSQLConnection`. Connection string from env vars only, never hardcoded.

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

Photos are **never** stored as blobs in SQLite. The DB stores the URL only (`BidaiaTxostena.ArgazkiUrl`, `string?`).

Flow:
1. User picks photo via `MediaPicker`
2. Show local thumbnail immediately
3. On submit: `ArgazkiIgotzeZerbitzua` uploads to Cloudinary
4. Store returned HTTPS URL in `ArgazkiUrl`
5. If upload fails: show error Toast, allow retry — never silently save null

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

---

## Expense Types and Statuses

```
GastuMota: Bazkaria | Gasolina | GarraioPublikoa | Hotela |
           Peajea | Aparkalekua | Bidaia | Materialak | Bestelakoa

TxartelEgoera: Zain | Onartua | Ukatua
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

---

## Change Protocol

Before every non-trivial change:

1. **Plan:** state which files to modify, what change, why, and which files to leave untouched.
2. **Confirm:** for large or multi-file changes, present the plan and wait for approval.
3. **Execute:** only what was stated. If something unexpected appears — stop and report.
4. **Report:** summarize what changed, what did not, and any follow-up needed.

**Root cause first:** never apply a quick patch. Identify and explain the root cause, then fix it structurally. Mark any unavoidable workaround as temporary and provide the real fix plan.
