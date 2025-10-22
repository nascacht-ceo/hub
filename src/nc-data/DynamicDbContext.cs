using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using nc.Hub;
using nc.Reflection;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace nc.Data;

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
			var entity = modelBuilder.Entity(type).ToTable(definition.ModelName.Value[7..], "Person");
			var keys = definition.Properties.Where(p => p.IsKey).Select(p => p.Name);
			if (keys == null || !keys.Any())
				entity.HasNoKey();
			else
				entity.HasKey([.. keys]);


			foreach (var prop in type.GetProperties())
			{
				if (prop.PropertyType == typeof(object))
				{
					modelBuilder.Entity(type).Ignore(prop.Name);
				}
				if (ValueConverters.TryGetValue(prop.PropertyType, out ValueConverter? value))
					modelBuilder.Entity(type).Property(prop.Name).HasConversion(value);

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

