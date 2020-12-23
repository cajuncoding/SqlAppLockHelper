using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAppLockHelper
{
    public class SqlServerAppLockAcquisitionException : Exception
    {
        public SqlServerAppLockAcquisitionException(SqlServerAppLockAcquisitionResult result, Exception innerException = null)
            : base($"Failed to Acquire the Application Lock with Sql Server due to [{result}]", innerException)
        {
            this.LockAcquisitionResult = result;
        }


        public SqlServerAppLockAcquisitionException(SqlServerAppLockAcquisitionResult result, string message, Exception innerException = null)
            : base(message, innerException)
        {
            this.LockAcquisitionResult = result;
        }

        public SqlServerAppLockAcquisitionResult LockAcquisitionResult { get; }
    }
}
