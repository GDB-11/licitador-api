using System.Reflection;
using Global.Attributes;
using Global.Helpers.Functional;
using Global.Objects.Encryption;
using Global.Objects.Functional;
using Global.Objects.Results;

namespace Global.Helpers.Reflection;

public static class ReflectionHelpers
{
    public static IEnumerable<PropertyInfo> GetEncryptedProperties<T>() =>
        typeof(T).GetProperties()
            .Where(p => p.GetCustomAttribute<EncryptedFieldAttribute>() is not null);

    public static IEnumerable<PropertyInfo> GetAllProperties<T>() =>
        typeof(T).GetProperties();

    public static Result<T, EncryptionError> CreateInstance<T>() where T : class
    {
        return ResultExtensions.Try(
            () => Activator.CreateInstance<T>(),
            "Failed to create instance"
        ).MapError(error => new DecryptionError(error) as EncryptionError);
    }

    public static Result<Unit, EncryptionError> CopyProperty(
        PropertyInfo property,
        object source,
        object destination)
    {
        return ResultExtensions.Try(() =>
                {
                    object? value = property.GetValue(source);
                    property.SetValue(destination, value);
                    return Unit.Value;
                }, $"Failed to copy property '{property.Name}'")
            .MapError(error => new DecryptionError(error, property.Name) as EncryptionError);
    }

    public static Result<Unit, EncryptionError> SetPropertyValue(
        PropertyInfo property,
        object target,
        object? value)
    {
        return ResultExtensions.Try(() =>
                {
                    property.SetValue(target, value);
                    return Unit.Value;
                }, $"Failed to set property '{property.Name}'")
            .MapError(error => new DecryptionError(error, property.Name) as EncryptionError);
    }
}