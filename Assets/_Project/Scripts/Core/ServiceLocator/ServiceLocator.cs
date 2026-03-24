using System;
using System.Collections.Generic;

namespace Mergistry.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T service) => _services[typeof(T)] = service;

        public static T Get<T>()
        {
            if (_services.TryGetValue(typeof(T), out var s)) return (T)s;
            throw new InvalidOperationException($"Service not registered: {typeof(T).Name}");
        }

        public static bool TryGet<T>(out T service)
        {
            if (_services.TryGetValue(typeof(T), out var s)) { service = (T)s; return true; }
            service = default;
            return false;
        }

        public static void Clear() => _services.Clear();
    }
}
