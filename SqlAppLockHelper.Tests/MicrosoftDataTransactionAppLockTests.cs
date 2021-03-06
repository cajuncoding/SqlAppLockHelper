using System;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlAppLockHelper.MicrosoftDataNS;

namespace SqlAppLockHelper.Tests
{
    [TestClass]
    public class TestMicrosoftDataTransactionAppLock
    {
        [TestMethod]
        public async Task TestAsyncTransactionAppLockAcquisitionExceptionsDisabled()
        {
            await using var sqlConn = SqlConnectionHelper.CreateMicrosoftDataSqlConnection();
            await sqlConn.OpenAsync();

            await using var sqlTrans = (SqlTransaction)await sqlConn.BeginTransactionAsync();

            //Acquire the Lock & Validate
            await using var appLock = await sqlTrans.AcquireAppLockAsync(
                nameof(TestSystemDataTransactionAppLock),
                acquisitionTimeoutSeconds: 1,
                throwsException: false,
                sqlCommandTimeout: 20
            );

            Assert.IsNotNull(appLock);
            Assert.AreEqual(appLock.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLock.LockName));

            //Attempt Acquisition from SECOND Connection Once Locked & Validate...
            await using var sqlConnWhileLocked = SqlConnectionHelper.CreateMicrosoftDataSqlConnection();
            await sqlConnWhileLocked.OpenAsync();

            await using var sqlTransWhileLocked = (SqlTransaction)await sqlConnWhileLocked.BeginTransactionAsync();
            await using var appLockFailWhileLocked = await sqlTransWhileLocked.AcquireAppLockAsync(
                nameof(TestSystemDataTransactionAppLock),
                acquisitionTimeoutSeconds: 3,
                throwsException: false
            );

            Assert.IsNotNull(appLockFailWhileLocked);
            Assert.AreEqual(appLockFailWhileLocked.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.FailedDueToTimeout);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLockFailWhileLocked.LockName));

            //Force Release the Lock!
            await sqlTrans.RollbackAsync();

            //Attempt Reacquisition of the Lock Once Released!
            //Get a new Transaction to test re-acquisition!
            await using var sqlConnAfterRelease = SqlConnectionHelper.CreateMicrosoftDataSqlConnection();
            await sqlConnAfterRelease.OpenAsync();

            var sqlTransAfterRelease = (SqlTransaction)await sqlConnAfterRelease.BeginTransactionAsync();
            await using var appLockAfterRelease = await sqlTransAfterRelease.AcquireAppLockAsync(
                nameof(TestSystemDataTransactionAppLock),
                acquisitionTimeoutSeconds: 3,
                throwsException: false
            );

            Assert.IsNotNull(appLockAfterRelease);
            Assert.AreEqual(appLockAfterRelease.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLockAfterRelease.LockName));
        }

        [TestMethod]
        public async Task TestAsyncTransactionAppLockAcquisitionWithExceptions()
        {
            await using var sqlConn = SqlConnectionHelper.CreateMicrosoftDataSqlConnection();
            await sqlConn.OpenAsync();

            await using var sqlTrans = (SqlTransaction)await sqlConn.BeginTransactionAsync();

            //Acquire the Lock & Validate
            await using var appLock = await sqlTrans.AcquireAppLockAsync(nameof(TestSystemDataTransactionAppLock));

            Assert.IsNotNull(appLock);
            Assert.AreEqual(appLock.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLock.LockName));

            //Attempt Acquisition from SECOND Connection Once Locked & Validate...
            try
            {
                await using var sqlConnWhileLocked = SqlConnectionHelper.CreateMicrosoftDataSqlConnection();
                await sqlConnWhileLocked.OpenAsync();

                await using var sqlTransWhileLocked = (SqlTransaction)await sqlConnWhileLocked.BeginTransactionAsync();
                await using var appLockFailWhileLocked = await sqlTransWhileLocked.AcquireAppLockAsync(
                    nameof(TestSystemDataTransactionAppLock),
                    1
                );

                //SHOULD NOT REACH THIS CODE DUE TO EXCEPTION!
                Assert.IsNull(appLockFailWhileLocked);
            }
            catch (SqlServerAppLockAcquisitionException appLockException)
            {
                Assert.IsNotNull(appLockException);
                Assert.AreEqual(appLockException.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.FailedDueToTimeout);
            }
        }

        [TestMethod]
        public void TestSyncTransactionAppLockAcquisitionExceptionsDisabled()
        {
            using var sqlConn = SqlConnectionHelper.CreateMicrosoftDataSqlConnection();
            sqlConn.Open();

            using var sqlTrans = (SqlTransaction)sqlConn.BeginTransaction();

            //Acquire the Lock & Validate
            using var appLock = sqlTrans.AcquireAppLock(nameof(TestSystemDataTransactionAppLock), 3, false);

            Assert.IsNotNull(appLock);
            Assert.AreEqual(appLock.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLock.LockName));

            //Attempt Acquisition from SECOND Connection Once Locked & Validate...
            using var sqlConnWhileLocked = SqlConnectionHelper.CreateMicrosoftDataSqlConnection();
            sqlConnWhileLocked.Open();

            using var sqlTransWhileLocked = (SqlTransaction)sqlConnWhileLocked.BeginTransaction();
            using var appLockFailWhileLocked = sqlTransWhileLocked.AcquireAppLock(
                nameof(TestSystemDataTransactionAppLock),
                throwsException: false
            );

            Assert.IsNotNull(appLockFailWhileLocked);
            Assert.AreEqual(appLockFailWhileLocked.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.FailedDueToTimeout);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLockFailWhileLocked.LockName));

            //Force Release the Lock!
            sqlTrans.Rollback();

            //Attempt Reacquisition of the Lock Once Released!
            //Get a new Transaction to test re-acquisition!
            using var sqlConnAfterRelease = SqlConnectionHelper.CreateMicrosoftDataSqlConnection();
            sqlConnAfterRelease.Open();

            var sqlTransAfterRelease = (SqlTransaction)sqlConnAfterRelease.BeginTransaction();
            using var appLockAfterRelease = sqlTransAfterRelease.AcquireAppLock(
                nameof(TestSystemDataTransactionAppLock),
                acquisitionTimeoutSeconds: 3,
                throwsException: false
            );

            Assert.IsNotNull(appLockAfterRelease);
            Assert.AreEqual(appLockAfterRelease.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLockAfterRelease.LockName));
        }

        [TestMethod]
        public void TestSyncTransactionAppLockAcquisitionWithExceptions()
        {
            using var sqlConn = SqlConnectionHelper.CreateMicrosoftDataSqlConnection();
            sqlConn.Open();

            using var sqlTrans = (SqlTransaction)sqlConn.BeginTransaction();

            //Acquire the Lock & Validate
            using var appLock = sqlTrans.AcquireAppLock(nameof(TestSystemDataTransactionAppLock));

            Assert.IsNotNull(appLock);
            Assert.AreEqual(appLock.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLock.LockName));

            //Attempt Acquisition from SECOND Connection Once Locked & Validate...
            try
            {
                using var sqlConnWhileLocked = SqlConnectionHelper.CreateMicrosoftDataSqlConnection();
                sqlConnWhileLocked.Open();

                using var sqlTransWhileLocked = (SqlTransaction)sqlConnWhileLocked.BeginTransaction();
                using var appLockFailWhileLocked = sqlTransWhileLocked.AcquireAppLock(nameof(TestSystemDataTransactionAppLock));

                //SHOULD NOT REACH THIS CODE DUE TO EXCEPTION!
                Assert.IsNull(appLockFailWhileLocked);
            }
            catch (SqlServerAppLockAcquisitionException appLockException)
            {
                Assert.IsNotNull(appLockException);
                Assert.AreEqual(appLockException.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.FailedDueToTimeout);
            }
        }
    }
}
