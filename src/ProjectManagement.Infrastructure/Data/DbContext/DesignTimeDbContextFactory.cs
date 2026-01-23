using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ProjectManagement.Infrastructure.Data.DbContext;

namespace ProjectManagement.Infrastructure.Data.DbContext;

/// <summary>
/// Design-time factory for creating DbContext during migrations
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ProjectManagementDbContext>
{
    public ProjectManagementDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ProjectManagementDbContext>();
        optionsBuilder.UseSqlite("Data Source=projectmanagement.db");

        return new ProjectManagementDbContext(optionsBuilder.Options);
    }
}
