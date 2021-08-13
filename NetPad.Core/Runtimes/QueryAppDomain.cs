using System;
using NetPad.Queries;

namespace NetPad.Runtimes
{
    public class QueryAppDomain
    {
        private AppDomain _appDomain;
        private string _name;

        public QueryAppDomain()
        {
            _name = $"QueryAppDomain_{Guid.NewGuid()}";
            ResetAppDomain();
        }

        public AppDomain AppDomain => _appDomain;
        
        public void ResetAppDomain()
        {
            UnloadAppDomain();

            _appDomain = AppDomain.CreateDomain(_name);
        }
        
        private void UnloadAppDomain()
        {
            if (_appDomain != null)
            {
                AppDomain.Unload(_appDomain);
            }
        }
    }
}