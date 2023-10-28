using Core.ServicioProfesores.DTOs.Responses;
using Infrastructure.Shared;

namespace Core.ServicioProfesores;

public class ServicioProfesores : IServicioProfesores
{
    private readonly IUnitOfWork _unitOfWork;


    public ServicioProfesores(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public ProfesorResponse BuscarProfesorPorDNI(string documento)
    {
        try
        {
            var profesor = _unitOfWork.Profesores.Buscar(x => x.InformacionPersonal.Documento == documento && x.EstaActivo == true).FirstOrDefault();
            if (profesor is null)
            {
                throw new NullReferenceException($"No se encontró el profesor con el D.N.I. { documento }");
            }

            return new ProfesorResponse(profesor.Id,
                                        profesor.InformacionPersonal.NombreCompleto(),
                                        profesor.InformacionPersonal.Documento,
                                        profesor.Legajo);
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
