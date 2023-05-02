using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LeaderboardBackend.Swagger;

// https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/2036#issuecomment-894015122
internal class RequiredNotNullableSchemaFilter : ISchemaFilter
{
	public void Apply(OpenApiSchema schema, SchemaFilterContext context)
	{
		if (schema.Properties is null)
		{
			return;
		}

		foreach ((string propertyName, OpenApiSchema property) in schema.Properties)
		{
			if (property.Reference != null)
			{

				MemberInfo? field = context.Type
					.GetMembers(BindingFlags.Public | BindingFlags.Instance)
					.FirstOrDefault(x => string.Equals(x.Name, propertyName, StringComparison.InvariantCultureIgnoreCase));

				if (field == null)
				{
					continue;
				}

				Type fieldType = field switch
				{
					FieldInfo fieldInfo => fieldInfo.FieldType,
					PropertyInfo propertyInfo => propertyInfo.PropertyType,
					_ => throw new NotSupportedException(),
				};

				property.Nullable = fieldType.IsValueType
					? Nullable.GetUnderlyingType(fieldType) != null // is not a Nullable<> type
					: !field.IsNonNullableReferenceType();
			}

			if (!property.Nullable)
			{
				schema.Required.Add(propertyName);
			}
		}
	}
}
