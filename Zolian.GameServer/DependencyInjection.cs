using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Chaos.Cryptography;
using Chaos.Cryptography.Abstractions;
using Chaos.Packets;
using Chaos.Packets.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Zolian.GameServer.DependencyInjection;

/// <summary>
/// DI helpers for wiring networking/crypto into Zolian
/// </summary>
public static class NetworkingExtensions
{
    private static readonly Type[] ConverterTypes = GetConverterTypes();

    /// <summary>
    /// Registers the Chaos <see cref="ICrypto"/> implementation.
    /// </summary>
    public static IServiceCollection AddCryptography(this IServiceCollection services)
    {
        services.AddTransient<ICrypto, Crypto>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="PacketSerializer"/> and all <see cref="IPacketConverter"/>s.
    /// </summary>
    public static IServiceCollection AddPacketSerializer(this IServiceCollection services)
    {
        services.AddSingleton<IPacketSerializer>(sp =>
        {
            var converters = LoadConverters(sp);
            return new PacketSerializer(converters, Encoding.GetEncoding(949));
        });

        return services;
    }

    private static Type[] GetConverterTypes()
    {
        var target = typeof(IPacketConverter);

        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic)
            .SelectMany(a =>
            {
                try
                {
                    return a.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    return ex.Types.Where(t => t != null)!;
                }
                catch
                {
                    return Type.EmptyTypes;
                }
            })
            .Where(t => t is { IsAbstract: false, IsInterface: false } &&
                        target.IsAssignableFrom(t))
            .Distinct()
            .ToArray();
    }

    private static IReadOnlyList<IPacketConverter> LoadConverters(IServiceProvider sp)
    {
        var list = new List<IPacketConverter>(ConverterTypes.Length);

        foreach (var type in ConverterTypes)
        {
            var instance = (IPacketConverter)ActivatorUtilities.CreateInstance(sp, type);
            list.Add(instance);
        }

        return list;
    }
}
