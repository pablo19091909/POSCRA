using System.Linq;
using System.Windows;

namespace PulperiaPOS.ApiClients
{
    public static class ApiSessionNavigationCoordinator
    {
        private static bool isSubscribed;
        private static bool isHandlingExpiration;

        public static void Subscribe()
        {
            if (isSubscribed)
            {
                return;
            }

            ApiSessionCoordinator.SessionExpired += OnSessionExpired;
            isSubscribed = true;
        }

        private static void OnSessionExpired(object? sender, ApiSessionExpiredEventArgs e)
        {
            var application = Application.Current;
            if (application is null)
            {
                return;
            }

            application.Dispatcher.Invoke(() =>
            {
                if (isHandlingExpiration)
                {
                    return;
                }

                isHandlingExpiration = true;

                MessageBox.Show(e.SafeMessage, "Sesión expirada", MessageBoxButton.OK, MessageBoxImage.Warning);

                var loginAlreadyOpen = application.Windows
                    .OfType<LoginWindow>()
                    .Any(window => window.IsVisible);

                if (!loginAlreadyOpen)
                {
                    new LoginWindow().Show();
                }

                foreach (var window in application.Windows.OfType<Window>().ToList())
                {
                    if (window is not LoginWindow)
                    {
                        window.Close();
                    }
                }

                isHandlingExpiration = false;
            });
        }
    }
}
