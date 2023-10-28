using Domain.Docentes;
using Infrastructure.Shared;

namespace Infrastructure.Repository;

internal class DocenteRepository : Repository<Docente>, IDocenteRepository
{
    private EdusisDBContext _context => Context as EdusisDBContext;


    public DocenteRepository(EdusisDBContext context)
        : base(context) { }

    public bool EsDocumentoValido(string documento)
    {
        throw new NotImplementedException();
    }
}
