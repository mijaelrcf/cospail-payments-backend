namespace Domain.Common;

/// <summary>
/// Generación centralizada de identificadores.
/// Usa UUID versión 7 (ordenable por tiempo): reduce la fragmentación de los
/// índices btree en Postgres frente a los UUID v4 aleatorios.
/// El tipo de columna (<c>uuid</c>) no cambia, por lo que coexiste con los
/// UUID v4 ya persistidos sin necesidad de migración.
/// </summary>
public static class NewId
{
    /// <summary>
    /// Crea un UUID versión 7.
    /// </summary>
    public static Guid V7() => Guid.CreateVersion7();
}
