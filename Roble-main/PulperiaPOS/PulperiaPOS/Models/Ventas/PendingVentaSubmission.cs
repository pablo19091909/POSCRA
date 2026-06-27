using System;

namespace PulperiaPOS.Models.Ventas
{
    public sealed class PendingVentaSubmission
    {
        public PendingVentaSubmission(Guid idempotencyKey, string intentFingerprint)
        {
            IdempotencyKey = idempotencyKey;
            IntentFingerprint = intentFingerprint;
            State = VentaSubmissionState.Ready;
        }

        public Guid IdempotencyKey { get; }
        public string IntentFingerprint { get; }
        public VentaSubmissionState State { get; private set; }

        public void MarkInProgress()
        {
            State = VentaSubmissionState.InProgress;
        }

        public void MarkReadyForRetry()
        {
            State = VentaSubmissionState.Ready;
        }
    }
}
