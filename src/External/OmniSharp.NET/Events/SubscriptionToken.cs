using System;

namespace OmniSharp.Events
{
    public sealed class SubscriptionToken : IDisposable
    {
        private readonly Action _unsubscribe;

        internal SubscriptionToken(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        /// <summary>
        /// Unsubscribes the token.
        /// </summary>
        public void Dispose()
        {
            _unsubscribe();
        }
    }
}
