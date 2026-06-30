using System;

namespace PulperiaPOS.Models.Caja
{
    public sealed class CajaOperationCoordinator
    {
        private PendingCajaOperation? pendingOperation;

        public PendingCajaOperation GetOrCreate(string operationName, string intentFingerprint)
        {
            if (string.IsNullOrWhiteSpace(operationName))
            {
                throw new ArgumentException("La operacion de caja es requerida.", nameof(operationName));
            }

            if (string.IsNullOrWhiteSpace(intentFingerprint))
            {
                throw new ArgumentException("La intencion de caja es requerida.", nameof(intentFingerprint));
            }

            if (pendingOperation is null ||
                !pendingOperation.IsSameIntent(operationName, intentFingerprint))
            {
                pendingOperation = new PendingCajaOperation(Guid.NewGuid(), operationName, intentFingerprint);
            }

            return pendingOperation;
        }

        public bool TryBegin(PendingCajaOperation operation)
        {
            if (operation.State == CajaOperationState.InProgress)
            {
                return false;
            }

            operation.MarkInProgress();
            return true;
        }

        public void MarkReadyForRetry(PendingCajaOperation operation)
        {
            operation.MarkReadyForRetry();
        }

        public void MarkResultUncertain(PendingCajaOperation operation)
        {
            operation.MarkResultUncertain();
        }

        public void Clear(PendingCajaOperation operation)
        {
            if (ReferenceEquals(pendingOperation, operation))
            {
                pendingOperation = null;
            }
        }

        public void ClearAll()
        {
            pendingOperation = null;
        }
    }
}
