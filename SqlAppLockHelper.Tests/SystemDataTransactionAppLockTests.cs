using System;
using System.Threading.Tasks;
using System.Data.SqlClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlAppLockHelper.SystemDataNS;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace SqlAppLockHelper.Tests
{
    [TestClass]
    public class TestSystemDataTransactionAppLock
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public async Task TestAsyncTransactionAppLockWaitInLineBlockingSupport()
        {
            const int waitSeconds = 60;
            const int workTimeSeconds = 2;

            async Task<WorkerResult> WorkerFuncAsync(int id)
            {
                TestContext.WriteLine($"Working ID [{id}] Waiting for Lock for up to [{waitSeconds}s]...");
                ////Attempt Lock Acquisition...
                await using var sqlConn = SqlConnectionHelper.CreateSystemDataSqlConnection();
                await sqlConn.OpenAsync();

                await using var sqlTrans = (SqlTransaction)await sqlConn.BeginTransactionAsync();

                await using var appLock = await sqlTrans.AcquireAppLockAsync(
                    nameof(TestSystemDataAppLock),
                    acquisitionTimeoutSeconds: waitSeconds,
                    throwsException: false
                );

                var waitTime = appLock.LockAcquisitionWaitTime;
                if (appLock.IsLockAcquired)
                {
                    TestContext.WriteLine($"Working ID [{id}] Successfully Acquired the Lock [{appLock.LockAcquisitionResult}] after waiting [{waitTime.TotalSeconds}s]...");
                    //Do Some Work for a couple seconds to distribute the wait times!
                    await Task.Delay(TimeSpan.FromSeconds(workTimeSeconds)).ConfigureAwait(false);
                }
                else
                {
                    TestContext.WriteLine($"Working ID [{id}] Failed to Acquire the Lock [{appLock.LockAcquisitionResult}] after waiting [{waitTime.TotalSeconds}s]...");
                }

                return new WorkerResult(id, appLock);
            }

            var workerResults = new ConcurrentBag<WorkerResult>();
            await Parallel.ForEachAsync(Enumerable.Range(0, 10), async (i, cancellationToken) =>
            {
                workerResults.Add(await WorkerFuncAsync(i).ConfigureAwait(false));
            }).ConfigureAwait(false);

            foreach (var workerResult in workerResults.OrderBy(wr => wr.AppLock.LockElapsedTime))
            {
                Assert.IsNotNull(workerResult);
                Assert.IsNotNull(workerResult.AppLock);
                Assert.IsTrue(workerResult.AppLock.IsLockAcquired);
                Assert.IsTrue(workerResult.AppLock.LockAcquisitionWaitTime.TotalMilliseconds > 0);
            }

            //Every Lock should have waiting a different/unique number of Seconds!
            Assert.AreEqual(
                workerResults.Count,
                workerResults.Select(r => (int)Math.Round(r.AppLock.LockAcquisitionWaitTime.TotalSeconds)).Distinct().Count()
            );
        }

        [TestMethod]
        public async Task TestAsyncTransactionAppLockAcquisitionExceptionsDisabled()
        {
            await using var sqlConn = SqlConnectionHelper.CreateSystemDataSqlConnection();
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
            await using var sqlConnWhileLocked = SqlConnectionHelper.CreateSystemDataSqlConnection();
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
            await using var sqlConnAfterRelease = SqlConnectionHelper.CreateSystemDataSqlConnection();
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
            await using var sqlConn = SqlConnectionHelper.CreateSystemDataSqlConnection();
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
                await using var sqlConnWhileLocked = SqlConnectionHelper.CreateSystemDataSqlConnection();
                await sqlConnWhileLocked.OpenAsync();

                await using var sqlTransWhileLocked = (SqlTransaction)await sqlConnWhileLocked.BeginTransactionAsync();
                await using var appLockFailWhileLocked = await sqlTransWhileLocked.AcquireAppLockAsync(
                    nameof(TestSystemDataTransactionAppLock),
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
        public void TestSyncTransactionAppLockAcquisitionExceptionsDisabled()
        {
            using var sqlConn = SqlConnectionHelper.CreateSystemDataSqlConnection();
            sqlConn.Open();

            using var sqlTrans = (SqlTransaction)sqlConn.BeginTransaction();

            //Acquire the Lock & Validate
            using var appLock = sqlTrans.AcquireAppLock(nameof(TestSystemDataTransactionAppLock), 3, false);

            Assert.IsNotNull(appLock);
            Assert.AreEqual(appLock.LockAcquisitionResult, SqlServerAppLockAcquisitionResult.AcquiredImmediately);
            Assert.IsFalse(string.IsNullOrWhiteSpace(appLock.LockName));

            //Attempt Acquisition from SECOND Connection Once Locked & Validate...
            using var sqlConnWhileLocked = SqlConnectionHelper.CreateSystemDataSqlConnection();
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
            using var sqlConnAfterRelease = SqlConnectionHelper.CreateSystemDataSqlConnection();
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
            using var sqlConn = SqlConnectionHelper.CreateSystemDataSqlConnection();
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
                using var sqlConnWhileLocked = SqlConnectionHelper.CreateSystemDataSqlConnection();
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
