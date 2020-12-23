using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace SqlAppLockHelper.MicrosoftDataNS
{
    public class SqlAppLockCommandBuilder
    {
        public static SqlCommand CreateAcquireLockSqlCommand(
            SqlConnection sqlConnection, 
            string lockName, 
            SqlServerAppLockScope lockScope,
            int acquisitionTimeoutSeconds,
            SqlTransaction sqlTransaction = null)
        {
            var sqlConn = sqlConnection ?? throw new ArgumentException(
              "The SqlConnection provided cannot be null.",
              nameof(sqlConnection)
            );

            if (sqlConn.State != ConnectionState.Open)
            {
                throw new ArgumentException(
                    $"The SqlConnection provided must be [Open]; current state is [{sqlConn.State}].",
                    nameof(sqlConnection)
                );
            }

            SqlAppLockValidation.AssertParamsAreValid(lockName, acquisitionTimeoutSeconds);

            //Sql Server uses Milliseconds, but we use Seconds to simplify the C# Api.
            var acquisitionTimeoutMillis = acquisitionTimeoutSeconds * 1000;

            var lockScopeText = lockScope == SqlServerAppLockScope.Transaction
                                        ? SqlServerLockScopeNames.Transaction
                                        : SqlServerLockScopeNames.Session;

            var sqlCmd = new SqlCommand(SqlServerStoredProcNames.AcquireLock, sqlConn)
            {
                CommandType = CommandType.StoredProcedure,
                Transaction = sqlTransaction
            };

            sqlCmd.Parameters.AddRange(new[]
            {
                CreateSqlParam(SqlServerStoredParamNames.Resource, lockName),
                CreateSqlParam(SqlServerStoredParamNames.LockTimeout, acquisitionTimeoutMillis),
                CreateSqlParam(SqlServerStoredParamNames.LockOwner, lockScopeText),
                CreateSqlParam(SqlServerStoredParamNames.LockMode, SqlServerLockModeNames.Exclusive),
                CreateSqlReturnParam(SqlServerStoredParamNames.ReturnValue)
            });

            return sqlCmd;
        }

        public static SqlCommand CreateReleaseLockSqlCommand(
            SqlConnection sqlConnection,
            string lockName,
            SqlServerAppLockScope sqlAppLockScope
        )
        {
            var sqlConn = sqlConnection ?? throw new ArgumentException(
                $"The SqlConnection cannot be null; this is CRITICAL error during Dispose(), and may result in an" +
                            $" abandoned {nameof(SqlServerAppLock)} on the server.",
                nameof(sqlConnection)
            );

            if (sqlConn.State != ConnectionState.Open)
            {
                throw new ArgumentException(
                    $"The SqlConnection must be [Open]; current state is [{sqlConn.State}]. This is CRITICAL error during" +
                                $" Dispose(), and may result in an abandoned {nameof(SqlServerAppLock)} on the server.",
                    nameof(sqlConnection)
                );
            }

            var lockScope = SqlAppLockValidation.GetLockOwnerFromScope(sqlAppLockScope);

            var sqlCmd = new SqlCommand(SqlServerStoredProcNames.ReleaseLock, sqlConn)
            {
                CommandType = CommandType.StoredProcedure
            };

            sqlCmd.Parameters.AddRange(new[]
            {
                CreateSqlParam(SqlServerStoredParamNames.Resource, lockName),
                CreateSqlParam(SqlServerStoredParamNames.LockOwner, lockScope),
                CreateSqlReturnParam(SqlServerStoredParamNames.ReturnValue)
            });

            return sqlCmd;
        }

        public static SqlParameter CreateSqlParam(string name, string value)
        {
            var param = new SqlParameter(name, SqlDbType.VarChar) { Value = value };
            return param;
        }

        public static SqlParameter CreateSqlParam(string name, int value)
        {
            var param = new SqlParameter(name, SqlDbType.Int) { Value = value };
            return param;
        }
        public static SqlParameter CreateSqlReturnParam(string name, SqlDbType dbType = SqlDbType.Int)
        {
            var param = new SqlParameter(name, dbType) { Direction = ParameterDirection.ReturnValue };
            return param;
        }

        public static Func<ValueTask> CreateReleaseLockAsyncDelegate(
            SqlConnection sqlConnection,
            string lockName,
            SqlServerAppLockScope sqlAppLockScope
        )
        {
            return async () =>
            {
                //Note: If using Session scoped Lock then we must explicitly release, but if using Transaction scoped lock
                //  then we skip this for performance, allow SqlServer to handle the release automatically.
                if (sqlAppLockScope == SqlServerAppLockScope.Transaction) return;

                try
                {
                    await using var sqlCmd = CreateReleaseLockSqlCommand(sqlConnection, lockName, sqlAppLockScope);

                    //Execute the Release process...
                    await sqlCmd.ExecuteNonQueryAsync();

                    //Get the return value...
                    var releaseResult = sqlCmd.GetLockReleaseResultValue();

                    if (releaseResult != SqlServerAppLockReleaseResult.ReleasedSuccessfully)
                        throw new SqlServerAppLockReleaseException(releaseResult);
                }
                catch (SqlException sqlExc)
                {
                    throw new SqlServerAppLockReleaseException(SqlServerAppLockReleaseResult.ParameterValidationOrOtherError, sqlExc);
                }
            };
        }

        public static Action CreateReleaseLockDelegate(
            SqlConnection sqlConnection,
            string lockName,
            SqlServerAppLockScope sqlAppLockScope
        )
        {
            return () =>
            {
                //Note: If using Session scoped Lock then we must explicitly release, but if using Transaction scoped lock
                //  then we skip this for performance, allow SqlServer to handle the release automatically.
                if (sqlAppLockScope == SqlServerAppLockScope.Transaction) return;

                try
                {
                    using var sqlCmd = CreateReleaseLockSqlCommand(sqlConnection, lockName, sqlAppLockScope);

                    //Execute the Release process...
                    sqlCmd.ExecuteNonQuery();

                    //Get the return value...
                    var releaseResult = sqlCmd.GetLockReleaseResultValue();

                    if (releaseResult != SqlServerAppLockReleaseResult.ReleasedSuccessfully)
                        throw new SqlServerAppLockReleaseException(releaseResult);
                }
                catch (SqlException sqlExc)
                {
                    throw new SqlServerAppLockReleaseException(SqlServerAppLockReleaseResult.ParameterValidationOrOtherError, sqlExc);
                }
            };
        }
    }
}
