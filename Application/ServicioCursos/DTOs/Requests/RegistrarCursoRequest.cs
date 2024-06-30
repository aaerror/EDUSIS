using Domain.Cursos;

namespace Core.ServicioCursos.DTOs.Requests;

public record RegistrarCursoRequest(int Grado, int NivelEducativo);
