namespace AgendaPositiva.Web.Features.Commons.Domain;

public class Email {
    public string Value { get; private set; } = string.Empty;

    public Email(){}

    public static Result<Email> Crear(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result.Failure<Email>("Email cannot be empty.");
        }

        if (!email.Contains('@'))
        {
            return Result.Failure<Email>("Email format is invalid.");
        }

        return new Email { Value = email };
    }

    public override string ToString()
    {
        return Value;
    }
}

public class EmptyEmail: Email {}