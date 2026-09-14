using FluentValidation;

namespace Application.Validators;

/// <summary>
/// Reglas de validación compartidas por los validadores de la API.
/// Centralizan los mensajes para que no diverjan entre endpoints.
/// </summary>
public static class ValidationRules
{
    /// <summary>
    /// Código fijo de socio: entero mayor a cero.
    /// </summary>
    public static IRuleBuilderOptions<T, int> FixedCode<T>(this IRuleBuilder<T, int> rule) =>
        rule.GreaterThan(0).WithMessage("fixedCode debe ser mayor a cero.");

    /// <summary>
    /// Documento de identidad o NIT: requerido.
    /// </summary>
    public static IRuleBuilderOptions<T, string> DocumentId<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().WithMessage("documentId es requerido.");

    /// <summary>
    /// Identificador de pago agrupado: requerido.
    /// </summary>
    public static IRuleBuilderOptions<T, Guid> PagoCospailId<T>(this IRuleBuilder<T, Guid> rule) =>
        rule.NotEmpty().WithMessage("pagoCospailId es requerido.");
}
