using Microsoft.EntityFrameworkCore;
using NetPad.Embedded;

namespace NetPad.Apps.Common.Tests.Data.EntityFrameworkCore;

public class EntityFrameworkDatabaseUtilTests
{
    [Fact]
    public void ColumnClrType_NamesNullableValueTypesByTheirUnderlyingType()
    {
        using var context = new EmployeesContext();

        var structure = EntityFrameworkDatabaseUtil.GetDatabaseStructure(context);

        var clrTypes = structure.Schemas.Single().Tables.Single().Columns.ToDictionary(c => c.Name, c => c.ClrType);
        Assert.Equal("Int32", clrTypes[nameof(Employee.Id)]);
        Assert.Equal("String", clrTypes[nameof(Employee.Name)]);
        Assert.Equal("Int32?", clrTypes[nameof(Employee.ReportsTo)]);
        Assert.Equal("DateTime?", clrTypes[nameof(Employee.BirthDate)]);
    }

    private class EmployeesContext : DbContext
    {
        public DbSet<Employee> Employees => Set<Employee>();

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=:memory:");
        }
    }

    private class Employee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ReportsTo { get; set; }
        public DateTime? BirthDate { get; set; }
    }
}
