﻿using Domain.Alumno;
using Infrastructure.Shared;

namespace Infrastructure;

public class AlumnoRepository : Repository<Alumno>, IAlumnoRepository
{
    private EdusisDBContext _context { get => Context as EdusisDBContext; }


    public AlumnoRepository(EdusisDBContext context) : base(context) { }

    public bool EsDocumentoValido(string documento) => !_context.Alumnos.Any(x => x.InformacionPersonal.Documento == documento);
}