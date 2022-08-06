using System;

namespace OmniSharp.Events
{
    public sealed class SubscriptionToken : IDisposable
    {
        private readonly Action _unsubscribe;

        public SubscriptionToken(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        /// <summary>
        /// Unsubscribes
        /// </summary>
        public void Dispose()
        {
            _unsubscribe();
        }
    }
}
