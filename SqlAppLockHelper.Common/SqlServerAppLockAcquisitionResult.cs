using System;

namespace SqlAppLockHelper
{
    public enum SqlServerAppLockAcquisitionResult
    {
        AcquiredImmediately = 0,
        AcquiredAfterRelease = 1,
        FailedDueToTimeout = -1,
        AcquisitionCancelled = -2,
        FailedDueToDeadlock = -3,
        ParameterValidationOrOtherError = -999
    }
}
