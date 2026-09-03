using System.Net;

namespace Core.ServicioAutenticaciones.DTOs.Requests;

public record LoginRequest(NetworkCredential credential);
