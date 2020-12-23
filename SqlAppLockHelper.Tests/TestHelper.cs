using System;
using System.Collections.Generic;
using System.Text;
using SystemData = System.Data.SqlClient;
using MicrosoftData = Microsoft.Data.SqlClient;

namespace SqlAppLockHelper.Tests
{
    public class TestHelper
    {
        public static SystemData.SqlConnection CreateSystemDataSqlConnection()
        {
            return new SystemData.SqlConnection(TestConfiguration.SqlConnectionString);
        }

        public static MicrosoftData.SqlConnection CreateMicrosoftDataSqlConnection()
        {
            return new MicrosoftData.SqlConnection(TestConfiguration.SqlConnectionString);
        }
    }
}
