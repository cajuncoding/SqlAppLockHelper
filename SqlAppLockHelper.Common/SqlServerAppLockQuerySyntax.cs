using System;
using System.Collections.Generic;
using System.Text;

namespace SqlAppLockHelper
{
    public class SqlServerLockScopeNames
    {
        public const string Transaction = "Transaction";
        public const string Session = "Session";
    }

    public class SqlServerLockModeNames
    {
        public const string Exclusive = "Exclusive";
    }

    public class SqlServerStoredProcNames
    {
        public const string AcquireLock = "dbo.sp_getapplock ";
        public const string ReleaseLock = "dbo.sp_releaseapplock";
    }

    public class SqlServerStoredParamNames
    {
        public const string Resource = "@Resource";
        public const string LockMode = "@LockMode";
        public const string LockOwner = "@LockOwner";
        public const string LockTimeout = "@LockTimeout";
        public const string ReturnValue = "@ReturnValue";
    }
}
