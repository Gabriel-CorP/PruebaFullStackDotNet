namespace Ventas.Application.Common.Constants;

public static class AppRoles
{
    public const string Administrador = "Administrador";
    public const string Operador = "Operador";
    public const string Todos = Administrador + "," + Operador;
}
