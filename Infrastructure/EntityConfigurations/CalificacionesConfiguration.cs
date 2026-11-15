using Domain.Cursantes.Calificaciones;
using Domain.Materias;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal class CalificacionesConfiguration : IEntityTypeConfiguration<Calificacion>
{
	public void Configure(EntityTypeBuilder<Calificacion> builder)
	{
		builder.ToTable("calificacion");

		builder.HasKey(x => x.Id)
			   .HasName("PK_CALIFICACION");

		builder.Property(x => x.Id)
			   .HasColumnName("calificacion_id")
			   .ValueGeneratedNever();

		// FK sombra hacia la raíz Cursante (declarada desde CursantesConfiguration)
		builder.Property<Guid>("CursanteID")
			   .HasColumnName("cursante_id")
			   .IsRequired();

		builder.Property(x => x.MateriaID)
			   .HasColumnName("materia_id")
			   .IsRequired();

		// FK_CALIFICACION_MATERIA
		builder.HasOne<Materia>()
			   .WithMany()
			   .HasForeignKey(x => x.MateriaID)
			   .HasConstraintName("FK_CALIFICACION_MATERIA")
			   .OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.Fecha)
			   .HasColumnName("fecha")
			   .HasColumnType("date")
			   .IsRequired();

		builder.Property(x => x.Periodo)
			   .HasColumnName("periodo")
			   .HasColumnType("varchar(30)")
			   .HasConversion(toProvider => toProvider.ToString(),
							  fromProvider => (Periodo) Enum.Parse(typeof(Periodo), fromProvider))
			   .IsRequired();

		// Segundo intento DENTRO del período, no un período aparte: el ciclo admite hasta cuatro.
		builder.Property(x => x.EsRecuperatorio)
			   .HasColumnName("es_recuperatorio")
			   .HasColumnType("bit")
			   .IsRequired();

		builder.Property(x => x.Rindio)
			   .HasColumnName("rindio")
			   .HasColumnType("bit")
			   .IsRequired();

		builder.Property(x => x.Nota)
			   .HasColumnName("nota")
			   .HasColumnType("float(24)")
			   .IsRequired(false);

		builder.Property(x => x.Observacion)
			   .HasColumnName("observacion")
			   .HasMaxLength(140)
			   .IsUnicode(false)
			   .IsRequired(false);
	}
}
