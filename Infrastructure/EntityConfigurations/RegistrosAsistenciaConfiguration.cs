using Domain.Asistencias;
using Domain.Cursantes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal class RegistrosAsistenciaConfiguration : IEntityTypeConfiguration<RegistroAsistencia>
{
	public void Configure(EntityTypeBuilder<RegistroAsistencia> builder)
	{
		builder.ToTable("registro_asistencia");

		builder.HasKey(x => x.Id)
			   .HasName("PK_REGISTRO_ASISTENCIA");

		builder.Property(x => x.Id)
			   .HasColumnName("registro_asistencia_id")
			   .ValueGeneratedNever();

		// FK sombra hacia la raíz PlanillaAsistencia (declarada desde PlanillasAsistenciaConfiguration)
		builder.Property<Guid>("PlanillaAsistenciaID")
			   .HasColumnName("planilla_asistencia_id")
			   .IsRequired();

		builder.Property(x => x.CursanteID)
			   .HasColumnName("cursante_id")
			   .IsRequired();

		// FK_REGISTRO_ASISTENCIA_CURSANTE
		builder.HasOne<Cursante>()
			   .WithMany()
			   .HasForeignKey(x => x.CursanteID)
			   .HasConstraintName("FK_REGISTRO_ASISTENCIA_CURSANTE")
			   .OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.Tipo)
			   .HasColumnName("tipo")
			   .HasColumnType("varchar(15)")
			   .HasConversion(toProvider => toProvider.ToString(),
							  fromProvider => (TipoAsistencia) Enum.Parse(typeof(TipoAsistencia), fromProvider))
			   .IsRequired();

		builder.Property(x => x.Minutos)
			   .HasColumnName("minutos")
			   .HasColumnType("time")
			   .IsRequired(false);

		builder.Property(x => x.Observacion)
			   .HasColumnName("observacion")
			   .HasMaxLength(140)
			   .IsUnicode(false)
			   .IsRequired(false);

		// Un registro por cursante y planilla
		builder.HasIndex("PlanillaAsistenciaID", nameof(RegistroAsistencia.CursanteID))
			   .IsUnique();
	}
}
