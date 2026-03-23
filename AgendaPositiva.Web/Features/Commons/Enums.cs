namespace AgendaPositiva.Web.Features.Commons;

public enum EstadoInscripcion
{
    Pendiente,
    Completado,
    NoAsistio
}

public enum TipoIdentificacion
{
    Cedula,
    Pasaporte,
    Nit,
    TarjetaIdentidad,
    CedulaExtranjeria,
    RegistroCivil
}

public enum Parentesco
{
    Esposa,
    Esposo,
    Hijo,
    Hija,
    Padre,
    Madre,
    Hermano,
    Hermana,
    Abuelo,
    Abuela,
    Nieto,
    Nieta,
    Tio,
    Tia,
    Sobrino,
    Sobrina,
    Primo,
    Prima,
    Cunado,
    Cunada,
    Suegro,
    Suegra,
    Yerno,
    Nuera,
    Otro
}

public enum Genero
{
    Masculino,
    Femenino,
}

public static class ParentescoExtensions
{
    /// <summary>
    /// Dado un parentesco seleccionado ("A es mi Tío"), retorna el inverso
    /// según el género de quien registra ("yo soy Sobrino/Sobrina de A").
    /// </summary>
    public static Parentesco ObtenerInverso(this Parentesco parentesco, Genero generoPropio) => parentesco switch
    {
        Parentesco.Esposa => Parentesco.Esposo,
        Parentesco.Esposo => Parentesco.Esposa,
        Parentesco.Hijo => generoPropio == Genero.Masculino ? Parentesco.Padre : Parentesco.Madre,
        Parentesco.Hija => generoPropio == Genero.Masculino ? Parentesco.Padre : Parentesco.Madre,
        Parentesco.Padre => generoPropio == Genero.Masculino ? Parentesco.Hijo : Parentesco.Hija,
        Parentesco.Madre => generoPropio == Genero.Masculino ? Parentesco.Hijo : Parentesco.Hija,
        Parentesco.Hermano => generoPropio == Genero.Masculino ? Parentesco.Hermano : Parentesco.Hermana,
        Parentesco.Hermana => generoPropio == Genero.Masculino ? Parentesco.Hermano : Parentesco.Hermana,
        Parentesco.Abuelo => generoPropio == Genero.Masculino ? Parentesco.Nieto : Parentesco.Nieta,
        Parentesco.Abuela => generoPropio == Genero.Masculino ? Parentesco.Nieto : Parentesco.Nieta,
        Parentesco.Nieto => generoPropio == Genero.Masculino ? Parentesco.Abuelo : Parentesco.Abuela,
        Parentesco.Nieta => generoPropio == Genero.Masculino ? Parentesco.Abuelo : Parentesco.Abuela,
        Parentesco.Tio => generoPropio == Genero.Masculino ? Parentesco.Sobrino : Parentesco.Sobrina,
        Parentesco.Tia => generoPropio == Genero.Masculino ? Parentesco.Sobrino : Parentesco.Sobrina,
        Parentesco.Sobrino => generoPropio == Genero.Masculino ? Parentesco.Tio : Parentesco.Tia,
        Parentesco.Sobrina => generoPropio == Genero.Masculino ? Parentesco.Tio : Parentesco.Tia,
        Parentesco.Primo => generoPropio == Genero.Masculino ? Parentesco.Primo : Parentesco.Prima,
        Parentesco.Prima => generoPropio == Genero.Masculino ? Parentesco.Primo : Parentesco.Prima,
        Parentesco.Cunado => generoPropio == Genero.Masculino ? Parentesco.Cunado : Parentesco.Cunada,
        Parentesco.Cunada => generoPropio == Genero.Masculino ? Parentesco.Cunado : Parentesco.Cunada,
        Parentesco.Suegro => generoPropio == Genero.Masculino ? Parentesco.Yerno : Parentesco.Nuera,
        Parentesco.Suegra => generoPropio == Genero.Masculino ? Parentesco.Yerno : Parentesco.Nuera,
        Parentesco.Yerno => generoPropio == Genero.Masculino ? Parentesco.Suegro : Parentesco.Suegra,
        Parentesco.Nuera => generoPropio == Genero.Masculino ? Parentesco.Suegro : Parentesco.Suegra,
        _ => Parentesco.Otro,
    };
}