using Domain.Personas;
using Domain.Shared;

namespace Domain.Alumno;

public interface IAlumnoRepository : IRepository<Alumno>, IPersonaRepository<Alumno> { }
