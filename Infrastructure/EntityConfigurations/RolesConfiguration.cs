using Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal class RolesConfiguration : IEntityTypeConfiguration<Rol>
{
	public void Configure(EntityTypeBuilder<Rol> builder)
	{
		builder.ToTable("rol");

		builder.HasKey(x => x.Id)
			   .HasName("PK_ROL");

		builder.Property(x => x.Id)
			   .HasColumnName("rol_id")
			   .ValueGeneratedNever();

		builder.Property(x => x.Descripcion)
			   .HasColumnName("descripcion")
			   .HasMaxLength(15)
			   .IsUnicode(false);

		// La atribución rol → permisos es código, no datos: Permiso no tiene tabla. Sin este
		// Ignore, EF alcanza Permiso desde la navegación, lo descubre como entidad y reclama
		// un mapeo para él.
		builder.Ignore(x => x.Permisos);

		// Datos de referencia, no datos de demo: usuario_rol tiene FK contra rol, así que estas
		// cuatro filas son parte del esquema. Mismo criterio que el catálogo de ciclo_lectivo
		// (ver CicloLectivoConfiguration).
		builder.HasData(Rol.Admin, Rol.Direccion, Rol.Docente, Rol.Secretaria);
	}
}