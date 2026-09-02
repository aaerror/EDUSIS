namespace Infrastructure.Documentos.Content;

public interface IParrafo : IContenido
{
	IParrafo ConTitulo(string titulo);
	IParrafo ConSubtitulo(string subtitulo);
	IParrafo ConParrafo(string texto);
}