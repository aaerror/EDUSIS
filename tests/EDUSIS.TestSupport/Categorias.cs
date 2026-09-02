namespace EDUSIS.TestSupport;

/// <summary>
/// Único origen válido de los valores de <c>[Trait("Categoria", …)]</c> en todas las suites
/// (contracts/test-support-api.md §2). La segmentación de <c>dotnet test</c> por capa de señal
/// depende de que estas constantes no se dupliquen como literales sueltos.
/// </summary>
public static class Categorias
{
	public const string Unidad = "Unidad";
	public const string Integracion = "Integracion";
	public const string E2E = "E2E";
}
