using System;

namespace WPF_Desktop.Models;

public class Usuario
{
    public Guid UsuarioID { get; set; }
    public Guid DocenteID { get; set; }
    public string Username { get; set; }
    public string NombreCompleto { get; set; }
    public string Rol { get; set; }
}