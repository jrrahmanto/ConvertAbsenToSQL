using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static WorkerGetDataAbsen.Model;

namespace WorkerGetDataAbsen
{
    public class Data : DbContext
    {
        private readonly IConfiguration _iConfiguration;
        private readonly string _connectionString;
        public Data()
        {
            var uri = new UriBuilder(Assembly.GetExecutingAssembly().CodeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            IConfigurationBuilder builder = new ConfigurationBuilder()
                        .SetBasePath(Path.GetDirectoryName(path))
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _iConfiguration = builder.Build();
            _connectionString = _iConfiguration.GetConnectionString("myconn");
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseSqlServer(_connectionString, providerOptions =>
            {
                providerOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                providerOptions.CommandTimeout(60000);
            })
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        }

        public DbSet<MMesinAbsen> MMesinAbsen { get; set; }
        public DbSet<MEmployee> MEmployee { get; set; }
        public DbSet<TAbsensi> TAbsensi { get; set; }
        public DbSet<TAbsenKhusus> TAbsenKhusus { get; set; }
        public DbSet<MHariLibur> MHariLibur { get; set; }
    }
}
