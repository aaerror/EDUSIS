﻿using Domain.Alumnos;
using Domain.Cursos;
using Domain.Docentes;

namespace Infrastructure.Shared;

public interface IUnitOfWork : IDisposable
{
    IAlumnoRepository Alumnos { get; }

    IDocenteRepository Docentes { get; }

    IProfesorRepository Profesores {  get; }

    ICursoRepository Cursos { get; } 



    void GuardarCambios();
}