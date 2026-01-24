using System.Reflection;
using BindSharp;
using Global.Attributes;

namespace Global.Helpers.Reflection;

public static class ReflectionHelpers
{
    public static IEnumerable<PropertyInfo> GetEncryptedProperties<T>() =>
        typeof(T).GetProperties()
            .Where(p => p.GetCustomAttribute<EncryptedFieldAttribute>() is not null);

    public static IEnumerable<PropertyInfo> GetAllProperties<T>() =>
        typeof(T).GetProperties();

    public static Result<T, Exception> CreateInstance<T>() where T : class =>
        Result.Try(
            Activator.CreateInstance<T>
        );

    public static Result<Unit, Exception> CopyProperty(
        PropertyInfo property,
        object source,
        object destination) =>
        Result.Try(() =>
                {
                    object? value = property.GetValue(source);
                    property.SetValue(destination, value);
                    return Unit.Value;
                });

    public static Result<Unit, Exception> SetPropertyValue(
        PropertyInfo property,
        object target,
        object? value) =>
        Result.Try(() =>
        {
            property.SetValue(target, value);
            return Unit.Value;
        });
}