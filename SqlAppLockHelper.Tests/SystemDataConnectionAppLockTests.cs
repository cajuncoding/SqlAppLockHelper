using System;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlAppLockHelper.SystemDataNS;

namespace SqlAppLockHelper.Tests
{
    [TestClass]
    public class TestSystemDataAppLock
    {
        [TestMethod]
        public async Task TestAsyncConnectionAppLockAcquisitionExceptionsDisabled()
        {
            await using var sqlConn = SqlConnectionHelper.CreateSystemDataSqlConnection();
            await sqlConn.OpenAsync();

            //Acquire the Lock & Validate
            await using var appLock = await sqlConn.AcquireAppLockAsync(
                nameof(TestSystemDataAppLock),
                acquisitionTimeoutSeconds: 1,
                throwsException: false
            );

            Assert.IsNotNull(appLock);
            Assert.AreEqual(appLock.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLock.LockName));

            ////Attempt Acquisition from SECOND Connection Once Locked & Validate...
            await using var sqlConnWhileLocked = SqlConnectionHelper.CreateSystemDataSqlConnection();
            await sqlConnWhileLocked.OpenAsync();

            await using var appLockFailWhileLocked = await sqlConnWhileLocked.AcquireAppLockAsync(
                nameof(TestSystemDataAppLock),
                acquisitionTimeoutSeconds: 3,
                throwsException: false
            );

            Assert.IsNotNull(appLockFailWhileLocked);
            Assert.AreEqual(appLockFailWhileLocked.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.FailedDueToTimeout);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLockFailWhileLocked.LockName));

            //Check App Lock Elapsed Timers!
            Assert.IsTrue(appLock.LockElapsedTime.TotalMilliseconds > 0);
            Assert.IsTrue(appLockFailWhileLocked.LockElapsedTime.TotalMilliseconds == 0);

            //Force Release the Lock!
            await appLock.DisposeAsync();

            //Attempt Reacquisition of the Lock Once Released!
            //Get a new Transaction to test re-acquisition!
            await using var sqlConnAfterRelease = SqlConnectionHelper.CreateSystemDataSqlConnection();
            await sqlConnAfterRelease.OpenAsync();

            await using var appLockAfterRelease = await sqlConnAfterRelease.AcquireAppLockAsync(
                nameof(TestSystemDataAppLock),
                throwsException: false
            );

            Assert.IsNotNull(appLockAfterRelease);
            Assert.AreEqual(appLockAfterRelease.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLockAfterRelease.LockName));
            Assert.IsTrue(appLockAfterRelease.LockElapsedTime.TotalMilliseconds > 0);
        }

        [TestMethod]
        public async Task TestAsyncConnectionAppLockAcquisitionWithExceptions()
        {
            await using var sqlConn = SqlConnectionHelper.CreateSystemDataSqlConnection();
            await sqlConn.OpenAsync();

            //Acquire the Lock & Validate
            await using var appLock = await sqlConn.AcquireAppLockAsync(nameof(TestSystemDataAppLock));

            Assert.IsNotNull(appLock);
            Assert.AreEqual(appLock.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLock.LockName));

            //Attempt Acquisition from SECOND Connection Once Locked & Validate...
            try
            {
                await using var sqlConnWhileLocked = SqlConnectionHelper.CreateSystemDataSqlConnection();
                await sqlConnWhileLocked.OpenAsync();

                await using var appLockFailWhileLocked = await sqlConnWhileLocked.AcquireAppLockAsync(
                    nameof(TestSystemDataAppLock),
                    acquisitionTimeoutSeconds: 1
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
        public async Task TestAsyncConnectionAppLockExplicitDisposalAsync()
        {
            await using var sqlConn = SqlConnectionHelper.CreateSystemDataSqlConnection();
            await sqlConn.OpenAsync();

            //Acquire the Lock & Validate
            await using var appLock = await sqlConn.AcquireAppLockAsync(nameof(TestSystemDataAppLock));

            Assert.IsNotNull(appLock);
            Assert.AreEqual(appLock.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLock.LockName));
 
            //Explicitly Release the AppLock & Validate
            await appLock.DisposeAsync();

            Assert.IsTrue(appLock.IsDisposed);
        }

        [TestMethod]
        public void TestAsyncConnectionAppLockExplicitDisposalSync()
        {
            using var sqlConn = SqlConnectionHelper.CreateSystemDataSqlConnection();
            sqlConn.Open();

            //Acquire the Lock & Validate
            using var appLock = sqlConn.AcquireAppLock(nameof(TestSystemDataAppLock));

            Assert.IsNotNull(appLock);
            Assert.AreEqual(appLock.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLock.LockName));

            //Explicitly Release the AppLock & Validate
            appLock.Dispose();

            Assert.IsTrue(appLock.IsDisposed);
        }

        [TestMethod]
        public async Task TestAsyncConnectionAppLockExplicitReleaseAsync()
        {
            await using var sqlConn = SqlConnectionHelper.CreateSystemDataSqlConnection();
            await sqlConn.OpenAsync();

            //Acquire the Lock & Validate
            await using var appLock = await sqlConn.AcquireAppLockAsync(nameof(TestSystemDataAppLock));

            Assert.IsNotNull(appLock);
            Assert.AreEqual(appLock.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLock.LockName));

            //Explicitly Release the AppLock & Validate
            await appLock.ReleaseAsync();

            await using var sqlConnAfterRelease = SqlConnectionHelper.CreateSystemDataSqlConnection();
            await sqlConnAfterRelease.OpenAsync();

            //Acquire the Lock & Validate
            await using var appLockAfterRelease = await sqlConnAfterRelease.AcquireAppLockAsync(nameof(TestSystemDataAppLock));

            Assert.IsNotNull(appLockAfterRelease);
            Assert.AreEqual(appLockAfterRelease.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLockAfterRelease.LockName));
        }

        [TestMethod]
        public void TestAsyncConnectionAppLockExplicitReleaseSync()
        {
            using var sqlConn = SqlConnectionHelper.CreateSystemDataSqlConnection();
            sqlConn.Open();
            
            //Acquire the Lock & Validate
            using var appLock = sqlConn.AcquireAppLock(nameof(TestSystemDataAppLock));

            Assert.IsNotNull(appLock);
            Assert.AreEqual(appLock.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLock.LockName));

            //Explicitly Release the AppLock & Validate
            appLock.Release();

            using var sqlConnAfterRelease = SqlConnectionHelper.CreateSystemDataSqlConnection();
            sqlConnAfterRelease.Open();

            //Acquire the Lock & Validate
            using var appLockAfterRelease = sqlConnAfterRelease.AcquireAppLock(nameof(TestSystemDataAppLock));

            Assert.IsNotNull(appLockAfterRelease);
            Assert.AreEqual(appLockAfterRelease.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLockAfterRelease.LockName));
        }

        [TestMethod]
        public async Task TestAsyncConnectionAppLockReleaseWithConnectionDisposalWithUsing()
        {
            await using (var sqlConn = SqlConnectionHelper.CreateSystemDataSqlConnection())
            {
                await sqlConn.OpenAsync();

                //Acquire the Lock & Validate but DO NOT DISPOSE of it in the current Scope!
                //await using var appLock = await sqlConn.AcquireAppLockAsync(
                var appLock = await sqlConn.AcquireAppLockAsync(
                    nameof(TestSystemDataAppLock),
                    acquisitionTimeoutSeconds: 1,
                    throwsException: false
                );

                Assert.IsNotNull(appLock);
                Assert.AreEqual(appLock.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
                Assert.IsFalse(string.IsNullOrWhiteSpace(appLock.LockName));

                ////Attempt Acquisition from SECOND Connection Once Locked & Validate...
                await using var sqlConnWhileLocked = SqlConnectionHelper.CreateSystemDataSqlConnection();
                await sqlConnWhileLocked.OpenAsync();

                await using var appLockFailWhileLocked = await sqlConnWhileLocked.AcquireAppLockAsync(
                    nameof(TestSystemDataAppLock),
                    acquisitionTimeoutSeconds: 1,
                    throwsException: false
                );

                Assert.IsNotNull(appLockFailWhileLocked);
                Assert.AreEqual(appLockFailWhileLocked.LockAcquisitionResult,
                    SqlServerAppLockAcquisitionResult.FailedDueToTimeout);
                Assert.IsFalse(string.IsNullOrWhiteSpace(appLockFailWhileLocked.LockName));
            }

            //Attempt Reacquisition of the Lock Once Released via Sql Connection Disposal (from using{} scope) above!
            //Get a new Transaction to test re-acquisition!
            await using var sqlConnAfterRelease = SqlConnectionHelper.CreateSystemDataSqlConnection();
            await sqlConnAfterRelease.OpenAsync();

            await using var appLockAfterRelease = await sqlConnAfterRelease.AcquireAppLockAsync(
                nameof(TestSystemDataAppLock),
                throwsException: false
            );

            Assert.IsNotNull(appLockAfterRelease);
            Assert.AreEqual(appLockAfterRelease.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLockAfterRelease.LockName));
        }
    }
}
