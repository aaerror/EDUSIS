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
	}
}