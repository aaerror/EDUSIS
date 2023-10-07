using Domain.Docentes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

public class PreceptoresConfigurations : IEntityTypeConfiguration<Preceptor>
{
    public void Configure(EntityTypeBuilder<Preceptor> builder)
    {
        builder.HasBaseType<Docente>();

        builder.ToTable("preceptores");
    }
}
