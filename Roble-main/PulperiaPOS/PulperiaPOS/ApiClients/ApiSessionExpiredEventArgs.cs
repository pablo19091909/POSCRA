using System;

namespace PulperiaPOS.ApiClients
{
    public sealed class ApiSessionExpiredEventArgs : EventArgs
    {
        public ApiSessionExpiredEventArgs(string safeMessage)
        {
            SafeMessage = safeMessage;
        }

        public string SafeMessage { get; }
    }
}
