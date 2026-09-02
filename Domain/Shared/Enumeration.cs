using System.Reflection;

namespace Domain.Shared;

public abstract class Enumeration : IComparable
{
	public int Id { get; private set; }
	public string Descripcion { get; private set; }


	#region CONSTRUCTOR
	protected Enumeration() { }

	protected Enumeration(int id, string descripcion) =>
		(Id, Descripcion) = (id, descripcion);
	#endregion

	public override bool Equals(object obj)
	{
		if (obj is not Enumeration otherValue)
		{
			return false;
		}

		var typeMatches = GetType().Equals(obj.GetType());
		var valueMatches = Id.Equals(otherValue.Id);

		return typeMatches && valueMatches;
	}

	public static int AbsoluteDifference(Enumeration firstValue, Enumeration secondValue)
	{
		var absoluteDifference = Math.Abs(firstValue.Id - secondValue.Id);
		return absoluteDifference;
	}

	public static T FromValue<T>(int value)
		where T : Enumeration
	{
		var matchingItem = Parse<T, int>(value, "value", item => item.Id == value);
		return matchingItem;
	}

	public static T FromDescripcion<T>(string descripcion)
		where T : Enumeration
	{
		var matchingItem = Parse<T, string>(descripcion, "display name", item => item.Descripcion == descripcion);
		return matchingItem;
	}

	private static T Parse<T, K>(K value, string description, Func<T, bool> predicate)
		where T : Enumeration
	{
		var matchingItem = GetAll<T>().FirstOrDefault(predicate);

		if (matchingItem == null)
		{
			throw new InvalidOperationException($"'{value}' is not a valid {description} in {typeof(T)}");
		}
		return matchingItem;
	}

	public static IEnumerable<T> GetAll<T>()
		where T : Enumeration
	{
		return typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
						.Select(f => f.GetValue(null))
						.Cast<T>();
	}

	public int CompareTo(object other) =>
		Id.CompareTo(((Enumeration)other).Id);

	public override int GetHashCode() =>
		Id.GetHashCode();

	public override string ToString() =>
		Descripcion;
}