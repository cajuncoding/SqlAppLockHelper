using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace SqlAppLockHelper.SystemDataNS
{
    public static class SqlAppLockCustomExtensions
    {
        public static SqlServerAppLock AcquireAppLock(
            this SqlTransaction sqlTransaction, 
            string lockName, 
            int acquisitionTimeoutSeconds = 0,
            bool throwsException = true,
            int? sqlCommandTimeout = null
        )
        {
            var sqlTrans = sqlTransaction ?? throw new ArgumentNullException(nameof(sqlTransaction));

            var sqlConn = sqlTrans.Connection ?? throw new ArgumentException(
                "The SqlConnection associated with the current SqlTransaction cannot be null.",
                nameof(sqlTransaction)
            );

            //Acquire the Lock using this Transaction as Scope!
            var resultAppLock = sqlConn.AcquireAppLock(
                lockName, 
                acquisitionTimeoutSeconds, 
                throwsException, 
                sqlCommandTimeout, 
                sqlTransaction
            );

            return resultAppLock;
        }

        public static async Task<SqlServerAppLock> AcquireAppLockAsync(
            this SqlTransaction sqlTransaction, 
            string lockName, 
            int acquisitionTimeoutSeconds = 0, 
            bool throwsException = true,
            int? sqlCommandTimeout = null
        )
        {
            var sqlTrans = sqlTransaction ?? throw new ArgumentNullException(nameof(sqlTransaction));

            var sqlConn = sqlTrans.Connection ?? throw new ArgumentException(
              "The SqlConnection associated with the current SqlTransaction cannot be null.",
              nameof(sqlTransaction)
            );

            //Acquire the Lock using this Transaction as Scope!
            var resultAppLock = await sqlConn.AcquireAppLockAsync(
                lockName, 
                acquisitionTimeoutSeconds, 
                throwsException, 
                sqlCommandTimeout,
                sqlTransaction
            );

            return resultAppLock;
        }

        public static SqlServerAppLock AcquireAppLock(
            this SqlConnection sqlConnection, 
            string lockName, 
            int acquisitionTimeoutSeconds = 0,
            bool throwsException = true,
            int? sqlCommandTimeout = null,
            SqlTransaction sqlTransaction = null
        )
        {
            var sqlConn = sqlConnection ?? throw new ArgumentException(
                "The SqlConnection cannot be null.",
                nameof(sqlConnection)
            );

            var lockScope = sqlTransaction != null ? SqlServerAppLockScope.Transaction : SqlServerAppLockScope.Session;

            using var sqlCmd = SqlAppLockCommandBuilder.CreateAcquireLockSqlCommand(
                sqlConn, 
                lockName,
                lockScope,
                acquisitionTimeoutSeconds,
                sqlCommandTimeout,
                sqlTransaction
            );

            //Execute the Acquisition process...
            sqlCmd.ExecuteNonQuery();

            //Get & Validate the Return Value!
            var acquisitionResult = sqlCmd.GetLockAcquisitionResultValue();

            if(throwsException)
                SqlAppLockValidation.AssertLockAcquisitionResultIsValid(acquisitionResult);

            var resultAppLock = new SqlServerAppLock(
                lockName,
                lockScope,
                acquisitionResult,
                releaseAction: SqlAppLockCommandBuilder.CreateReleaseLockDelegate(sqlConn, lockName, lockScope),
                releaseActionAsync: SqlAppLockCommandBuilder.CreateReleaseLockAsyncDelegate(sqlConn, lockName, lockScope)
            );

            return resultAppLock;
        }

        public static async Task<SqlServerAppLock> AcquireAppLockAsync(
            this SqlConnection sqlConnection, 
            string lockName,
            int acquisitionTimeoutSeconds = 0, 
            bool throwsException = true,
            int? sqlCommandTimeout = null,
            SqlTransaction sqlTransaction = null
        )
        {
            var sqlConn = sqlConnection ?? throw new ArgumentException(
              "The SqlConnection cannot be null.",
              nameof(sqlConnection)
            );

            var lockScope = sqlTransaction != null ? SqlServerAppLockScope.Transaction : SqlServerAppLockScope.Session;

            await using var sqlCmd = SqlAppLockCommandBuilder.CreateAcquireLockSqlCommand(
                sqlConn, 
                lockName,
                lockScope,
                acquisitionTimeoutSeconds,
                sqlCommandTimeout,
                sqlTransaction
            );
            
            //Execute the Acquisition process...
            await sqlCmd.ExecuteNonQueryAsync();

            //Get & Validate the Return Value!
            var acquisitionResult = sqlCmd.GetLockAcquisitionResultValue();

            if (throwsException)
                SqlAppLockValidation.AssertLockAcquisitionResultIsValid(acquisitionResult);

            var resultAppLock = new SqlServerAppLock(
                lockName,
                lockScope,
                acquisitionResult,
                releaseAction: SqlAppLockCommandBuilder.CreateReleaseLockDelegate(sqlConn, lockName, lockScope),
                releaseActionAsync: SqlAppLockCommandBuilder.CreateReleaseLockAsyncDelegate(sqlConn, lockName, lockScope)
            );

            return resultAppLock;
        }

        public static SqlServerAppLockAcquisitionResult GetLockAcquisitionResultValue(this SqlCommand sqlCmd)
        {
            //Get the Return Value!
            var sqlReturnParam = sqlCmd.Parameters[SqlServerStoredParamNames.ReturnValue];

            var acquisitionResult = sqlReturnParam != null
                ? (SqlServerAppLockAcquisitionResult)sqlReturnParam.Value
                : SqlServerAppLockAcquisitionResult.ParameterValidationOrOtherError;

            return acquisitionResult;
        }

        public static SqlServerAppLockReleaseResult GetLockReleaseResultValue(this SqlCommand sqlCmd)
        {
            //Get the Return Value!
            var sqlReturnParam = sqlCmd.Parameters[SqlServerStoredParamNames.ReturnValue];

            var acquisitionResult = sqlReturnParam != null
                ? (SqlServerAppLockReleaseResult)sqlReturnParam.Value
                : SqlServerAppLockReleaseResult.ParameterValidationOrOtherError;

            return acquisitionResult;
        }
    }
}
