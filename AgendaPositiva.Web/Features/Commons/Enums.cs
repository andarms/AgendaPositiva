namespace AgendaPositiva.Web.Features.Commons;

public enum EstadoInscripcion
{
    Pendiente,
    Abono1,
    Abono2,
    Completado,
    NoVaAsistir
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

public enum ServicioInscripcion
{
    Fvkids,
    CdaAdolescentes,
    Jovenes,
    Ujier,
    Seguridad,
    Enfermeria,
    Alimentacion,
    Aseo,
    Expolibro,
    Ofrenda,
    Alabanza,
    Multimedia,
    Otros
}

public enum TipoSangre
{
    OPositivo,
    ONegativo,
    APositivo,
    ANegativo,
    BPositivo,
    BNegativo,
    ABPositivo,
    ABNegativo
}

public static class TipoSangreExtensions
{
    public static string Descripcion(this TipoSangre tipo) => tipo switch
    {
        TipoSangre.OPositivo => "O+",
        TipoSangre.ONegativo => "O−",
        TipoSangre.APositivo => "A+",
        TipoSangre.ANegativo => "A−",
        TipoSangre.BPositivo => "B+",
        TipoSangre.BNegativo => "B−",
        TipoSangre.ABPositivo => "AB+",
        TipoSangre.ABNegativo => "AB−",
        _ => tipo.ToString()
    };
}

public static class ServicioInscripcionExtensions
{
    public static string Descripcion(this ServicioInscripcion servicio) => servicio switch
    {
        ServicioInscripcion.Fvkids => "FVKIDS (Niños)",
        ServicioInscripcion.CdaAdolescentes => "CDA (Adolescentes)",
        ServicioInscripcion.Jovenes => "Jóvenes",
        ServicioInscripcion.Ujier => "Ujier",
        ServicioInscripcion.Seguridad => "Seguridad",
        ServicioInscripcion.Enfermeria => "Enfermería",
        ServicioInscripcion.Alimentacion => "Alimentación",
        ServicioInscripcion.Aseo => "Aseo",
        ServicioInscripcion.Expolibro => "Expolibro",
        ServicioInscripcion.Ofrenda => "Ofrenda",
        ServicioInscripcion.Alabanza => "Alabanza",
        ServicioInscripcion.Multimedia => "Multimedia",
        ServicioInscripcion.Otros => "Otros",
        _ => servicio.ToString()
    };
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