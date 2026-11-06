using Core.ServicioAutenticaciones.DTOs.Responses;
using Core.ServicioDocentes.DTOs.Responses;
using Core.ServicioDocentes;
using Core.ServicioUsuarios.DTOs.Requests;
using Core.ServicioUsuarios;
using Domain.Usuarios;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WPF_Desktop.UnitTests.Dobles;
using WPF_Desktop.ViewModels.Usuarios;
using Xunit;

namespace WPF_Desktop.UnitTests.ViewModels;

/// <summary>
/// <see cref="GestionUsuariosViewModel"/>: la pantalla que cierra UI-46. Cubre el cruce que arma la
/// lista de docentes sin cuenta, las tres acciones y —sobre todo— las condiciones de los
/// <c>CanExecute</c>, que son donde esta pantalla evita carteles de error.
/// </summary>
public class GestionUsuariosViewModelTests
{
	private sealed class Contexto
	{
		public IServicioUsuario ServicioUsuario { get; } = Substitute.For<IServicioUsuario>();
		public IServicioDocente ServicioDocente { get; } = Substitute.For<IServicioDocente>();
		public DialogServiceFalso Dialogos { get; } = new();
		public GestionUsuariosViewModel ViewModel { get; }

		public Contexto()
		{
			DevolverUsuarios();
			DevolverDocentes();

			ViewModel = new GestionUsuariosViewModel(ServicioUsuario, ServicioDocente, Dialogos);
		}

		public void DevolverUsuarios(params UsuarioResponse[] usuarios) =>
			ServicioUsuario.ListarUsuariosAsync()
						   .Returns(Task.FromResult<IReadOnlyCollection<UsuarioResponse>>(usuarios.ToList()));

		public void DevolverDocentes(params LegajoDocenteResponse[] docentes) =>
			ServicioDocente.ListarDocentesActivosAsync()
						   .Returns(Task.FromResult<IReadOnlyCollection<LegajoDocenteResponse>>(docentes.ToList()));
	}

	private static UsuarioResponse CrearUsuario(Guid? docenteID = null, string nombre = "Luna, Juan", params int[] roles) =>
		new(Guid.NewGuid(), docenteID ?? Guid.NewGuid(), "juan.luna", nombre, "Sin puesto docente asignado", roles.ToList());

	private static LegajoDocenteResponse CrearDocente(Guid? docenteID = null, string nombre = "Soto, Ana") =>
		new(docenteID ?? Guid.NewGuid(), nombre, "30111222", "20301112229", "L-2", new DateTime(2021, 3, 1), null, true);

	#region CargarCommand
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_trae_las_cuentas_y_los_docentes_sin_cuenta()
	{
		// El cruce: de dos docentes activos, el que ya tiene cuenta no aparece como candidato.
		var conCuenta = Guid.NewGuid();
		var sinCuenta = Guid.NewGuid();
		var contexto = new Contexto();
		contexto.DevolverUsuarios(CrearUsuario(conCuenta, "Luna, Juan", Rol.Docente.Id));
		contexto.DevolverDocentes(CrearDocente(conCuenta, "Luna, Juan"), CrearDocente(sinCuenta, "Soto, Ana"));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.Usuarios.Count.ShouldBe(1);
		contexto.ViewModel.DocentesSinCuenta.ShouldHaveSingleItem().DocenteID.ShouldBe(sinCuenta);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Cargar_repone_la_seleccion_por_Id_y_no_por_instancia()
	{
		// Los Response son records nuevos en cada carga: si la selección se repusiera por
		// referencia, la fila quedaría deseleccionada después de asignar un rol y los dos botones
		// se apagarían sin que se vea por qué.
		var contexto = new Contexto();
		var original = CrearUsuario(roles: Rol.Docente.Id);
		contexto.DevolverUsuarios(original);
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);
		contexto.ViewModel.UsuarioSeleccionado = contexto.ViewModel.Usuarios.Single();

		// Misma cuenta, instancia distinta y un rol más, como la devolvería el servicio tras asignar.
		contexto.DevolverUsuarios(original with { RolesIDs = new List<int> { Rol.Docente.Id, Rol.Admin.Id } });
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.UsuarioSeleccionado.ShouldNotBeNull();
		contexto.ViewModel.UsuarioSeleccionado!.RolesIDs.Count.ShouldBe(2);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_error_al_cargar_se_muestra_por_el_dialogo_y_no_se_propaga()
	{
		var contexto = new Contexto();
		contexto.ServicioUsuario.ListarUsuariosAsync()
				.Returns(Task.FromException<IReadOnlyCollection<UsuarioResponse>>(new InvalidOperationException("sin conexion")));

		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.Dialogos.Errores.Count.ShouldBe(1);
		contexto.ViewModel.Cargando.ShouldBeFalse();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public void El_constructor_no_dispara_la_carga()
	{
		// Se construye dentro de la factory del INavigationService, donde no hay nada que atrape
		// una excepción: una consulta que falle ahí revienta la navegación. La dispara el Loaded
		// de la vista. Es el mismo defecto que UI-41 registró para MainViewModel.
		var contexto = new Contexto();

		_ = contexto.ServicioUsuario.DidNotReceive().ListarUsuariosAsync();
		contexto.ViewModel.Usuarios.ShouldBeEmpty();
	}
	#endregion

	#region Roles
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Asignar_rol_esta_deshabilitado_si_el_usuario_ya_lo_tiene()
	{
		// Anticipa la invariante de Usuario.AgregarRol en lugar de dejarla explotar como cartel.
		var contexto = new Contexto();
		contexto.DevolverUsuarios(CrearUsuario(roles: Rol.Docente.Id));
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.UsuarioSeleccionado = contexto.ViewModel.Usuarios.Single();
		contexto.ViewModel.RolSeleccionado = Rol.Docente;

		contexto.ViewModel.AsignarRolCommand.CanExecute(null).ShouldBeFalse();
		contexto.ViewModel.QuitarRolCommand.CanExecute(null).ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Quitar_rol_esta_deshabilitado_si_el_usuario_no_lo_tiene()
	{
		var contexto = new Contexto();
		contexto.DevolverUsuarios(CrearUsuario(roles: Rol.Docente.Id));
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.UsuarioSeleccionado = contexto.ViewModel.Usuarios.Single();
		contexto.ViewModel.RolSeleccionado = Rol.Admin;

		contexto.ViewModel.QuitarRolCommand.CanExecute(null).ShouldBeFalse();
		contexto.ViewModel.AsignarRolCommand.CanExecute(null).ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Asignar_rol_manda_el_Id_del_rol_y_no_su_descripcion()
	{
		var contexto = new Contexto();
		var usuario = CrearUsuario(roles: Rol.Docente.Id);
		contexto.DevolverUsuarios(usuario);
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.UsuarioSeleccionado = contexto.ViewModel.Usuarios.Single();
		contexto.ViewModel.RolSeleccionado = Rol.Preceptoria;

		await contexto.ViewModel.AsignarRolCommand.ExecuteAsync(null);

		_ = contexto.ServicioUsuario.Received(1).AsignarRolAsync(
			new ActualizarRolRequest(usuario.UsuarioID, Rol.Preceptoria.Id));
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Quitar_el_unico_rol_pide_confirmacion_y_sin_ella_no_llega_al_servicio()
	{
		// La cuenta quedaría sin un solo permiso, que es el estado del que esta pantalla vino a
		// rescatar al sistema. Se avisa, pero se deja decidir.
		var contexto = new Contexto();
		contexto.Dialogos.RespuestaDeConfirmacion = false;
		contexto.DevolverUsuarios(CrearUsuario(roles: Rol.Docente.Id));
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.UsuarioSeleccionado = contexto.ViewModel.Usuarios.Single();
		contexto.ViewModel.RolSeleccionado = Rol.Docente;

		await contexto.ViewModel.QuitarRolCommand.ExecuteAsync(null);

		contexto.Dialogos.Confirmaciones.Count.ShouldBe(1);
		_ = contexto.ServicioUsuario.DidNotReceive().QuitarRolAsync(Arg.Any<ActualizarRolRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Quitar_un_rol_cuando_quedan_otros_no_pide_confirmacion()
	{
		var contexto = new Contexto();
		contexto.DevolverUsuarios(CrearUsuario(roles: new[] { Rol.Docente.Id, Rol.Admin.Id }));
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.UsuarioSeleccionado = contexto.ViewModel.Usuarios.Single();
		contexto.ViewModel.RolSeleccionado = Rol.Admin;

		await contexto.ViewModel.QuitarRolCommand.ExecuteAsync(null);

		contexto.Dialogos.Confirmaciones.ShouldBeEmpty();
		_ = contexto.ServicioUsuario.Received(1).QuitarRolAsync(Arg.Any<ActualizarRolRequest>());
	}
	#endregion

	#region CrearUsuarioCommand
	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Crear_cuenta_exige_docente_Y_rol()
	{
		// El rol es obligatorio: un alta sin rol es exactamente lo que el bug de UI-46(a) producía
		// —cuentas sin un solo permiso— y lo que esta pantalla no debe poder generar.
		var contexto = new Contexto();
		contexto.DevolverDocentes(CrearDocente());
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.CrearUsuarioCommand.CanExecute(null).ShouldBeFalse();

		contexto.ViewModel.DocenteSeleccionado = contexto.ViewModel.DocentesSinCuenta.Single();
		contexto.ViewModel.CrearUsuarioCommand.CanExecute(null).ShouldBeFalse();

		contexto.ViewModel.RolSeleccionado = Rol.Docente;
		contexto.ViewModel.CrearUsuarioCommand.CanExecute(null).ShouldBeTrue();
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Crear_cuenta_manda_el_docente_y_el_Id_del_rol()
	{
		var contexto = new Contexto();
		var docente = CrearDocente();
		contexto.DevolverDocentes(docente);
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.DocenteSeleccionado = contexto.ViewModel.DocentesSinCuenta.Single();
		contexto.ViewModel.RolSeleccionado = Rol.SecretariaAcademica;

		await contexto.ViewModel.CrearUsuarioCommand.ExecuteAsync(null);

		_ = contexto.ServicioUsuario.Received(1).RegistrarUsuarioAsync(
			new RegistrarUsuarioRequest(docente.DocenteID, Rol.SecretariaAcademica.Id));
		contexto.Dialogos.Informaciones.Count.ShouldBe(1);
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Crear_cuenta_sin_confirmar_no_llega_al_servicio()
	{
		var contexto = new Contexto();
		contexto.Dialogos.RespuestaDeConfirmacion = false;
		contexto.DevolverDocentes(CrearDocente());
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.DocenteSeleccionado = contexto.ViewModel.DocentesSinCuenta.Single();
		contexto.ViewModel.RolSeleccionado = Rol.Docente;

		await contexto.ViewModel.CrearUsuarioCommand.ExecuteAsync(null);

		_ = contexto.ServicioUsuario.DidNotReceive().RegistrarUsuarioAsync(Arg.Any<RegistrarUsuarioRequest>());
	}

	[Fact]
	[Trait("Categoria", "Unidad")]
	public async Task Un_error_al_crear_la_cuenta_se_muestra_por_el_dialogo()
	{
		var contexto = new Contexto();
		contexto.DevolverDocentes(CrearDocente());
		contexto.ServicioUsuario.RegistrarUsuarioAsync(Arg.Any<RegistrarUsuarioRequest>())
				.Returns(Task.FromException(new ArgumentException("El docente ya cuenta con un usuario en el sistema.")));
		await contexto.ViewModel.CargarCommand.ExecuteAsync(null);

		contexto.ViewModel.DocenteSeleccionado = contexto.ViewModel.DocentesSinCuenta.Single();
		contexto.ViewModel.RolSeleccionado = Rol.Docente;

		await contexto.ViewModel.CrearUsuarioCommand.ExecuteAsync(null);

		contexto.Dialogos.Errores.Count.ShouldBe(1);
		contexto.Dialogos.Informaciones.ShouldBeEmpty();
	}
	#endregion

	#region Catálogo de roles
	[Fact]
	[Trait("Categoria", "Unidad")]
	public void Los_roles_salen_del_dominio_y_son_los_seis()
	{
		// No un x:Array en el XAML, que es el defecto que UI-27 registró para las horas cátedra:
		// agregar un rol al dominio tiene que aparecer en el combo sin tocar la vista.
		var contexto = new Contexto();

		contexto.ViewModel.Roles.Count.ShouldBe(6);
		contexto.ViewModel.Roles.ShouldContain(Rol.Preceptoria);
		contexto.ViewModel.Roles.ShouldContain(Rol.SecretariaAcademica);
	}
	#endregion
}
