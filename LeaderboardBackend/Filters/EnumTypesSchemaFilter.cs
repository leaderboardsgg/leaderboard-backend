using System.Xml.Linq;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LeaderboardBackend.Filters;

/// <summary>
/// Reflects the contents of <c>summary</c> tags in enum fields' doc comments
/// to the enum's schema definition in the Swagger doc. I.e. this class allows
/// us to describe an enum's values in its schema definition in the doc.
/// <br/>
/// Adapted from <see href="https://www.codeproject.com/Articles/5300099/Description-of-the-Enumeration-Members-in-Swashbuc"/>.
/// <br/>
/// </summary>
public class EnumTypesSchemaFilter(string xmlPath) : ISchemaFilter
{
    private readonly XDocument? _xmlComments =
        File.Exists(xmlPath) ? XDocument.Load(xmlPath) : null;

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (_xmlComments is null)
        {
            return;
        }

        if (schema.Enum != null && schema.Enum.Count > 0 &&
            context.Type != null && context.Type.IsEnum)
        {
            schema.Description += "<p>Members:</p><ul>";

            string? enumName = context.Type.FullName;

            foreach (string memberName in schema.Enum.OfType<OpenApiString>().Select(v => v.Value))
            {
                string fullName = $"F:{enumName}.{memberName}";

                XElement? memberComments = _xmlComments.Descendants("member")
                    .FirstOrDefault(m => m.Attribute("name")?.Value.Equals(
                        fullName, StringComparison.OrdinalIgnoreCase
                    ) ?? false);

                if (memberComments is null)
                {
                    continue;
                }

                XElement? summary = memberComments.Descendants("summary").FirstOrDefault();

                if (summary is null)
                {
                    continue;
                }

                schema.Description += $"<li><i>{memberName}</i> - {summary.Value.Trim()}</li>";
            }

            schema.Description += "</ul>";
        }
    }
}
