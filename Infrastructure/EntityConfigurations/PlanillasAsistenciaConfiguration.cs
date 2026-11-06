using Domain.Asistencias;
using Domain.Divisiones;
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

		// NO hay clave ajena a docente, y es deliberado desde el 2026-11-06.
		//
		// Acá vivía FK_PLANILLA_ASISTENCIA_DOCENTE, un HasOne<Docente>().WithMany() sin navegación
		// en Restrict. La planilla referencia al preceptor por PreceptorID, un Guid pelado, que es
		// la regla del repo: "los agregados se referencian sólo por identificador, nunca por
		// navegación directa entre raíces". La clave ajena no la pedía el dominio, la agregaba esta
		// configuración, y ataba dos raíces de agregado en la base.
		//
		// Se quitó para que eliminar un docente sea posible sin destruir la asistencia de divisiones
		// enteras: preceptor_id es la FIRMA de quien confeccionó la planilla, un dato histórico del
		// registro. Borrar el legajo no desfirma la planilla.
		//
		// Se descartó la alternativa de hacer anulable preceptor_id y ponerlo en NULL al borrar:
		// choca con SinDatosPreceptorException, invariante del constructor de PlanillaAsistencia, y
		// una planilla sin firmante no documenta la jornada que vino a documentar. Por eso la columna
		// sigue siendo IsRequired(): lo que se fue es la restricción, no el dato.
		//
		// La migración se llevó también IX_planilla_asistencia_preceptor_id, que existía sólo para
		// servir a esa clave ajena: ninguna consulta filtra planillas por preceptor —PreceptorID se
		// lee de la planilla ya cargada para armar el Response, nada más—. Si alguna vez hace falta
		// buscar las planillas que firmó un docente, el índice hay que volver a declararlo con
		// HasIndex, porque ya no viene de arrastre con la relación.
		//
		// Precio asumido: la base ya no impide un preceptor_id huérfano. Para un registro firmado es
		// la semántica correcta, y la integridad entre raíces de agregado pasa a ser responsabilidad
		// del caso de uso, que es el que conoce la intención. Misma conclusión que CORE-23.

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
