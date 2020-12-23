using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAppLockHelper
{
    public class SqlAppLockValidation
    {
        public static string GetLockOwnerFromScope(SqlServerAppLockScope lockScope)
        {
            var lockOwner = lockScope == SqlServerAppLockScope.Transaction
                ? SqlServerLockScopeNames.Transaction
                : SqlServerLockScopeNames.Session;
            return lockOwner;
        }

        public static void AssertParamsAreValid(string lockName, int lockAcquisitionTimeoutSeconds)
        {
            if (string.IsNullOrWhiteSpace(lockName))
                throw new ArgumentNullException(nameof(lockName));

            if (lockAcquisitionTimeoutSeconds < 1)
                throw new ArgumentOutOfRangeException(
                    nameof(lockAcquisitionTimeoutSeconds), 
                    "The Lock Acquisition timeout must be greater than or equal to 1 second."
                );
        }
        
        public static void AssertLockAcquisitionResultIsValid(SqlServerAppLockAcquisitionResult acquisitionResult)
        {
            if (acquisitionResult == SqlServerAppLockAcquisitionResult.AcquiredImmediately
                || acquisitionResult == SqlServerAppLockAcquisitionResult.AcquiredAfterRelease)
            {
                return;
            }

            throw new SqlServerAppLockAcquisitionException(acquisitionResult);
        }

        public static void AssertLockReleaseResultIsValid(SqlServerAppLockReleaseResult releaseResult)
        {
            if (releaseResult == SqlServerAppLockReleaseResult.ReleasedSuccessfully)
            {
                return;
            }

            throw new SqlServerAppLockReleaseException(releaseResult);

        }
    }
}
