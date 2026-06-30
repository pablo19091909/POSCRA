using System;

namespace PulperiaPOS.Models.Caja
{
    public sealed class PendingCajaOperation
    {
        public PendingCajaOperation(Guid idempotencyKey, string operationName, string intentFingerprint)
        {
            IdempotencyKey = idempotencyKey;
            OperationName = operationName;
            IntentFingerprint = intentFingerprint;
            State = CajaOperationState.Ready;
        }

        public Guid IdempotencyKey { get; }
        public string OperationName { get; }
        public string IntentFingerprint { get; }
        public CajaOperationState State { get; private set; }

        public bool IsSameIntent(string operationName, string intentFingerprint)
        {
            return string.Equals(OperationName, operationName, StringComparison.Ordinal) &&
                   string.Equals(IntentFingerprint, intentFingerprint, StringComparison.Ordinal);
        }

        public void MarkInProgress()
        {
            State = CajaOperationState.InProgress;
        }

        public void MarkReadyForRetry()
        {
            State = CajaOperationState.Ready;
        }

        public void MarkResultUncertain()
        {
            State = CajaOperationState.ResultUncertain;
        }
    }
}
