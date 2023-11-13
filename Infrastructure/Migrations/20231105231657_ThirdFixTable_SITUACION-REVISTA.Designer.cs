﻿// <auto-generated />
using System;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(EdusisDBContext))]
    [Migration("20231105231657_ThirdFixTable_SITUACION-REVISTA")]
    partial class ThirdFixTable_SITUACIONREVISTA
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
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

                    b.Property<string>("Descripcion")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(15)")
                        .HasColumnName("descripcion");

                    b.Property<string>("NivelEducativo")
                        .IsRequired()
                        .HasColumnType("varchar(15)")
                        .HasColumnName("nivel_educativo");

                    b.HasKey("Id")
                        .HasName("PK_CURSOS");

                    b.ToTable("cursos", (string)null);
                });

            modelBuilder.Entity("Domain.Cursos.Divisiones.CicloLectivo", b =>
                {
                    b.Property<int>("ciclo_lectivo_id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("ciclo_lectivo_id"));

                    b.Property<string>("Periodo")
                        .IsRequired()
                        .HasMaxLength(4)
                        .HasColumnType("varchar")
                        .HasColumnName("periodo");

                    b.HasKey("ciclo_lectivo_id")
                        .HasName("PK_CICLO-LECTIVO");

                    b.ToTable("ciclo_lectivo", (string)null);
                });

            modelBuilder.Entity("Domain.Cursos.Divisiones.Cursante", b =>
                {
                    b.Property<int>("cursante_id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("cursante_id"));

                    b.Property<Guid>("Alumno")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("alumno_id");

                    b.Property<int>("ciclo_lectivo_id")
                        .HasColumnType("int");

                    b.Property<Guid?>("curso_id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid?>("division_id")
                        .HasColumnType("uniqueidentifier");

                    b.HasKey("cursante_id")
                        .HasName("PK_CURSANTES");

                    b.HasIndex("Alumno")
                        .IsUnique();

                    b.HasIndex("ciclo_lectivo_id");

                    b.HasIndex("curso_id", "division_id");

                    b.ToTable("cursantes", (string)null);
                });

            modelBuilder.Entity("Domain.Cursos.Divisiones.Division", b =>
                {
                    b.Property<Guid>("curso_id")
                        .HasColumnType("uniqueidentifier");

                    b.Property<Guid>("Id")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("division_id");

                    b.Property<string>("Descripcion")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(15)")
                        .HasColumnName("descripicion");

                    b.Property<Guid?>("Preceptor")
                        .HasColumnType("uniqueidentifier")
                        .HasColumnName("preceptor_id");

                    b.HasKey("curso_id", "Id")
                        .HasName("PK_DIVISIONES");

                    b.HasIndex("Preceptor")
                        .IsUnique()
                        .HasFilter("[preceptor_id] IS NOT NULL");

                    b.ToTable("divisiones", (string)null);
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

                    b.ToTable("personas", (string)null);

                    b.UseTptMappingStrategy();
                });

            modelBuilder.Entity("Domain.Alumnos.Alumno", b =>
                {
                    b.HasBaseType("Domain.Personas.Persona");

                    b.Property<DateTime>("FechaAlta")
                        .HasColumnType("datetime2")
                        .HasColumnName("fecha_alta");

                    b.Property<DateTime?>("FechaBaja")
                        .HasColumnType("datetime2")
                        .HasColumnName("fecha_baja");

                    b.Property<string>("Legajo")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("legajo");

                    b.ToTable("alumnos", (string)null);
                });

            modelBuilder.Entity("Domain.Docentes.Docente", b =>
                {
                    b.HasBaseType("Domain.Personas.Persona");

                    b.Property<string>("CUIL")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("varchar(11)")
                        .HasColumnName("cuil");

                    b.Property<bool>("EstaActivo")
                        .HasColumnType("bit")
                        .HasColumnName("esta_activo");

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

                    b.ToTable("docentes", (string)null);
                });

            modelBuilder.Entity("Domain.Cursos.Curso", b =>
                {
                    b.OwnsMany("Domain.Cursos.Materias.Materia", "Materias", b1 =>
                        {
                            b1.Property<Guid>("curso_id")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<Guid>("Id")
                                .HasColumnType("uniqueidentifier")
                                .HasColumnName("materia_id");

                            b1.Property<string>("Descripcion")
                                .IsRequired()
                                .HasMaxLength(50)
                                .HasColumnType("varchar(30)")
                                .HasColumnName("descripcion");

                            b1.Property<byte>("HorasCatedra")
                                .HasColumnType("tinyint")
                                .HasColumnName("horas_catedra");

                            b1.HasKey("curso_id", "Id")
                                .HasName("PK_MATERIAS");

                            b1.ToTable("materias", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("curso_id")
                                .HasConstraintName("FK_CURSOS_MATERIAS");

                            b1.OwnsMany("Domain.Cursos.Materias.Horario", "Horarios", b2 =>
                                {
                                    b2.Property<Guid>("curso_id")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<Guid>("materia_id")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<int>("horario_id")
                                        .ValueGeneratedOnAdd()
                                        .HasColumnType("int");

                                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b2.Property<int>("horario_id"));

                                    b2.Property<string>("DiaSemana")
                                        .IsRequired()
                                        .HasColumnType("varchar(10)")
                                        .HasColumnName("dia");

                                    b2.Property<TimeSpan>("HoraFin")
                                        .HasColumnType("time(0)")
                                        .HasColumnName("hora_fin");

                                    b2.Property<TimeSpan>("HoraInicio")
                                        .HasColumnType("time(0)")
                                        .HasColumnName("hora_inicio");

                                    b2.Property<string>("Turno")
                                        .IsRequired()
                                        .HasColumnType("varchar(10)")
                                        .HasColumnName("turno");

                                    b2.HasKey("curso_id", "materia_id", "horario_id")
                                        .HasName("PK_HORARIOS");

                                    b2.ToTable("horarios", (string)null);

                                    b2.WithOwner()
                                        .HasForeignKey("curso_id", "materia_id")
                                        .HasConstraintName("FK_MATERIAS_HORARIOS");
                                });

                            b1.OwnsMany("Domain.Cursos.Materias.SituacionRevista", "Profesores", b2 =>
                                {
                                    b2.Property<Guid>("curso_id")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<Guid>("materia_id")
                                        .HasColumnType("uniqueidentifier");

                                    b2.Property<int>("situacion_revista_id")
                                        .ValueGeneratedOnAdd()
                                        .HasColumnType("int");

                                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b2.Property<int>("situacion_revista_id"));

                                    b2.Property<string>("Cargo")
                                        .IsRequired()
                                        .HasColumnType("varchar(10)")
                                        .HasColumnName("cargo");

                                    b2.Property<bool>("EnFunciones")
                                        .ValueGeneratedOnAdd()
                                        .HasColumnType("bit")
                                        .HasDefaultValue(false)
                                        .HasColumnName("en_funciones");

                                    b2.Property<DateTime>("FechaAlta")
                                        .HasColumnType("datetime2")
                                        .HasColumnName("fecha_alta");

                                    b2.Property<DateTime?>("FechaBaja")
                                        .HasColumnType("datetime2")
                                        .HasColumnName("fecha_baja");

                                    b2.Property<Guid>("ProfesorId")
                                        .HasColumnType("uniqueidentifier")
                                        .HasColumnName("profesor_id");

                                    b2.HasKey("curso_id", "materia_id", "situacion_revista_id")
                                        .HasName("PK_SITUACION-REVISTA");

                                    b2.HasIndex("ProfesorId")
                                        .IsUnique();

                                    b2.ToTable("situacion_revista", (string)null);

                                    b2.HasOne("Domain.Docentes.Docente", null)
                                        .WithOne()
                                        .HasForeignKey("Domain.Cursos.Curso.Materias#Domain.Cursos.Materias.Materia.Profesores#Domain.Cursos.Materias.SituacionRevista", "ProfesorId")
                                        .OnDelete(DeleteBehavior.Cascade)
                                        .IsRequired()
                                        .HasConstraintName("FK_DOCENTES_SITUACION-REVISTA");

                                    b2.WithOwner()
                                        .HasForeignKey("curso_id", "materia_id")
                                        .HasConstraintName("FK_MATERIAS_SITUACION-REVISTA");
                                });

                            b1.Navigation("Horarios");

                            b1.Navigation("Profesores");
                        });

                    b.Navigation("Materias");
                });

            modelBuilder.Entity("Domain.Cursos.Divisiones.Cursante", b =>
                {
                    b.HasOne("Domain.Alumnos.Alumno", null)
                        .WithOne()
                        .HasForeignKey("Domain.Cursos.Divisiones.Cursante", "Alumno")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_ALUMNOS_CURSANTES");

                    b.HasOne("Domain.Cursos.Divisiones.CicloLectivo", "CicloLectivo")
                        .WithMany()
                        .HasForeignKey("ciclo_lectivo_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_CICLO-LECTIVO_CURSANTES");

                    b.HasOne("Domain.Cursos.Divisiones.Division", null)
                        .WithMany("_listadosDefinitivos")
                        .HasForeignKey("curso_id", "division_id")
                        .HasConstraintName("FK_DIVISIONES_CURSANTES");

                    b.Navigation("CicloLectivo");
                });

            modelBuilder.Entity("Domain.Cursos.Divisiones.Division", b =>
                {
                    b.HasOne("Domain.Docentes.Docente", null)
                        .WithOne()
                        .HasForeignKey("Domain.Cursos.Divisiones.Division", "Preceptor")
                        .HasConstraintName("FK_DOCENTES_DIVISIONES");

                    b.HasOne("Domain.Cursos.Curso", null)
                        .WithMany("Divisiones")
                        .HasForeignKey("curso_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired()
                        .HasConstraintName("FK_CURSOS_DIVISIONES");
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
                                .HasColumnType("datetime")
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

                            b1.ToTable("personas");

                            b1.WithOwner()
                                .HasForeignKey("PersonaId");
                        });

                    b.OwnsOne("Domain.Personas.Domicilios.Domicilio", "Domicilio", b1 =>
                        {
                            b1.Property<Guid>("persona_id")
                                .HasColumnType("uniqueidentifier");

                            b1.HasKey("persona_id")
                                .HasName("PK_DOMICILIOS");

                            b1.ToTable("domicilios", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("persona_id")
                                .HasConstraintName("FK_PERSONAS_DOMICILIOS");

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

                                    b2.ToTable("domicilios");

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

                                    b2.ToTable("domicilios");

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

                    b.OwnsMany("Domain.Docentes.Puesto", "Puestos", b1 =>
                        {
                            b1.Property<int>("puesto_id")
                                .ValueGeneratedOnAdd()
                                .HasColumnType("int");

                            SqlServerPropertyBuilderExtensions.UseIdentityColumn(b1.Property<int>("puesto_id"));

                            b1.Property<Guid>("docente_id")
                                .HasColumnType("uniqueidentifier");

                            b1.Property<DateTime?>("FechaFin")
                                .HasColumnType("datetime2")
                                .HasColumnName("fecha_fin");

                            b1.Property<DateTime>("FechaInicio")
                                .HasColumnType("datetime2")
                                .HasColumnName("fecha_inicio");

                            b1.Property<string>("Posicion")
                                .IsRequired()
                                .HasColumnType("varchar(15)")
                                .HasColumnName("posicion");

                            b1.HasKey("puesto_id", "docente_id")
                                .HasName("PK_PUESTOS");

                            b1.HasIndex("docente_id");

                            b1.ToTable("puestos", (string)null);

                            b1.WithOwner()
                                .HasForeignKey("docente_id")
                                .HasConstraintName("FK_DOCENTES_PUESTOS");
                        });

                    b.Navigation("Puestos");
                });

            modelBuilder.Entity("Domain.Cursos.Curso", b =>
                {
                    b.Navigation("Divisiones");
                });

            modelBuilder.Entity("Domain.Cursos.Divisiones.Division", b =>
                {
                    b.Navigation("_listadosDefinitivos");
                });
#pragma warning restore 612, 618
        }
    }
}
