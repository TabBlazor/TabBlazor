using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

const string ComponentBaseName = "Microsoft.AspNetCore.Components.ComponentBase";
const string ParameterAttr = "Microsoft.AspNetCore.Components.ParameterAttribute";
const string CascadingParameterAttr = "Microsoft.AspNetCore.Components.CascadingParameterAttribute";
const string EditorRequiredAttr = "Microsoft.AspNetCore.Components.EditorRequiredAttribute";

string? assemblyPath = null, xmlPath = null, outputPath = null;
for (var i = 0; i < args.Length - 1; i++)
{
    switch (args[i])
    {
        case "--assembly": assemblyPath = args[i + 1]; break;
        case "--xmldoc": xmlPath = args[i + 1]; break;
        case "--output": outputPath = args[i + 1]; break;
    }
}

if (assemblyPath is null || outputPath is null)
{
    Console.Error.WriteLine("usage: ComponentApiGenerator --assembly <dll> [--xmldoc <xml>] --output <json>");
    return 1;
}

assemblyPath = Path.GetFullPath(assemblyPath);
var docs = LoadXmlDocs(xmlPath);

var resolver = new AssemblyDependencyResolver(assemblyPath);
var alc = new AssemblyLoadContext("componentapi", isCollectible: false);
alc.Resolving += (ctx, name) =>
{
    var path = resolver.ResolveAssemblyToPath(name);
    return path is null ? null : ctx.LoadFromAssemblyPath(path);
};

var assembly = alc.LoadFromAssemblyPath(assemblyPath);

var components = new List<ComponentInfo>();
foreach (var type in assembly.GetExportedTypes())
{
    if (!IsComponent(type)) continue;

    var parameters = new List<ParameterInfo>();
    var seen = new HashSet<string>();
    var instance = TryCreateInstance(type);

    foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
    {
        if (!seen.Add(prop.Name)) continue;

        var attrs = prop.GetCustomAttributesData();
        var parameterAttr = attrs.FirstOrDefault(a => a.AttributeType.FullName == ParameterAttr);
        var cascadingAttr = attrs.FirstOrDefault(a => a.AttributeType.FullName == CascadingParameterAttr);
        if (parameterAttr is null && cascadingAttr is null) continue;

        var captureUnmatched = parameterAttr?.NamedArguments
            .Any(a => a.MemberName == "CaptureUnmatchedValues" && a.TypedValue.Value is true) ?? false;

        parameters.Add(new ParameterInfo
        {
            Name = prop.Name,
            Type = TypeName(prop.PropertyType),
            Required = attrs.Any(a => a.AttributeType.FullName == EditorRequiredAttr),
            Cascading = cascadingAttr is not null,
            CaptureUnmatchedValues = captureUnmatched ? true : null,
            Default = TryGetDefault(prop, instance),
            InheritedFrom = prop.DeclaringType != type ? DisplayName(prop.DeclaringType!) : null,
            Summary = docs.GetValueOrDefault(DocId('P', prop.DeclaringType!, prop.Name)),
        });
    }

    if (instance is IDisposable disposable)
    {
        try { disposable.Dispose(); } catch { }
    }

    components.Add(new ComponentInfo
    {
        Name = DisplayName(type),
        FullName = type.FullName ?? type.Name,
        Namespace = type.Namespace ?? string.Empty,
        IsAbstract = type.IsAbstract,
        Summary = docs.GetValueOrDefault(DocId('T', type, null)),
        Parameters = parameters.OrderBy(p => p.Cascading).ThenBy(p => p.Name, StringComparer.Ordinal).ToList(),
    });
}

components = components.OrderBy(c => c.FullName, StringComparer.Ordinal).ToList();

var manifest = new Manifest
{
    Schema = "tabblazor-component-api/v1",
    Library = assembly.GetName().Name ?? "TabBlazor",
    Version = assembly.GetName().Version?.ToString(),
    GeneratedFrom = Path.GetFileName(assemblyPath),
    ComponentCount = components.Count,
    Components = components,
};

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
File.WriteAllText(outputPath, JsonSerializer.Serialize(manifest, options));
Console.WriteLine($"Wrote {components.Count} components to {outputPath}");
return 0;

static bool IsComponent(Type type)
{
    if (!type.IsClass) return false;
    for (var t = type.BaseType; t is not null; t = t.BaseType)
    {
        if (t.FullName == ComponentBaseName) return true;
    }
    return false;
}

static object? TryCreateInstance(Type type)
{
    if (type.IsAbstract || type.IsGenericTypeDefinition) return null;
    if (type.GetConstructor(Type.EmptyTypes) is null) return null;
    try { return Activator.CreateInstance(type); }
    catch { return null; }
}

static object? TryGetDefault(PropertyInfo prop, object? instance)
{
    if (instance is null || prop.GetMethod is null) return null;

    var underlying = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
    var serializable = underlying.IsEnum
        || underlying == typeof(string)
        || underlying == typeof(bool)
        || (underlying.IsPrimitive && underlying != typeof(IntPtr) && underlying != typeof(UIntPtr));
    if (!serializable) return null;

    object? value;
    try { value = prop.GetValue(instance); }
    catch { return null; }
    if (value is null) return null;

    return underlying.IsEnum ? value.ToString() : value;
}

static string DisplayName(Type type)
{
    if (!type.IsGenericType && !type.IsGenericTypeDefinition) return type.Name;
    var name = type.Name;
    var tick = name.IndexOf('`');
    if (tick > 0) name = name[..tick];
    var args = string.Join(", ", type.GetGenericArguments().Select(TypeName));
    return $"{name}<{args}>";
}

static string TypeName(Type type)
{
    if (type.IsGenericParameter) return type.Name;

    var nullable = Nullable.GetUnderlyingType(type);
    if (nullable is not null) return TypeName(nullable) + "?";

    if (type.IsArray) return TypeName(type.GetElementType()!) + "[]";

    if (type.IsGenericType)
    {
        var name = type.Name;
        var tick = name.IndexOf('`');
        if (tick > 0) name = name[..tick];
        var args = string.Join(", ", type.GetGenericArguments().Select(TypeName));
        return $"{name}<{args}>";
    }

    return type.FullName switch
    {
        "System.String" => "string",
        "System.Boolean" => "bool",
        "System.Int32" => "int",
        "System.Int64" => "long",
        "System.Double" => "double",
        "System.Single" => "float",
        "System.Decimal" => "decimal",
        "System.Object" => "object",
        _ => type.Name,
    };
}

static string DocId(char prefix, Type declaringType, string? member)
{
    var typeId = declaringType.FullName ?? declaringType.Name;
    return member is null ? $"{prefix}:{typeId}" : $"{prefix}:{typeId}.{member}";
}

static Dictionary<string, string> LoadXmlDocs(string? path)
{
    var result = new Dictionary<string, string>(StringComparer.Ordinal);
    if (string.IsNullOrEmpty(path) || !File.Exists(path)) return result;

    var doc = XDocument.Load(path);
    foreach (var member in doc.Descendants("member"))
    {
        var name = member.Attribute("name")?.Value;
        var summary = member.Element("summary");
        if (name is null || summary is null) continue;

        var text = string.Concat(summary.Nodes().Select(NodeText));
        text = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (text.Length > 0) result[name] = text;
    }
    return result;

    static string NodeText(XNode node) => node switch
    {
        XText t => t.Value,
        XElement e when e.Name == "see" => e.Attribute("cref")?.Value is { } cref
            ? cref.Contains('.') ? cref[(cref.LastIndexOf('.') + 1)..] : cref
            : e.Value,
        XElement e => e.Value,
        _ => string.Empty,
    };
}

sealed class Manifest
{
    [JsonPropertyName("$schema")] public string Schema { get; set; } = "";
    public string Library { get; set; } = "";
    public string? Version { get; set; }
    public string GeneratedFrom { get; set; } = "";
    public int ComponentCount { get; set; }
    public List<ComponentInfo> Components { get; set; } = new();
}

sealed class ComponentInfo
{
    public string Name { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Namespace { get; set; } = "";
    public bool IsAbstract { get; set; }
    public string? Summary { get; set; }
    public List<ParameterInfo> Parameters { get; set; } = new();
}

sealed class ParameterInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool Required { get; set; }
    public bool Cascading { get; set; }
    public bool? CaptureUnmatchedValues { get; set; }
    public object? Default { get; set; }
    public string? InheritedFrom { get; set; }
    public string? Summary { get; set; }
}
