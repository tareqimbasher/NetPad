using System;
using NetPad.Queries;

namespace NetPad.Runtimes
{
    public sealed class QueryAppDomain : IDisposable
    {
        private AppDomain? _appDomain;
        private readonly string _name;

        public QueryAppDomain()
        {
            _name = $"QueryAppDomain_{Guid.NewGuid()}";
        }

        public AppDomain AppDomain => _appDomain ??= CreateNewAppDomain();

        private AppDomain CreateNewAppDomain()
        {
            return AppDomain.CreateDomain(_name);
        }

        private void UnloadAppDomain()
        {
            if (_appDomain != null)
            {
                AppDomain.Unload(_appDomain);
            }
        }

        public void Dispose()
        {
            UnloadAppDomain();
        }
    }
}