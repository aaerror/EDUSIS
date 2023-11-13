using Domain.Docentes;
using Infrastructure.Shared;

namespace Infrastructure.Repository;

internal class DocenteRepository : Repository<Docente>, IDocenteRepository
{
    private EdusisDBContext _context => Context as EdusisDBContext;


    public DocenteRepository(EdusisDBContext context)
        : base(context) { }

    public bool EsDocumentoInvalido(string documento) => _context.Docentes.Any(x => x.InformacionPersonal.Documento == documento);

    public bool EsCuilInvalido(string cuil) => _context.Docentes.Any(x => x.CUIL == cuil);

    public bool EsLegajoInvalido(string legajo) => _context.Docentes.Any(x => x.Legajo == legajo);

    public IReadOnlyCollection<Puesto> Puestos(Guid docenteID) => _context.Docentes.Where(x => x.Id.Equals(docenteID))
                                                                                   .FirstOrDefault().Puestos;
}
