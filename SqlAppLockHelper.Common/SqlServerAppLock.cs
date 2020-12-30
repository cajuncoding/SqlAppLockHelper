using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace SqlAppLockHelper
{
    public class SqlServerAppLock : IDisposable, IAsyncDisposable
    {
        //NOTE: Using Delegates here allows this class to be independent of Microsoft.Data/System.Data
        //      namespaces, reducing duplication.
        private Func<ValueTask> _releaseActionAsync = null;
        private Action _releaseAction = null;
        private Stopwatch _lockTimer = new Stopwatch();

        public string LockName { get; }

        public SqlServerAppLockScope LockScope { get; }

        public SqlServerAppLockAcquisitionResult LockAcquisitionResult { get; }

        public TimeSpan LockElapsedTime => _lockTimer.Elapsed;

        public SqlServerAppLock(
            string lockName, 
            SqlServerAppLockScope scope, 
            SqlServerAppLockAcquisitionResult lockAcquisitionResult,
            Action releaseAction,
            Func<ValueTask> releaseActionAsync)
        {
            if(string.IsNullOrWhiteSpace(lockName)) 
                throw new ArgumentNullException(nameof(lockName));

            LockName = lockName;
            LockScope = scope;
            LockAcquisitionResult = lockAcquisitionResult;

            //Start the Lock Timer ONLY if Lock was Acquired!
            if(this.IsLockAcquired)
                _lockTimer.Start();

            //Initialize Sync & Async callbacks for Disposal!
            //NOTE: Using Delegates here allows this class to be independent of Microsoft.Data/System.Data
            //      namespaces, reducing duplication.
            _releaseAction = releaseAction;
            _releaseActionAsync = releaseActionAsync;
        }

        public bool IsDisposed { get; protected set; }

        public bool IsLockAcquired => LockAcquisitionResult == SqlServerAppLockAcquisitionResult.AcquiredImmediately
                                  || LockAcquisitionResult == SqlServerAppLockAcquisitionResult.AcquiredAfterRelease;

        /// <summary>
        /// Explicitly release the Lock on demand asynchronously; also called when disposed asynchronously.
        /// This may error if no Lock is currently acquired, however if a lock is acquired then this method is
        /// idempotent and safe to call multiple times.
        /// </summary>
        /// <returns></returns>
        public async Task ReleaseAsync()
        {
            if (_releaseActionAsync != null)
            {
                await _releaseActionAsync.Invoke();
                _releaseActionAsync = null;

                _lockTimer.Stop();
            }
        }

        /// <summary>
        /// Explicitly release the Lock on demand; also called when disposed.
        /// This may error if no Lock is currently acquired, however if a lock is acquired then this method is
        /// idempotent and safe to call multiple times.
        /// </summary>
        public void Release()
        {
            if (_releaseAction != null)
            {
                _releaseAction.Invoke();
                _releaseAction = null;
                _lockTimer.Stop();
            }
        }

        /// <summary>
        /// Safely Dispose and release the lock.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) return;

            if (IsLockAcquired)
            {
                //NOTE: This is Safe/Idempotent to call...
                Release();
            }

            _lockTimer.Stop();
            IsDisposed = true;
        }

        /// <summary>
        /// Safely Dispose and release the lock asynchronously.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (IsDisposed) return;

            if (IsLockAcquired)
            {
                //NOTE: This is Safe/Idempotent to call...
                await ReleaseAsync();
            }

            _lockTimer.Stop();
            IsDisposed = true;
        }
    }
}
