using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAppLockHelper
{
    public class SqlServerAppLockReleaseException : Exception
    {
        public SqlServerAppLockReleaseException(SqlServerAppLockReleaseResult result, Exception innerException = null)
            : base($"Failed to Release the Application Lock with Sql Server due to [{result}]", innerException)
        {
            this.LockReleaseResult = result;
        }


        public SqlServerAppLockReleaseException(SqlServerAppLockReleaseResult result, string message, Exception innerException = null)
            : base(message, innerException)
        {
            this.LockReleaseResult = result;
        }

        public SqlServerAppLockReleaseResult LockReleaseResult { get; }
    }
}
