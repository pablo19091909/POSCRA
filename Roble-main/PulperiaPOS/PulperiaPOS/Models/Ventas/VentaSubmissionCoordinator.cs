using System;

namespace PulperiaPOS.Models.Ventas
{
    public sealed class VentaSubmissionCoordinator
    {
        private PendingVentaSubmission? pendingSubmission;

        public PendingVentaSubmission GetOrCreate(string intentFingerprint)
        {
            if (pendingSubmission is null ||
                !string.Equals(pendingSubmission.IntentFingerprint, intentFingerprint, StringComparison.Ordinal))
            {
                pendingSubmission = new PendingVentaSubmission(Guid.NewGuid(), intentFingerprint);
            }

            return pendingSubmission;
        }

        public void Clear()
        {
            pendingSubmission = null;
        }
    }
}
