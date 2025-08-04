using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Reflection.Tests;

public class EFFacts
{
    private readonly TypeService _typeService;

    public EFFacts()
    {
        _typeService = new TypeService();
    }

    [Fact]
    public async Task QueriesPocoTypes()
    {
        var options = new DbContextOptionsBuilder<DynamicDbContext>()
            .UseInMemoryDatabase("DynamicDbTest")
            .Options;

        var dynamicType = _typeService.GetClass(new ClassDefinition<SampleClass>());

        // Act
        using (var context = new DynamicDbContext(options, new[] { dynamicType }))
        {
            context.Database.EnsureCreated();


            var entity = Activator.CreateInstance(dynamicType);
            Assert.NotNull(entity);
            dynamicType.GetProperty("Id")!.SetValue(entity, 27);
            dynamicType.GetProperty("Name")!.SetValue(entity, "Queries Poco Types");
            context.Add(entity);
            context.SaveChanges();
        }

        object? fetched;
        using (var context = new DynamicDbContext(options, new[] { dynamicType }))
        {
            var set = (IQueryable)typeof(DbContext)
                .GetMethod("Set", Type.EmptyTypes)!
                .MakeGenericMethod(dynamicType)
                .Invoke(context, null)!;

            fetched = set.Cast<IIdentity<long>>().FirstOrDefault(e => e.Id == 27);
        }

        // Assert
        Assert.NotNull(fetched);
        Assert.Equal(27, (long)dynamicType.GetProperty("Id")!.GetValue(fetched)!);
        Assert.Equal("Queries Poco Types", dynamicType.GetProperty("Name")!.GetValue(fetched));
    }


    public class DynamicDbContext : DbContext
    {
        private readonly IEnumerable<Type> _dynamicTypes;

        public DynamicDbContext(DbContextOptions options, IEnumerable<Type> dynamicTypes)
            : base(options)
        {
            _dynamicTypes = dynamicTypes;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            foreach (var type in _dynamicTypes)
            {
                var entity = modelBuilder.Entity(type);
                var idProp = type.GetProperty("Id");
                if (idProp != null)
                {
                    entity.HasKey("Id");
                }
            }
        }
    }

    public class SampleClass: IIdentity<long>
    {
        public long Id { get; set; }
        public string? Name { get; set; }
    }

    public interface IIdentity<T>
    {
        T Id { get; set; }
    }
}

