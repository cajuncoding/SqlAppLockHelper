using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SqlAppLockHelper
{
    public class SqlServerAppLock : IDisposable, IAsyncDisposable
    {
        private Func<ValueTask> _releaseActionAsync = null;
        private Action _releaseAction = null;

        public string LockName { get; }

        public SqlServerAppLockScope LockScope { get; }

        public SqlServerAppLockAcquisitionResult LockAcquisitionResult { get; }

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

            //Initialize Sync & Async callbacks for Disposal!
            _releaseAction = releaseAction;
            _releaseActionAsync = releaseActionAsync;
        }

        public bool IsDisposed { get; protected set; }

        public bool IsLockAcquired => LockAcquisitionResult == SqlServerAppLockAcquisitionResult.AcquiredImmediately
                                  || LockAcquisitionResult == SqlServerAppLockAcquisitionResult.AcquiredAfterRelease;

        public void Dispose()
        {
            if (!IsDisposed && _releaseAction != null)
            {
                if (_releaseAction != null && IsLockAcquired)
                {
                    _releaseAction.Invoke();
                    _releaseAction = null;
                }

                IsDisposed = true;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!IsDisposed && _releaseActionAsync != null)
            {
                if (_releaseAction != null && IsLockAcquired)
                {
                    await _releaseActionAsync.Invoke();
                    _releaseActionAsync = null;
                }

                IsDisposed = true;
            }
        }
    }
}
