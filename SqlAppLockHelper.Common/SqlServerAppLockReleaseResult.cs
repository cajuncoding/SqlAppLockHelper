using System;

namespace SqlAppLockHelper
{
    public enum SqlServerAppLockReleaseResult
    {
        ReleasedSuccessfully = 0,
        ParameterValidationOrOtherError = -999
    }
}
