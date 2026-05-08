namespace Kirokuu.Zerbitzuak;

public sealed class TursoExekuzioSalbuespena : Exception
{
    public TursoExekuzioSalbuespena(string mezua) : base(mezua)
    {
    }

    public TursoExekuzioSalbuespena(string mezua, Exception barnekoa) : base(mezua, barnekoa)
    {
    }
}
