using System;

namespace SqlAppLockHelper.Tests
{
    public record WorkerResult(int Id, SqlServerAppLock AppLock);
}
