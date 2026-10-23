namespace Core.Shared.Documentos;

/// <summary>
/// Identificación del establecimiento que emite la documentación. Lo define <c>Core</c> y lo
/// resuelve la infraestructura desde configuración, igual que <see cref="IGeneradorDocumentos"/>.
/// </summary>
/// <remarks>
/// Todo documento oficial tiene que decir qué establecimiento lo emite: nombre, domicilio y CUE
/// son datos obligatorios del encabezado, no decoración. Estaban escritos a mano dentro del
/// cuerpo del certificado de alumno regular —"Instituto Maria Saleme"—, de modo que cambiar de
/// establecimiento exigía editar el texto de cada documento. Acá hay una sola fuente.
/// </remarks>
public interface IDatosInstitucionales
{
	/// <summary>Nombre oficial del centro educativo.</summary>
	string Nombre { get; }

	/// <summary>Clave Única de Establecimiento.</summary>
	string CUE { get; }

	/// <summary>Clave Única de Establecimiento – anexo.</summary>
	string CueAnexo { get; }

	string Direccion { get; }

	string Localidad { get; }

	string CodigoPostal { get; }

	string Departamento { get; }

	/// <summary>Jurisdicción provincial, que es también el lugar de expedición.</summary>
	string Jurisdiccion { get; }

	string Telefono { get; }

	string Sector { get; }

	string Ambito { get; }

	string Modalidad { get; }

	string NivelesEnsenanza { get; }
}
