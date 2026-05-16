using System.Net.Http;
using Kirokuu.Zerbitzuak;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using SQLite;

namespace Kirokuu.ZerbitzuakSaioa;

public static class ViewModelSalbuespenTratatzailea
{
    public static bool TratatuIrakurketa(
        Exception ex,
        Action<string?> ezarriErroreMezua,
        ILogger log,
        string testuingurua)
    {
        switch (ex)
        {
            case ErabiltzaileMurrizketaSalbuespena murEx:
                ezarriErroreMezua(murEx.Message);
                log.LogError(murEx, "{Testuingurua}: erabiltzaile murrizketa.", testuingurua);
                return true;
            case TursoExekuzioSalbuespena libEx:
                ezarriErroreMezua(LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                    ?? "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.");
                log.LogError(libEx, "{Testuingurua}: Turso errorea.", testuingurua);
                return true;
            case KeyNotFoundException knfEx:
                ezarriErroreMezua(LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua);
                log.LogError(knfEx, "{Testuingurua}: mapa errorea.", testuingurua);
                return true;
            case UriFormatException uriEx:
                ezarriErroreMezua("Argazkiaren helbidea baliogabea da.");
                log.LogError(uriEx, "{Testuingurua}: URI formatu errorea.", testuingurua);
                return true;
            case FormatException fmtEx:
                ezarriErroreMezua(LibsqlErroreaErabiltzaileMezura.BalioFormatuMezua);
                log.LogError(fmtEx, "{Testuingurua}: formatu errorea.", testuingurua);
                return true;
            case SQLiteException sqlEx:
                ezarriErroreMezua("Datu-base errorea: ezin izan da irakurri. Saiatu berriro.");
                log.LogError(sqlEx, "{Testuingurua}: SQLite errorea.", testuingurua);
                return true;
            case HttpRequestException httpEx:
                ezarriErroreMezua("Sare errorea: konexioa egiaztatu eta saiatu berriro.");
                log.LogError(httpEx, "{Testuingurua}: sare errorea.", testuingurua);
                return true;
            case UnauthorizedAccessException uaaEx:
                ezarriErroreMezua("Baimena ukatu da. Ezarpenetan baimena eman.");
                log.LogError(uaaEx, "{Testuingurua}: baimena ukatua.", testuingurua);
                return true;
            case FileNotFoundException fnfEx:
                ezarriErroreMezua("Fitxategia ez da aurkitu.");
                log.LogError(fnfEx, "{Testuingurua}: fitxategia ez da aurkitu.", testuingurua);
                return true;
            case IOException ioEx:
                ezarriErroreMezua("Fitxategi errorea: ezin izan dira datuak irakurri.");
                log.LogError(ioEx, "{Testuingurua}: fitxategi errorea.", testuingurua);
                return true;
            case InvalidOperationException opEx:
                ezarriErroreMezua("Eragiketa baliogabea. Berriz saiatu saioa hasita.");
                log.LogError(opEx, "{Testuingurua}: eragiketa baliogabea.", testuingurua);
                return true;
            case OperationCanceledException:
                ezarriErroreMezua("Eskaerak denbora muga gainditu du. Saiatu berriro.");
                return true;
            case TimeoutException:
                ezarriErroreMezua("Eskaerak denbora muga gainditu du. Saiatu berriro.");
                return true;
            case FeatureNotSupportedException:
                ezarriErroreMezua("Ezin da argazkia ireki gailu honetan.");
                return true;
            case AggregateException aggEx:
                log.LogError(aggEx, "{Testuingurua}: errore multzoa.", testuingurua);
                return TratatuIrakurketa(aggEx.GetBaseException(), ezarriErroreMezua, log, testuingurua);
            case Exception ustekabea:
                ezarriErroreMezua(LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(ustekabea)
                    ?? "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.");
                log.LogError(ustekabea, "{Testuingurua}: ustekabeko errorea.", testuingurua);
                return true;
            default:
                return false;
        }
    }

    public static bool TratatuErregistroIdazketa(
        Exception ex,
        Action<string?> ezarriErroreMezua,
        Action<string?> ezarriErroreXehetasuna,
        ILogger log,
        string testuingurua)
    {
        ezarriErroreXehetasuna(null);

        switch (ex)
        {
            case ErabiltzaileMurrizketaSalbuespena:
                ezarriErroreMezua("Datu bikoiztua: posta hau dagoeneko erregistratuta dago.");
                log.LogWarning(ex, "{Testuingurua}: murrizketa.", testuingurua);
                return true;
            case SQLiteException sqlEx when sqlEx.Result == SQLite3.Result.Constraint:
                var (eskN, eskX) = DatuBaseaErroreaErabiltzaileMezura.EskuratuSqliteMezuak(sqlEx.Message);
                ezarriErroreMezua(eskN ?? "Datu bikoiztua: posta hau dagoeneko erregistratuta dago.");
                ezarriErroreXehetasuna(eskX);
                log.LogWarning(sqlEx, "{Testuingurua}: SQLite murrizketa.", testuingurua);
                return true;
            case SQLiteException sqlEx:
                var (n, x) = DatuBaseaErroreaErabiltzaileMezura.EskuratuSqliteMezuak(sqlEx.Message);
                if (n is not null)
                {
                    ezarriErroreMezua(n);
                    ezarriErroreXehetasuna(x);
                }
                else
                    ezarriErroreMezua("Datu-base errorea: ezin izan da gorde. Saiatu berriro.");

                log.LogError(sqlEx, "{Testuingurua}: SQLite errorea.", testuingurua);
                return true;
            case TursoExekuzioSalbuespena libEx:
                var (nagusia, xehetasuna) = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezuaXehetasunarekin(libEx);
                ezarriErroreMezua(nagusia ?? "Datu-base errorea: ezin izan da gorde. Saiatu berriro.");
                ezarriErroreXehetasuna(xehetasuna);
                log.LogError(libEx, "{Testuingurua}: Turso errorea.", testuingurua);
                return true;
            case ArgumentException argEx:
                ezarriErroreMezua("Ezin izan da saioaren datuak gorde. Saiatu berriro edo berrabiarazi aplikazioa.");
                log.LogError(argEx, "{Testuingurua}: argumentu errorea.", testuingurua);
                return true;
            default:
                var (n2, x2) = LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezuaXehetasunarekin(ex);
                if (n2 is not null)
                {
                    ezarriErroreMezua(n2);
                    ezarriErroreXehetasuna(x2);
                    log.LogError(ex, "{Testuingurua}: errorea xehetasunarekin.", testuingurua);
                    return true;
                }

                if (TratatuIdazketa(ex, ezarriErroreMezua, log, testuingurua))
                    return true;

                ezarriErroreMezua(LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(ex)
                    ?? "Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.");
                log.LogError(ex, "{Testuingurua}: ustekabeko errorea.", testuingurua);
                return true;
        }
    }

    public static bool TratatuIdazketa(
        Exception ex,
        Action<string?> ezarriErroreMezua,
        ILogger log,
        string testuingurua)
    {
        switch (ex)
        {
            case ErabiltzaileMurrizketaSalbuespena murEx:
                ezarriErroreMezua(murEx.Message);
                log.LogError(murEx, "{Testuingurua}: erabiltzaile murrizketa.", testuingurua);
                return true;
            case SQLiteException sqlEx when sqlEx.Result == SQLite3.Result.Constraint:
                ezarriErroreMezua("Datu bikoiztua: sarrera hau dagoeneko existitzen da.");
                log.LogError(sqlEx, "{Testuingurua}: murrizketa errorea.", testuingurua);
                return true;
            case TursoExekuzioSalbuespena libEx:
                ezarriErroreMezua(LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                    ?? "Datu-base errorea: ezin izan da gorde. Saiatu berriro.");
                log.LogError(libEx, "{Testuingurua}: Turso idazketa errorea.", testuingurua);
                return true;
            case HttpRequestException httpEx:
                ezarriErroreMezua("Sare errorea: konexioa egiaztatu eta saiatu berriro.");
                log.LogError(httpEx, "{Testuingurua}: sare errorea.", testuingurua);
                return true;
            case UnauthorizedAccessException:
                ezarriErroreMezua("Baimena ukatu da. Ezarpenetan baimena eman.");
                return true;
            case IOException ioEx:
                ezarriErroreMezua("Fitxategi errorea: ezin izan da argazkia gorde.");
                log.LogError(ioEx, "{Testuingurua}: fitxategi errorea.", testuingurua);
                return true;
            case ArgumentNullException argEx:
                ezarriErroreMezua("Eremu bat edo gehiago hutsik daude. Bete beharrezko eremuak.");
                log.LogError(argEx, "{Testuingurua}: argumentu nulua.", testuingurua);
                return true;
            case ArgumentException argEx:
                ezarriErroreMezua("Baliogabeko balioa.");
                log.LogError(argEx, "{Testuingurua}: argumentu baliogabea.", testuingurua);
                return true;
            case NotSupportedException nsEx:
                ezarriErroreMezua("Eragiketa ez da onartzen gailu honetan.");
                log.LogError(nsEx, "{Testuingurua}: ez da onartzen.", testuingurua);
                return true;
            default:
                return TratatuIrakurketa(ex, ezarriErroreMezua, log, testuingurua);
        }
    }

    public static bool TratatuArgazkiEragiketa(
        Exception ex,
        Action<string?> ezarriErroreMezua,
        ILogger log,
        string testuingurua,
        bool kamera)
    {
        switch (ex)
        {
            case PermissionException:
                ezarriErroreMezua("Baimena ukatu da. Ezarpenetan baimena eman.");
                return true;
            case FeatureNotSupportedException:
                ezarriErroreMezua(kamera
                    ? "Gailu honek ez du kamerarik."
                    : "Gailu honek ez du galeria onartzen.");
                return true;
            default:
                log.LogError(ex, "{Testuingurua}: argazki errorea.", testuingurua);
                return false;
        }
    }
}
