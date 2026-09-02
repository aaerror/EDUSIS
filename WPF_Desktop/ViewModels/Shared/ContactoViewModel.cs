using CommunityToolkit.Mvvm.ComponentModel;
using Core.Shared.DTOs.Personas.Responses;
using System.ComponentModel.DataAnnotations;

namespace WPF_Desktop.ViewModels.Shared;

internal partial class ContactoViewModel : ObservableValidator
{
	[Required(AllowEmptyStrings=false, ErrorMessage="Se debe especificar un número de telefono.")]
	[RegularExpression(@"^(?:\+54|549)?(?:0?(\d{1,4}))?[\s\.-]?(\d{1,4})[\s\.-]?(\d{4})$", ErrorMessage="El formato del número de teléfono es inválido.", MatchTimeoutInMilliseconds=2500)]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _telefono;

	[Required(AllowEmptyStrings = false, ErrorMessage="Se debe especificar un correo electrónico.")]
	[EmailAddress]
	[RegularExpression(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", ErrorMessage="El formato del correo electrónico es inválido.", MatchTimeoutInMilliseconds=2500)]
	[NotifyDataErrorInfo]
	[ObservableProperty]
	private string _email;


	public ContactoViewModel(ContactoResponse contactoResponse)
	{
		if (contactoResponse is not null)
		{
			Telefono = contactoResponse.Telefono;
			Email = contactoResponse.Email;
		}
	}
}