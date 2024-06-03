﻿using Domain.Alumnos;

namespace Infrastructure.Repository;

public class AlumnoRepository : Repository<Alumno>, IAlumnoRepository
{
    private EdusisDBContext _context => Context as EdusisDBContext;


    public AlumnoRepository(EdusisDBContext context)
        : base(context) { }

    public bool EsDocumentoInvalido(string documento) => _context.Alumnos.Any(x => x.InformacionPersonal.Documento == documento);

    public bool ExisteID(Guid id) => _context.Alumnos.Any(x => x.Id == id);
}