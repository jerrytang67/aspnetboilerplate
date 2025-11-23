using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Abp.Collections.Extensions;
using Abp.Logging;

namespace Abp.Reflection {
    public class TypeFinder : ITypeFinder {
        private readonly IAssemblyFinder _assemblyFinder;
        private readonly ILogger<TypeFinder> _logger;
        private readonly object _syncObj = new object();
        private Type[] _types;

        public TypeFinder(IAssemblyFinder assemblyFinder, ILogger<TypeFinder> logger) {
            _assemblyFinder = assemblyFinder;
            _logger = logger;
        }

        public Type[] Find(Func<Type, bool> predicate) {
            return GetAllTypes().Where(predicate).ToArray();
        }

        public Type[] FindAll() {
            return GetAllTypes().ToArray();
        }

        private Type[] GetAllTypes() {
            if (_types == null) {
                lock (_syncObj) {
                    if (_types == null) {
                        _types = CreateTypeList().ToArray();
                    }
                }
            }

            return _types;
        }

        private List<Type> CreateTypeList() {
            var allTypes = new List<Type>();

            var assemblies = _assemblyFinder.GetAllAssemblies().Distinct();

            foreach (var assembly in assemblies) {
                try {
                    Type[] typesInThisAssembly;

                    try {
                        typesInThisAssembly = assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex) {
                        typesInThisAssembly = ex.Types;
                    }

                    if (typesInThisAssembly.IsNullOrEmpty()) {
                        continue;
                    }

                    allTypes.AddRange(typesInThisAssembly.Where(type => type != null));
                }
                catch (Exception ex) {
                    _logger.LogWarning(ex.ToString(), ex);
                }
            }

            return allTypes;
        }
    }
}