using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.IdentityModel.Tokens;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace nc.Reflection.Tests;

public class EFFacts : IAsyncLifetime
{
    private readonly TypeService _typeService;
    private static string ConnectionString = "Server=(localdb)\\mssqllocaldb;Database=AdventureWorks;Trusted_Connection=True;";

    public EFFacts()
    {
        _typeService = new TypeService();
    }

    public async Task InitializeAsync()
    {
        var sqlReflection = new SqlReflection(ConnectionString);
        await foreach (var modelDefinition in sqlReflection.DiscoverModels("AdventureWorks", "Person", "%"))
        {
            var type = _typeService.GetModel(modelDefinition);
            Assert.NotNull(type);
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task QueriesPocoTypes()
    {
        var options = new DbContextOptionsBuilder<DynamicDbContext>()
            .UseInMemoryDatabase("DynamicDbTest")
            .Options;

        var sampleDefinition = new ModelDefinition<SampleClass>();
        sampleDefinition.Properties.First(p => p.Name == "Id").IsKey = true;
        var sampleType = _typeService.GetModel(sampleDefinition);

        // Act
        using (var context = new DynamicDbContext(options, _typeService, new[] { sampleDefinition }))
        {
            context.Database.EnsureCreated();


            var entity = Activator.CreateInstance(sampleType);
            Assert.NotNull(entity);
            sampleType.GetProperty("Id")!.SetValue(entity, 27);
            sampleType.GetProperty("Name")!.SetValue(entity, "Queries Poco Types");
            context.Add(entity);
            context.SaveChanges();
        }

        object? fetched;
        using (var context = new DynamicDbContext(options, _typeService, new[] { sampleDefinition }))
        {
            var set = (IQueryable)typeof(DbContext)
                .GetMethod("Set", Type.EmptyTypes)!
                .MakeGenericMethod(sampleType)
                .Invoke(context, null)!;

            fetched = set.Cast<IIdentity<long>>().FirstOrDefault(e => e.Id == 27);
        }

        // Assert
        Assert.NotNull(fetched);
        Assert.Equal(27, (long)sampleType.GetProperty("Id")!.GetValue(fetched)!);
        Assert.Equal("Queries Poco Types", sampleType.GetProperty("Name")!.GetValue(fetched));
    }

    [Fact]
    public async Task DiscoversTables()
    {
        Assert.NotEmpty(_typeService.GetTypes());
    }

    [Fact]
    public async Task QueriesTables()
    {
        var options = new DbContextOptionsBuilder<DynamicDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.UseNetTopologySuite())
            .Options;



        var types = _typeService.GetTypes("AdventureWorks").ToList();
        var addressType = types.First(t => t.Name == "Person_Address");
        using var context = new DynamicDbContext(options, _typeService, _typeService.GetModelDefinitions("AdventureWorks"));

        var set = (IQueryable)typeof(DbContext)
            .GetMethod("Set", Type.EmptyTypes)!
            .MakeGenericMethod(addressType)
            .Invoke(context, null)!;
        var items = set.Cast<object>().Take(100).ToList();
        Assert.Equal(100, items.Count);
    }

    public class DynamicDbContext : DbContext
    {
        private readonly ITypeService _typeService;
        private readonly IEnumerable<ModelDefinition> _classDefinitions;

        public DynamicDbContext(DbContextOptions options, ITypeService typeService, IEnumerable<ModelDefinition> classDefinitions)
            : base(options)
        {
            _typeService = typeService;
            _classDefinitions = classDefinitions;
        }

        public static IDictionary<Type, ValueConverter> ValueConverters { get; } = new Dictionary<Type, ValueConverter>
        {
            { typeof(XElement), new ValueConverter<XElement, string>(
                v => v.ToString(),
                v => XElement.Parse(v)) },
            { typeof(XDocument), new ValueConverter<XDocument, string>(
                v => v.ToString(),
                v => XDocument.Parse(v)) }
            //{ typeof(Point), new ValueConverter<Point, string>(
            //    v => v.ToString(),
            //    v => Point.Parse(v)) }
        };

        protected override void OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
        {
            foreach (var definition in _classDefinitions)
            {
                var type = _typeService.GetModel(definition);
                var entity = modelBuilder.Entity(type).ToTable(definition.ModelName.Value.Substring(7), "Person");
                var keys = definition.Properties.Where(p => p.IsKey).Select(p => p.Name);
                if (keys.IsNullOrEmpty())
                    entity.HasNoKey();
                else
                    entity.HasKey(keys.ToArray());


                foreach (var prop in type.GetProperties())
                {
                    if (prop.PropertyType == typeof(object))
                    {
                        modelBuilder.Entity(type).Ignore(prop.Name);
                    }
                    if (ValueConverters.ContainsKey(prop.PropertyType))
                        modelBuilder.Entity(type).Property(prop.Name).HasConversion(ValueConverters[prop.PropertyType]);

                    //if (prop.PropertyType == typeof(string))
                    //{
                    //    entity.Property(prop.Name).HasMaxLength(255);
                    //}
                    //else if (prop.PropertyType == typeof(int) || prop.PropertyType == typeof(long))
                    //{
                    //    entity.Property(prop.Name).IsRequired();
                    //}
                    //else if (prop.PropertyType == typeof(DateTime))
                    //{
                    //    entity.Property(prop.Name).HasColumnType("datetime2");
                    //}
                    if (prop.PropertyType == typeof(Point))
                    {
                        // entity.Property<Point>(prop.Name).HasColumnType("geography");
                        modelBuilder.Entity(type).Ignore(nameof(Point.UserData));
                    }
                }
            }

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.ClrType.GetProperties())
                {
                    if (property.PropertyType == typeof(Point))
                    {
                        modelBuilder.Entity(entityType.ClrType)
                            .Ignore(nameof(Point.UserData));
                    }
                }
            }
        }
    }

    public class SampleClass : IIdentity<long>
    {
        public long Id { get; set; }
        public string? Name { get; set; }
    }

    public interface IIdentity<T>
    {
        T Id { get; set; }
    }
}

