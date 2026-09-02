namespace Core.Shared.Documentos;

public record CertificadoAlumnoRegularRequest(string NombreCompleto,
											  string DNI,
											  DateTime FechaIngreso,
											  string Grado,
											  string NivelEducativo);
