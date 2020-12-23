using System;
using System.Security.Cryptography.X509Certificates;

namespace SqlAppLockHelper
{
    public enum SqlServerAppLockScope
    {
        Transaction,
        Session
    }
}
