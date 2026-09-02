using Domain.Curriculas.Materias.CargosDocentes;
using Domain.Curriculas.Materias;
using Domain.Docentes;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Domain.Shared;

namespace Infrastructure.EntityConfigurations;

internal class SituacionRevistaConfiguration : IEntityTypeConfiguration<SituacionRevista>
{
	public void Configure(EntityTypeBuilder<SituacionRevista> builder)
	{
		ConfigureTableSituacionRevista(builder);
	}

	private void ConfigureTableSituacionRevista(EntityTypeBuilder<SituacionRevista> builder)
	{
		builder.ToTable("situacion_revista");

		builder.Property(x => x.Id)
			   .HasColumnName("situacion_revista_id")
			   .ValueGeneratedNever();

		// PK_SITUACIÓN-REVISTA
		builder.HasKey(x => x.Id)
			   .HasName("PK_SITUACION-REVISTA");

		builder.Property(x => x.MateriaID)
			   .HasColumnName("materia_id");

		// FK_MATERIAS_SITUACION-REVISTA
		builder.HasOne<Materia>()
			   .WithMany(x => x.Docentes)
			   .HasPrincipalKey(x => x.Id)
			   .HasForeignKey(x => x.MateriaID)
			   .HasConstraintName("FK_MATERIA_SITUACION-REVISTA");

		builder.Property(x => x.DocenteID)
			   .HasColumnName("docente_id");

		// FK_PROFESORES_SITUACION-REVISTA
		builder.HasOne<Docente>()
			   .WithMany()
			   .HasForeignKey(x => x.DocenteID)
			   .HasConstraintName("FK_DOCENTE_SITUACION-REVISTA");

		builder.Property(x => x.Estado)
			   .HasColumnName("estado")
			   .HasColumnType("varchar(20)")
			   .HasConversion(toProvider => toProvider.Descripcion,
							  fromProvider => Enumeration.FromDescripcion<EstadoSituacionRevista>(fromProvider.ToString()));

		builder.Property(x => x.Cargo)
			   .HasColumnName("cargo")
			   .HasColumnType("varchar(10)")
			   .HasConversion(toProvider => toProvider.ToString(),
							  fromProvider => (Cargo) Enum.Parse(typeof(Cargo), fromProvider));

		builder.OwnsOne(x => x.Periodo, static builder =>
		{
			builder.Property(x => x.FechaInicio)
				   .HasColumnName("fecha_inicio")
				   .HasColumnType("date");

			builder.Property(x => x.FechaFin)
				   .HasColumnName("fecha_fin")
				   .HasColumnType("date")
				   .IsRequired(false);
		});

		builder.Property(x => x.EnFunciones)
			   .HasColumnName("en_funciones")
			   .HasColumnType("bit")
			   .HasDefaultValue(false);
	}
}