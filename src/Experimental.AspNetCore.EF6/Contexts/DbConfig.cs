using System.Data.Entity;
using System.Data.Entity.SqlServer;

namespace Experimental.AspNetCore.EF6.Contexts
{
    public class DbConfig : DbConfiguration
    {
        public DbConfig()
        {
            SetProviderFactory("System.Data.SqlClient", System.Data.SqlClient.SqlClientFactory.Instance);
            SetProviderServices("System.Data.SqlClient", SqlProviderServices.Instance);
        }
    }
}
