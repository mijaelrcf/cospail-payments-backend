namespace Application.Common.Exceptions;

/// <summary>
/// Error funcional devuelto por Banco Económico (rechazo con código y mensaje).
/// El mensaje ya viene en español y apto para mostrarse al usuario final,
/// por lo que el middleware lo propaga en el detalle de la respuesta
/// en lugar de ocultarlo como error 500 genérico.
/// </summary>
public sealed class BankOperationException : Exception
{
    /// <summary>
    /// Código funcional devuelto por el banco (no es un status HTTP).
    /// </summary>
    public int BankResponseCode { get; }

    public BankOperationException(int bankResponseCode, string message)
        : base(message)
    {
        BankResponseCode = bankResponseCode;
    }
}
