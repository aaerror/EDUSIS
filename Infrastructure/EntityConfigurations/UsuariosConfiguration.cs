using Domain.Usuarios;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.EntityConfigurations;

internal class UsuariosConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
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

        builder.Property(x => x.Username)
               .HasColumnName("usuario")
               .HasColumnType("varchar(18)")
               .IsRequired();

        builder.Property(x => x.PasswordSalt)
               .HasColumnName("password_salt")
               .HasColumnType("varchar(256)")
               .IsRequired();

        builder.Property(x => x.PasswordHash)
               .HasColumnName("password_hash")
               .HasColumnType("varchar(256)")
               .IsRequired();

        builder.Property(x => x.Rol)
               .HasColumnName("rol")
               .HasColumnType("varchar(15)")
               .HasConversion(toProvider => toProvider.ToString(),
                              fromProvider => (Rol) Enum.Parse(typeof(Rol), fromProvider));

        builder.OwnsMany(x => x.Accesos, accesosBuilder =>
        {
            accesosBuilder.ToTable("acceso");

            accesosBuilder.Property<int>("acceso_id")
                          .UseIdentityColumn();

            accesosBuilder.WithOwner()
                          .HasForeignKey("usuario_id")
                          .HasConstraintName("FK_USUARIO_PERMISO");

            accesosBuilder.HasKey("acceso_id", "usuario_id")
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
                                         fromProvider => (Permiso)Enum.Parse(typeof(Permiso), fromProvider));
        });

        builder.Metadata.FindNavigation(nameof(Usuario.Accesos))
                        .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}