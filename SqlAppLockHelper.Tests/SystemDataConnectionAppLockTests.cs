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
            await using var sqlConn = TestHelper.CreateSystemDataSqlConnection();
            await sqlConn.OpenAsync();

            //Acquire the Lock & Validate
            await using var appLock = await sqlConn.AcquireAppLockAsync(
                nameof(TestSystemDataAppLock), 
                3, 
                false
            );

            Assert.IsNotNull(appLock);
            Assert.AreEqual(appLock.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLock.LockName));

            ////Attempt Acquisition from SECOND Connection Once Locked & Validate...
            await using var sqlConnWhileLocked = TestHelper.CreateSystemDataSqlConnection();
            await sqlConnWhileLocked.OpenAsync();

            await using var appLockFailWhileLocked = await sqlConnWhileLocked.AcquireAppLockAsync(
                nameof(TestSystemDataAppLock),
                1,
                false
            );

            Assert.IsNotNull(appLockFailWhileLocked);
            Assert.AreEqual(appLockFailWhileLocked.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.FailedDueToTimeout);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLockFailWhileLocked.LockName));

            //Force Release the Lock!
            await appLock.DisposeAsync();

            //Attempt Reacquisition of the Lock Once Released!
            //Get a new Transaction to test re-acquisition!
            await using var sqlConnAfterRelease = TestHelper.CreateSystemDataSqlConnection();
            await sqlConnAfterRelease.OpenAsync();

            await using var appLockAfterRelease = await sqlConnAfterRelease.AcquireAppLockAsync(
                nameof(TestSystemDataAppLock), 
                3,
                false
            );

            Assert.IsNotNull(appLockAfterRelease);
            Assert.AreEqual(appLockAfterRelease.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLockAfterRelease.LockName));
        }

        [TestMethod]
        public async Task TestAsyncConnectionAppLockAcquisitionWithExceptions()
        {
            await using var sqlConn = TestHelper.CreateSystemDataSqlConnection();
            await sqlConn.OpenAsync();

            //Acquire the Lock & Validate
            await using var appLock = await sqlConn.AcquireAppLockAsync(
                nameof(TestSystemDataAppLock),
                3
            );

            Assert.IsNotNull(appLock);
            Assert.AreEqual(appLock.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLock.LockName));

            //Attempt Acquisition from SECOND Connection Once Locked & Validate...
            try
            {
                await using var sqlConnWhileLocked = TestHelper.CreateSystemDataSqlConnection();
                await sqlConnWhileLocked.OpenAsync();

                await using var appLockFailWhileLocked = await sqlConnWhileLocked.AcquireAppLockAsync(
                    nameof(TestSystemDataAppLock), 
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
        public async Task TestAsyncConnectionAppLockExplicitRelease()
        {
            await using var sqlConn = TestHelper.CreateSystemDataSqlConnection();
            await sqlConn.OpenAsync();

            //Acquire the Lock & Validate
            await using var appLock = await sqlConn.AcquireAppLockAsync(
                nameof(TestSystemDataAppLock),
                3
            );

            Assert.IsNotNull(appLock);
            Assert.AreEqual(appLock.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLock.LockName));

 
            //Explicitly Release the AppLock & Validate
            await appLock.DisposeAsync();

            Assert.IsTrue(appLock.IsDisposed);
        }
    }
}
