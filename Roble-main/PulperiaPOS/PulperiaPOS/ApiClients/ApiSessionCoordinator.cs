using System;

namespace PulperiaPOS.ApiClients
{
    public static class ApiSessionCoordinator
    {
        private static readonly object SyncRoot = new();
        private static bool sessionExpiredNotified;

        public static event EventHandler<ApiSessionExpiredEventArgs>? SessionExpired;

        public static void NotifySessionExpired(string safeMessage)
        {
            lock (SyncRoot)
            {
                if (sessionExpiredNotified)
                {
                    return;
                }

                sessionExpiredNotified = true;
            }

            UserSession.Clear();
            SessionExpired?.Invoke(null, new ApiSessionExpiredEventArgs(safeMessage));
        }

        public static void Reset()
        {
            lock (SyncRoot)
            {
                sessionExpiredNotified = false;
            }
        }
    }
}
