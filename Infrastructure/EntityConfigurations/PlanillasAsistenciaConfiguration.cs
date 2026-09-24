using Domain.Asistencias;
using Domain.Divisiones;
using Domain.Docentes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal class PlanillasAsistenciaConfiguration : IEntityTypeConfiguration<PlanillaAsistencia>
{
	public void Configure(EntityTypeBuilder<PlanillaAsistencia> builder)
	{
		builder.ToTable("planilla_asistencia");

		builder.HasKey(x => x.Id)
			   .HasName("PK_PLANILLA_ASISTENCIA");

		builder.Property(x => x.Id)
			   .HasColumnName("planilla_asistencia_id")
			   .ValueGeneratedNever();

		builder.Property(x => x.DivisionID)
			   .HasColumnName("division_id")
			   .IsRequired();

		// FK_PLANILLA_ASISTENCIA_DIVISION
		builder.HasOne<Division>()
			   .WithMany()
			   .HasForeignKey(x => x.DivisionID)
			   .HasConstraintName("FK_PLANILLA_ASISTENCIA_DIVISION")
			   .OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.PreceptorID)
			   .HasColumnName("preceptor_id")
			   .IsRequired();

		// FK_PLANILLA_ASISTENCIA_DOCENTE
		builder.HasOne<Docente>()
			   .WithMany()
			   .HasForeignKey(x => x.PreceptorID)
			   .HasConstraintName("FK_PLANILLA_ASISTENCIA_DOCENTE")
			   .OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.Fecha)
			   .HasColumnName("fecha")
			   .HasColumnType("date")
			   .IsRequired();

		builder.Property(x => x.Cerrada)
			   .HasColumnName("cerrada")
			   .HasColumnType("bit")
			   .IsRequired();

		// Una planilla por división y fecha
		builder.HasIndex(x => new { x.DivisionID, x.Fecha })
			   .IsUnique();

		// FK_REGISTRO_ASISTENCIA_PLANILLA_ASISTENCIA (FK sombra "PlanillaAsistenciaID",
		// ver RegistrosAsistenciaConfiguration)
		builder.HasMany(x => x.Registros)
			   .WithOne()
			   .HasForeignKey("PlanillaAsistenciaID")
			   .HasConstraintName("FK_REGISTRO_ASISTENCIA_PLANILLA_ASISTENCIA")
			   .OnDelete(DeleteBehavior.Cascade);

		builder.Navigation(x => x.Registros)
			   .UsePropertyAccessMode(PropertyAccessMode.Field);
	}
}
