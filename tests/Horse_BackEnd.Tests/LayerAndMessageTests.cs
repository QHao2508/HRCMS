using System.Globalization;
using HorseClub.DAL.Data;
using HorseClub.BLL.Messaging;
using Xunit;

namespace Horse_BackEnd.Tests;

public sealed class LayerAndMessageTests
{
    [Fact]
    public void BusinessUsesServiceContractsAndHasNoEntityFrameworkDependency()
    {
        var business = typeof(AuthenticationService).Assembly;
        Assert.DoesNotContain(business.GetReferencedAssemblies(), x => x.Name!.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
        foreach (var service in business.GetExportedTypes().Where(x => x.IsClass && x.Name.EndsWith("Service", StringComparison.Ordinal)))
            Assert.Contains(service.GetInterfaces(), x => x.FullName == "HorseClub.BLL.Abstractions.Services.I" + service.Name);
        foreach (var type in typeof(Program).Assembly.GetTypes().Where(x => x.FullName?.StartsWith("Horse_BackEnd.Endpoints.", StringComparison.Ordinal) == true))
            foreach (var parameter in type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).SelectMany(x => x.GetParameters()))
                Assert.False(parameter.ParameterType.IsClass && parameter.ParameterType.Assembly == business && parameter.ParameterType.Name.EndsWith("Service", StringComparison.Ordinal), $"{type.Name} depends on concrete {parameter.ParameterType.Name}");
    }

    [Fact]
    public void LayerDependenciesHaveNoReverseReferences()
    {
        var api = typeof(Program).Assembly;
        var business = typeof(AuthenticationService).Assembly;
        var data = typeof(ClubDbContext).Assembly;
        Assert.Equal("HorseClub.BLL", business.GetName().Name);
        Assert.Equal("HorseClub.DAL", data.GetName().Name);
        Assert.Contains(api.GetReferencedAssemblies(), x => x.Name == business.GetName().Name);
        Assert.Contains(business.GetReferencedAssemblies(), x => x.Name == data.GetName().Name);
        Assert.DoesNotContain(business.GetReferencedAssemblies(), x => x.Name == api.GetName().Name);
        Assert.DoesNotContain(data.GetReferencedAssemblies(), x => x.Name == business.GetName().Name || x.Name == api.GetName().Name);
        Assert.DoesNotContain(data.GetReferencedAssemblies(), x => x.Name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
        foreach (var assembly in new[] { api, business, data })
            Assert.DoesNotContain(assembly.GetReferencedAssemblies(), x => x.Name!.Contains("Sqlite", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BusinessServicesDoNotExposeHttpRequestsOrResults()
    {
        var services = typeof(AuthenticationService).Assembly.GetExportedTypes()
            .Where(x => x.Name.EndsWith("Service", StringComparison.Ordinal));
        foreach (var service in services)
            foreach (var method in service.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly))
            {
                var types = method.GetParameters().Select(x => x.ParameterType).Append(method.ReturnType);
                foreach (var type in types.SelectMany(ExpandTypes))
                    Assert.False(type.Namespace?.StartsWith("Microsoft.AspNetCore.Http", StringComparison.Ordinal) == true,
                        $"{service.Name}.{method.Name} exposes HTTP type {type.Name}.");
            }
        Assert.DoesNotContain(typeof(AuthenticationService).Assembly.GetExportedTypes(), x => x.Name.EndsWith("Workflow", StringComparison.Ordinal));
    }

    private static IEnumerable<Type> ExpandTypes(Type type)
    {
        yield return type;
        foreach (var argument in type.GetGenericArguments())
            foreach (var nested in ExpandTypes(argument)) yield return nested;
    }

    [Fact]
    public void EveryMessageKeyHasANonEmptyTemplate()
    {
        foreach (var key in Enum.GetValues<MessageKey>()) Assert.False(string.IsNullOrWhiteSpace(Messages.Get(key)));
    }

    [Fact]
    public void DynamicMessagesFormatValuesAndEmailExpirationWithInvariantCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("vi-VN");
            Assert.Equal("Staff must be an active Trainer.", Messages.Get(MessageKey.StaffMustBeAnActive, "Trainer"));
            var expires = new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
            var email = Messages.Get(MessageKey.YourCodeExpiresAtNeverShareThisCode, "Verify", "123456", expires);
            Assert.Contains("Verify code: 123456", email);
            Assert.Contains(expires.ToString("O", CultureInfo.InvariantCulture), email);
            Assert.DoesNotContain("{0}", email);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
