using System.Net.Http;
using Kirokuu.Zerbitzuak;
using Microsoft.Extensions.Logging;
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
            case TursoExekuzioSalbuespena libEx:
                ezarriErroreMezua(LibsqlErroreaErabiltzaileMezura.ErabiltzaileMezua(libEx)
                    ?? "Datu-base errorea: ezin izan da irakurri. Saiatu berriro.");
                log.LogError(libEx, "{Testuingurua}: Turso errorea.", testuingurua);
                return true;
            case KeyNotFoundException knfEx:
                ezarriErroreMezua(LibsqlErroreaErabiltzaileMezura.ZutabeEskemaMezua);
                log.LogError(knfEx, "{Testuingurua}: mapa errorea.", testuingurua);
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
            case InvalidOperationException opEx:
                ezarriErroreMezua("Eragiketa baliogabea. Berriz saiatu saioa hasita.");
                log.LogError(opEx, "{Testuingurua}: eragiketa baliogabea.", testuingurua);
                return true;
            case TaskCanceledException:
                ezarriErroreMezua("Eskaerak denbora muga gainditu du. Saiatu berriro.");
                return true;
            case Exception ustekabea:
                ezarriErroreMezua("Ustekabeko errorea gertatu da. Garatzailearekin jarri harremanetan.");
                log.LogError(ustekabea, "{Testuingurua}: ustekabeko errorea.", testuingurua);
                return true;
            default:
                return false;
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
            default:
                return TratatuIrakurketa(ex, ezarriErroreMezua, log, testuingurua);
        }
    }
}
