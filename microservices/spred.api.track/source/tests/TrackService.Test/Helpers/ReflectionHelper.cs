using System.Reflection;

namespace TrackService.Test.Helpers;

public static class ReflectionHelper
{
    public static void SetProtectedProperty<T>(object target, string propertyName, T value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property == null)
            throw new ArgumentException($"Property '{propertyName}' not found on type {target.GetType().Name}");

        property.SetValue(target, value);
    }
}