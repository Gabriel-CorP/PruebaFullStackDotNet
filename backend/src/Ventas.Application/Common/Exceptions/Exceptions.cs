namespace Ventas.Application.Common.Exceptions;

/// <summary>Error de regla de negocio (incluye errores THROW 50000+ de los SP).</summary>
public class BusinessException : Exception
{
    public int Code { get; }
    public BusinessException(string message, int code = 0) : base(message) => Code = code;
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message) { }
}
