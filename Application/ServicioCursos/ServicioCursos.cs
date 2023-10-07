using Core.ServicioCursos.DTOs.Requests;
using Domain.Cursos;
using Infrastructure.Shared;

namespace Core.ServicioCursos;

public class ServicioCursos
{
    private readonly IUnitOfWork _unityOfWork;

    public ServicioCursos(IUnitOfWork unitOfWork)
    {
        _unityOfWork = unitOfWork;
    }

    private Curso BuscarCursoPorId(Guid curso)
    {
        var cursoBuscado = _unityOfWork.Cursos.BuscarPorID(curso);
        if (cursoBuscado == null)
        {
            throw new NullReferenceException($"No se encontró el curso buscado: { cursoBuscado }");
        }

        return cursoBuscado;
    }

    public void RegistrarCursoPrimario(char descripcion)
    {
        try
        {
            Curso primario = new(descripcion, Formacion.Primaria);
            _unityOfWork.Cursos.Agregar(primario);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void RegistrarCursoSecundario(char descripcion)
    {
        try
        {
            Curso secundario = new(descripcion, Formacion.Secundaria);
            _unityOfWork.Cursos.Agregar(secundario);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    public void AgregarMateria(Guid cursoId, MateriaRequest request)
    {
        try
        {
            var curso = BuscarCursoPorId(cursoId);
            curso.AgregarMateria(request.Descripcion, request.HorasCatedra);

            _unityOfWork.Cursos.ActualizarDatos(curso);
        }
        catch(Exception ex)
        {
            throw;
        }
    }
}
