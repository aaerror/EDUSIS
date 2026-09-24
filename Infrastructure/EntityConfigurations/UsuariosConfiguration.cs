using Domain.Docentes;
using Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal class UsuariosConfiguration : IEntityTypeConfiguration<Usuario>
{
	public void Configure(EntityTypeBuilder<Usuario> builder)
	{
		ConfigureTableUsuarios(builder);
		//ConfigureTableAccesos(builder);
	}

	private void ConfigureTableUsuarios(EntityTypeBuilder<Usuario> builder)
	{
		builder.ToTable("usuario");

		builder.HasKey(x => x.Id)
			   .HasName("PK_USUARIO");

		builder.Property(x => x.Id)
			   .HasColumnName("usuario_id")
			   .ValueGeneratedNever();

		builder.Property(x => x.DocenteID)
			   .HasColumnName("docente_id")
			   .IsRequired();

		// FK_USUARIO_DOCENTE
		builder.HasOne<Docente>()
			   .WithOne()
			   .HasForeignKey<Usuario>(x => x.DocenteID)
			   .HasConstraintName("FK_USUARIO_DOCENTE")
			   .IsRequired()
			   .OnDelete(DeleteBehavior.Restrict);
/*
		builder.HasOne<Docente>()
			   .WithOne()
			   .HasPrincipalKey<Usuario>(x => x.DocenteID)
			   .HasForeignKey<Docente>(x => x.Id)
			   .HasConstraintName("FK_DOCENTE_USUARIO");*/

		builder.Property(x => x.Username)
			   .HasColumnName("usuario")
			   .HasMaxLength(18)
			   .IsUnicode(false)
			   .IsRequired();

		builder.Property(x => x.PasswordSalt)
			   .HasColumnName("password_salt")
			   .HasMaxLength(256)
			   .IsUnicode(false)
			   .IsRequired();

		builder.Property(x => x.PasswordHash)
			   .HasColumnName("password_hash")
			   .HasMaxLength(256)
			   .IsUnicode(false)
			   .IsRequired();

		builder.HasMany(x => x.Roles)
			   .WithMany()
			   .UsingEntity(
					right => right.HasOne(typeof(Rol))
								  .WithMany()
								  .HasForeignKey("RolesId")
								  .HasConstraintName("FK_USUARIO_ROL_ROL"),
					left => left.HasOne(typeof(Usuario))
								.WithMany()
								.HasForeignKey("UsuarioId")
								.HasConstraintName("FK_USUARIO_ROL_USUARIO"),
					manyToMany =>
					{
						manyToMany.ToTable("usuario_rol");

						manyToMany.Property("RolesId")
								  .HasColumnName("rol_id");

						manyToMany.Property("UsuarioId")
								  .HasColumnName("usuario_id");

						manyToMany.HasKey("RolesId", "UsuarioId")
								  .HasName("PK_USUARIO_ROL");
					})
			   .UsePropertyAccessMode(PropertyAccessMode.Field);
		/*
		builder.Property(x => x.Rol)
			   .HasColumnName("rol")
			   .HasColumnType("varchar(15)")
			   .HasConversion(toProvider => toProvider.ToString(),
							  fromProvider => (Rol) Enum.Parse(typeof(Rol), fromProvider));
		*/

		builder.HasIndex(x => x.DocenteID)
			   .IsUnique();

		/*builder.Metadata.FindNavigation(nameof(Usuario.Roles))
						.SetPropertyAccessMode(PropertyAccessMode.Field);*/
	}

	/*
	private void ConfigureTableAccesos(EntityTypeBuilder<Usuario> builder)
	{
		builder.OwnsMany(x => x.Accesos, accesosBuilder =>
		{
			accesosBuilder.ToTable("acceso");

			accesosBuilder.Property<int>("acceso_id")
						  .UseIdentityColumn();

			accesosBuilder.WithOwner()
						  .HasForeignKey("usuario_id")
						  .HasConstraintName("FK_USUARIO_PERMISO");

			accesosBuilder.HasKey("usuario_id", "acceso_id")
						  .HasName("PK_ACCESO");

			accesosBuilder.Property(x => x.FechaAlta)
						  .HasColumnName("fecha_alta")
						  .HasColumnType("date");

			accesosBuilder.Property(x => x.FechaBaja)
						  .HasColumnName("fecha_baja")
						  .HasColumnType("date")
						  .IsRequired(false);

			accesosBuilder.Property(x => x.Permiso)
						  .HasColumnName("permiso")
						  .HasColumnType("varchar(15)")
						  .HasConversion(toProvider => toProvider.ToString(),
										 fromProvider => (Rol) Enum.Parse(typeof(Rol), fromProvider));
		});
	}
	*/
}