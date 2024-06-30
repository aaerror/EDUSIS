﻿// <auto-generated />
using System;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(EdusisDBContext))]
    partial class EdusisDBContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Domain.Cursos.Curso", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("curso_id");

                    b.Property<string>("Grado")
                        .IsRequired()
                        .HasColumnType("varchar(10)")
                        .HasColumnName("grado");

                    b.Property<string>("NivelEducativo")
                        .IsRequired()
                        .HasColumnType("varchar(15)")
                        .HasColumnName("nivel_educativo");

                    b.HasKey("Id")
                        .HasName("PK_CURSO");

                    b.ToTable("curso", (string)null);
                });

            modelBuilder.Entity("Domain.Cursos.Divisiones.Cursantes.CicloLectivo", b =>
                {
                    b.Property<int>("ciclo_lectivo_id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ciclo_lectivo_id"));

                    b.Property<int>("Periodo")
                        .HasMaxLength(4)
                        .HasColumnType("smallint")
                        .HasColumnName("periodo");

                    b.HasKey("ciclo_lectivo_id")
                        .HasName("PK_CICLO-LECTIVO");

                    b.ToTable("ciclo_lectivo", (string)null);
                });

            modelBuilder.Entity("Domain.Materias.Materia", b =>
                {
                    b.Property<Guid>("CursoID")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("curso_id");

                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("materia_id");

                    b.Property<string>("Descripcion")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("descripcion");

                    b.Property<byte>("HorasCatedra")
                        .HasColumnType("tinyint")
                        .HasColumnName("horas_catedra");

                    b.HasKey("CursoID", "Id")
                        .HasName("PK_MATERIA");

                    b.ToTable("materia", (string)null);
                });

            modelBuilder.Entity("Domain.Personas.Persona", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("persona_id");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(50)")
                        .HasColumnName("email");

                    b.Property<string>("Telefono")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(15)")
                        .HasColumnName("telefono");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.ToTable("persona", (string)null);

                    b.UseTptMappingStrategy();
                });

            modelBuilder.Entity("Domain.Usuarios.Usuario", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("usuario_id");

                    b.Property<Guid>("DocenteID")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("docente_id");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(256)")
                        .HasColumnName("password_hash");

                    b.Property<string>("PasswordSalt")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(256)")
                        .HasColumnName("password_salt");

                    b.Property<string>("Rol")
                        .IsRequired()
                        .HasColumnType("varchar(15)")
                        .HasColumnName("rol");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(18)")
                        .HasColumnName("usuario");

                    b.HasKey("Id")
                        .HasName("PK_USUARIO");

                    b.HasIndex("DocenteID")
                        .IsUnique();

                    b.ToTable("usuario", (string)null);
                });

            modelBuilder.Entity("Domain.Alumnos.Alumno", b =>
                {
                    b.HasBaseType("Domain.Personas.Persona");

                    b.Property<DateTime>("FechaAlta")
                        .HasColumnType("date")
                        .HasColumnName("fecha_alta");

                    b.Property<DateTime?>("FechaBaja")
                        .HasColumnType("date")
                        .HasColumnName("fecha_baja");

                    b.Property<string>("Legajo")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("legajo");

                    b.ToTable("alumno", (string)null);
                });

            modelBuilder.Entity("Domain.Docentes.Docente", b =>
                {
                    b.HasBaseType("Domain.Personas.Persona");

                    b.Property<string>("CUIL")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(11)")
                        .HasColumnName("cuil");

                    b.Property<DateTime>("FechaAlta")
                        .HasColumnType("date")
                        .HasColumnName("fecha_alta");

                    b.Property<DateTime?>("FechaBaja")
                        .HasColumnType("date")
                        .HasColumnName("fecha_baja");

                    b.Property<string>("Legajo")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(6)")
                        .HasColumnName("legajo");

                    b.ToTable("docente", (string)null);
                });

            modelBuilder.Entity("Domain.Cursos.Curso", b =>
                {
                    b.OwnsMany("Domain.Cursos.Divisiones.Division", "Divisiones", b1 =>
                        {
                            b1.Property<Guid>("curso_id")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<Guid>("Id")
                                .HasColumnType("uniqueidentifier")
                                .HasColumnName("division_id");

                            b1.Property<string>("Descripcion")
                                .IsRequired()
                                .HasMaxLength(50)
                                .HasColumnType("char(1)")
                                .HasColumnName("division");

                            b1.Property<Guid?>("Preceptor")
                                .HasColumnType("uniqueidentifier")
                                .HasColumnName("docente_id");

                            b1.HasKey("curso_id", "Id")
                                .HasName("PK_DIVISION");

                            b1.HasIndex("Preceptor");

                            b1.ToTable("division", (string)null);

                            b1.HasOne("Domain.Docentes.Docente", null)
                                .WithMany()
                                .HasForeignKey("Preceptor")
                                .HasConstraintName("FK_PRECEPTOR_DIVISION");

                            b1.WithOwner()
                                .HasForeignKey("curso_id")
                                .HasConstraintName("FK_CURSO_DIVISION");

                            b1.OwnsMany("Domain.Cursos.Divisiones.Cursantes.Cursante", "Cursantes", b2 =>
                                {
                                    b2.Property<Guid>("curso_id")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<Guid>("division_id")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<Guid>("Id")
                                        .HasColumnType("uniqueidentifier")
                                        .HasColumnName("cursante_id");

                                    b2.Property<Guid>("AlumnoID")
                                        .HasColumnType("uniqueidentifier")
                                        .HasColumnName("alumno_id");

                                    b2.Property<int>("ciclo_lectivo_id")
                                        .HasColumnType("int");

                                    b2.HasKey("curso_id", "division_id", "Id")
                                        .HasName("PK_CURSANTE");

                                    b2.HasIndex("AlumnoID")
                                        .IsUnique();

                                    b2.HasIndex("ciclo_lectivo_id");

                                    b2.ToTable("cursante", (string)null);

                                    b2.HasOne("Domain.Alumnos.Alumno", null)
                                        .WithOne()
                                        .HasForeignKey("Domain.Cursos.Curso.Divisiones#Domain.Cursos.Divisiones.Division.Cursantes#Domain.Cursos.Divisiones.Cursantes.Cursante", "AlumnoID")
                                        .OnDelete(DeleteBehavior.Cascade)
                                        .IsRequired()
                                        .HasConstraintName("FK_ALUMNO_CURSANTE");

                                    b2.HasOne("Domain.Cursos.Divisiones.Cursantes.CicloLectivo", "CicloLectivo")
                                        .WithMany()
                                        .HasForeignKey("ciclo_lectivo_id")
                                        .OnDelete(DeleteBehavior.Cascade)
                                        .IsRequired()
                                        .HasConstraintName("FK_CICLO-LECTIVO_CURSANTE");

                                    b2.WithOwner()
                                        .HasForeignKey("curso_id", "division_id")
                                        .HasConstraintName("FK_DIVISION_CURSANTE");

                                    b2.OwnsMany("Domain.Cursos.Divisiones.Cursantes.Calificacion", "Calificaciones", b3 =>
                                        {
                                            b3.Property<Guid>("curso_id")
                                                .HasColumnType("uniqueidentifier");

                                            b3.Property<Guid>("division_id")
                                                .HasColumnType("uniqueidentifier");

                                            b3.Property<Guid>("cursante_id")
                                                .HasColumnType("uniqueidentifier");

                                            b3.Property<int>("calificacion_id")
                                                .ValueGeneratedOnAdd()
                                                .HasColumnType("int");

                                            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b3.Property<int>("calificacion_id"));

                                            b3.Property<Guid>("CursoID")
                                                .HasColumnType("uniqueidentifier")
                                                .HasColumnName("materia_curso_id");

                                            b3.Property<DateTime?>("Fecha")
                                                .HasColumnType("date")
                                                .HasColumnName("fecha");

                                            b3.Property<string>("Instancia")
                                                .IsRequired()
                                                .HasColumnType("varchar(15)")
                                                .HasColumnName("instancia");

                                            b3.Property<Guid>("MateriaID")
                                                .HasColumnType("uniqueidentifier")
                                                .HasColumnName("materia_materia_id");

                                            b3.Property<decimal?>("Nota")
                                                .HasColumnType("decimal(5,2)")
                                                .HasColumnName("nota");

                                            b3.Property<bool>("_asistencia")
                                                .HasColumnType("bit")
                                                .HasColumnName("asistencia");

                                            b3.HasKey("curso_id", "division_id", "cursante_id", "calificacion_id");

                                            b3.HasIndex("CursoID", "MateriaID");

                                            b3.ToTable("calificacion", (string)null);

                                            b3.HasOne("Domain.Materias.Materia", null)
                                                .WithMany()
                                                .HasForeignKey("CursoID", "MateriaID")
                                                .OnDelete(DeleteBehavior.NoAction)
                                                .IsRequired()
                                                .HasConstraintName("FK_MATERIA_CALIFICACION");

                                            b3.WithOwner()
                                                .HasForeignKey("curso_id", "division_id", "cursante_id")
                                                .HasConstraintName("FK_CURSANTE_CALIFICACION");
                                        });

                                    b2.Navigation("Calificaciones");

                                    b2.Navigation("CicloLectivo");
                                });

                            b1.Navigation("Cursantes");
                        });

                    b.Navigation("Divisiones");
                });

            modelBuilder.Entity("Domain.Materias.Materia", b =>
                {
                    b.HasOne("Domain.Cursos.Curso", null)
                        .WithMany()
                        .HasForeignKey("CursoID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_CURSO_MATERIA");

                    b.OwnsMany("Domain.Materias.CargosDocentes.SituacionRevista", "Docentes", b1 =>
                        {
                            b1.Property<Guid>("curso_id")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<Guid>("materia_id")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<int>("situacion_revista_id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int");

                            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b1.Property<int>("situacion_revista_id"));

                            b1.Property<string>("Cargo")
                                .IsRequired()
                                .HasColumnType("varchar(10)")
                                .HasColumnName("cargo");

                            b1.Property<Guid>("DocenteID")
                                .HasColumnType("uniqueidentifier")
                                .HasColumnName("docente_id");

                            b1.Property<bool>("EnFunciones")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("bit")
                                .HasDefaultValue(false)
                                .HasColumnName("en_funciones");

                            b1.Property<DateTime>("FechaAlta")
                                .HasColumnType("date")
                                .HasColumnName("fecha_alta");

                            b1.Property<DateTime?>("FechaBaja")
                                .HasColumnType("date")
                                .HasColumnName("fecha_baja");

                            b1.HasKey("curso_id", "materia_id", "situacion_revista_id")
                                .HasName("PK_SITUACION-REVISTA");

                            b1.HasIndex("DocenteID");

                            b1.ToTable("situacion_revista", (string)null);

                            b1.HasOne("Domain.Docentes.Docente", null)
                                .WithMany()
                                .HasForeignKey("DocenteID")
                                .OnDelete(DeleteBehavior.Cascade)
                                .IsRequired()
                                .HasConstraintName("FK_DOCENTE_SITUACION-REVISTA");

                            b1.WithOwner()
                                .HasForeignKey("curso_id", "materia_id")
                                .HasConstraintName("FK_MATERIA_SITUACION-REVISTA");
                        });

                    b.OwnsMany("Domain.Materias.Horarios.Horario", "Horarios", b1 =>
                        {
                            b1.Property<Guid>("curso_id")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<Guid>("materia_id")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<int>("horario_id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int");

                            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b1.Property<int>("horario_id"));

                            b1.Property<string>("DiaSemana")
                                .IsRequired()
                                .HasColumnType("varchar(10)")
                                .HasColumnName("dia");

                            b1.Property<TimeSpan>("HoraFin")
                                .HasColumnType("time(0)")
                                .HasColumnName("hora_fin");

                            b1.Property<TimeSpan>("HoraInicio")
                                .HasColumnType("time(0)")
                                .HasColumnName("hora_inicio");

                            b1.Property<string>("Turno")
                                .IsRequired()
                                .HasColumnType("varchar(10)")
                                .HasColumnName("turno");

                            b1.HasKey("curso_id", "materia_id", "horario_id")
                                .HasName("PK_HORARIO");

                            b1.ToTable("horario", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("curso_id", "materia_id")
                                .HasConstraintName("FK_MATERIA_HORARIO");
                        });

                    b.Navigation("Docentes");

                    b.Navigation("Horarios");
                });

            modelBuilder.Entity("Domain.Personas.Persona", b =>
                {
                    b.OwnsOne("Domain.Personas.InformacionPersonal", "InformacionPersonal", b1 =>
                        {
                            b1.Property<Guid>("PersonaId")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Apellido")
                                .IsRequired()
                                .HasMaxLength(50)
                                .HasColumnType("varchar(50)")
                                .HasColumnName("apellido");

                            b1.Property<int>("Documento")
                                .HasMaxLength(50)
                                .HasColumnType("int")
                                .HasColumnName("documento");

                            b1.Property<DateTime>("FechaNacimiento")
                                .HasColumnType("date")
                                .HasColumnName("fecha_nacimiento");

                            b1.Property<string>("Nacionalidad")
                                .IsRequired()
                                .HasMaxLength(50)
                                .HasColumnType("varchar(20)")
                                .HasColumnName("nacionalidad");

                            b1.Property<string>("Nombre")
                                .IsRequired()
                                .HasMaxLength(50)
                                .HasColumnType("varchar(50)")
                                .HasColumnName("nombre");

                            b1.Property<string>("Sexo")
                                .IsRequired()
                                .HasColumnType("varchar(15)")
                                .HasColumnName("sexo");

                            b1.HasKey("PersonaId");

                            b1.HasIndex("Documento")
                                .IsUnique();

                            b1.ToTable("persona");

                            b1.WithOwner()
                                .HasForeignKey("PersonaId");
                        });

                    b.OwnsOne("Domain.Personas.Domicilios.Domicilio", "Domicilio", b1 =>
                        {
                            b1.Property<Guid>("persona_id")
                                .HasColumnType("uniqueidentifier");

                            b1.HasKey("persona_id")
                                .HasName("PK_DOMICILIO");

                            b1.ToTable("domicilio", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("persona_id")
                                .HasConstraintName("FK_PERSONA_DOMICILIO");

                            b1.OwnsOne("Domain.Personas.Domicilios.Direccion", "Direccion", b2 =>
                                {
                                    b2.Property<Guid>("Domiciliopersona_id")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<string>("Altura")
                                        .HasMaxLength(50)
                                        .HasColumnType("varchar(6)")
                                        .HasColumnName("altura");

                                    b2.Property<string>("Calle")
                                        .IsRequired()
                                        .HasMaxLength(50)
                                        .HasColumnType("varchar(50)")
                                        .HasColumnName("calle");

                                    b2.Property<string>("Observacion")
                                        .IsRequired()
                                        .HasMaxLength(50)
                                        .HasColumnType("varchar(120)")
                                        .HasColumnName("observaciones");

                                    b2.Property<string>("Vivienda")
                                        .IsRequired()
                                        .HasColumnType("varchar(20)")
                                        .HasColumnName("vivienda");

                                    b2.HasKey("Domiciliopersona_id");

                                    b2.ToTable("domicilio");

                                    b2.WithOwner()
                                        .HasForeignKey("Domiciliopersona_id");
                                });

                            b1.OwnsOne("Domain.Personas.Domicilios.Ubicacion", "Ubicacion", b2 =>
                                {
                                    b2.Property<Guid>("Domiciliopersona_id")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<string>("Localidad")
                                        .IsRequired()
                                        .HasMaxLength(50)
                                        .HasColumnType("varchar(50)")
                                        .HasColumnName("localidad");

                                    b2.Property<string>("Pais")
                                        .IsRequired()
                                        .HasMaxLength(50)
                                        .HasColumnType("varchar(50)")
                                        .HasColumnName("pais");

                                    b2.Property<string>("Provincia")
                                        .IsRequired()
                                        .HasMaxLength(50)
                                        .HasColumnType("varchar(50)")
                                        .HasColumnName("provincia");

                                    b2.HasKey("Domiciliopersona_id");

                                    b2.ToTable("domicilio");

                                    b2.WithOwner()
                                        .HasForeignKey("Domiciliopersona_id");
                                });

                            b1.Navigation("Direccion")
                                .IsRequired();

                            b1.Navigation("Ubicacion")
                                .IsRequired();
                        });

                    b.Navigation("Domicilio")
                        .IsRequired();

                    b.Navigation("InformacionPersonal")
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Usuarios.Usuario", b =>
                {
                    b.HasOne("Domain.Docentes.Docente", null)
                        .WithOne()
                        .HasForeignKey("Domain.Usuarios.Usuario", "DocenteID")
                        .HasConstraintName("FK_USUARIO_DOCENTE");

                    b.OwnsMany("Domain.Usuarios.Acceso", "Accesos", b1 =>
                        {
                            b1.Property<Guid>("usuario_id")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<int>("acceso_id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int");

                            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b1.Property<int>("acceso_id"));

                            b1.Property<DateTime>("FechaAlta")
                                .HasColumnType("date")
                                .HasColumnName("fecha_alta");

                            b1.Property<DateTime?>("FechaBaja")
                                .HasColumnType("date")
                                .HasColumnName("fecha_baja");

                            b1.Property<string>("Permiso")
                                .IsRequired()
                                .HasColumnType("varchar(15)")
                                .HasColumnName("permiso");

                            b1.HasKey("usuario_id", "acceso_id")
                                .HasName("PK_ACCESO");

                            b1.ToTable("acceso", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("usuario_id")
                                .HasConstraintName("FK_USUARIO_PERMISO");
                        });

                    b.Navigation("Accesos");
                });

            modelBuilder.Entity("Domain.Alumnos.Alumno", b =>
                {
                    b.HasOne("Domain.Personas.Persona", null)
                        .WithOne()
                        .HasForeignKey("Domain.Alumnos.Alumno", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Docentes.Docente", b =>
                {
                    b.HasOne("Domain.Personas.Persona", null)
                        .WithOne()
                        .HasForeignKey("Domain.Docentes.Docente", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsMany("Domain.Docentes.Licencias.Licencia", "Licencias", b1 =>
                        {
                            b1.Property<int>("licencia_id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int");

                            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b1.Property<int>("licencia_id"));

                            b1.Property<Guid>("docente_id")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<string>("Articulo")
                                .IsRequired()
                                .HasColumnType("varchar(15)")
                                .HasColumnName("articulo");

                            b1.Property<int>("Dias")
                                .HasColumnType("int")
                                .HasColumnName("dias");

                            b1.Property<string>("Estado")
                                .IsRequired()
                                .HasColumnType("varchar(15)")
                                .HasColumnName("estado");

                            b1.Property<DateTime>("FechaInicio")
                                .HasColumnType("date")
                                .HasColumnName("fecha_inicio");

                            b1.Property<string>("Observacion")
                                .IsRequired()
                                .HasMaxLength(120)
                                .HasColumnType("varchar(10)")
                                .HasColumnName("observacion");

                            b1.HasKey("licencia_id", "docente_id")
                                .HasName("PK_LICENCIA");

                            b1.HasIndex("docente_id");

                            b1.ToTable("licencia", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("docente_id")
                                .HasConstraintName("FK_DOCENTE_LICENCIA");
                        });

                    b.OwnsMany("Domain.Docentes.Puesto", "Puestos", b1 =>
                        {
                            b1.Property<int>("puesto_id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int");

                            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b1.Property<int>("puesto_id"));

                            b1.Property<Guid>("docente_id")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<DateTime?>("FechaFin")
                                .HasColumnType("date")
                                .HasColumnName("fecha_fin");

                            b1.Property<DateTime>("FechaInicio")
                                .HasColumnType("date")
                                .HasColumnName("fecha_inicio");

                            b1.Property<string>("Posicion")
                                .IsRequired()
                                .HasColumnType("varchar(15)")
                                .HasColumnName("posicion");

                            b1.HasKey("puesto_id", "docente_id")
                                .HasName("PK_PUESTO");

                            b1.HasIndex("docente_id");

                            b1.ToTable("puesto", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("docente_id")
                                .HasConstraintName("FK_DOCENTE_PUESTO");
                        });

                    b.Navigation("Licencias");

                    b.Navigation("Puestos");
                });
#pragma warning restore 612, 618
        }
    }
}
