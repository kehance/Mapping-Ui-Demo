using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;


namespace TestProject.Pages.MappingUi
{
    public class MapFieldsModel : PageModel
    {
        public Dictionary<string, string> Mappings { get; set; } = new Dictionary<string, string>();
        public List<string> TargetKeys { get; set; } = new List<string>();
        public List<JsonPropertyInfo>? SourceDataProperties { get; private set; }
        public List<JsonPropertyInfo>? SourceSchemaProperties { get; private set; }
        public List<JsonPropertyInfo>? TargetSchemaProperties { get; private set; }
        public class JsonPropertyInfo
        {
            public string? ObjectPath { get; set; }
            public string? PropertyPath { get; set; }
            public string? Name { get; set; }
            public string? Type { get; set; }
            public string? Value { get; set; }
            public string? ObjectType { get; set; }
            public List<JsonPropertyInfo>? NestedProperties { get; internal set; }
        }

        public void OnGet()
        {
            var sourceSchemaJson = TempData.Peek("SourceSchema") as string; // using Peek to retain TempData across requests
            var targetSchemaJson = TempData.Peek("TargetSchema") as string; 

            var sourceObject = JObject.Parse(sourceSchemaJson);
            var sourceSchemaProperties = new List<JsonPropertyInfo>();
            TraverseJson(sourceObject, sourceSchemaProperties);
            SourceSchemaProperties = sourceSchemaProperties;

            var targetObject = JObject.Parse(targetSchemaJson);
            var targetSchemaProperties = new List<JsonPropertyInfo>();
            TraverseJson(targetObject, targetSchemaProperties);
            TargetSchemaProperties = targetSchemaProperties;


            if (!string.IsNullOrEmpty(sourceSchemaJson) && !string.IsNullOrEmpty(targetSchemaJson))
            {
                //InitializePath(sourceSchemaProperties);
                //PopulateTargetPath(targetSchemaProperties);

                InitializeValue(sourceSchemaProperties);
                PopulateTargetValue(targetSchemaProperties);
            }

        }

        private void InitializePath(List<JsonPropertyInfo> properties, string prefix = "") // source path uesd to initialize mapping key
        {
            foreach (var property in properties)
            {
                var key = !string.IsNullOrEmpty(prefix) ? $"{prefix}.{property.Name}" : property.Name;
                Mappings[key] = ""; // initialize with empty or default mappings
                if (property.NestedProperties != null)
                {
                    InitializePath(property.NestedProperties, key);
                }
            }
        }

        private void PopulateTargetPath(List<JsonPropertyInfo> properties, string prefix = "") // target path selection
        {
            foreach (var property in properties)
            {
                var key = !string.IsNullOrEmpty(prefix) ? $"{prefix}.{property.Name}" : property.Name;
                TargetKeys.Add(key);
                if (property.NestedProperties != null)
                {
                    PopulateTargetPath(property.NestedProperties, key);
                }
            }
        }

        private void InitializeValue(List<JsonPropertyInfo> properties) // source field used to initialize mapping key
        {
            foreach (var property in properties)
            {
                Mappings[property.Name] = ""; 
                if (property.NestedProperties != null)
                {
                    InitializeValue(property.NestedProperties);
                }
            }
        }

        private void PopulateTargetValue(List<JsonPropertyInfo> properties) // target field selection
        {
            foreach (var property in properties)
            {
                TargetKeys.Add(property.Name);
                if (property.NestedProperties != null)
                {
                    PopulateTargetValue(property.NestedProperties);
                }
            }
        }

        // map all properites of JSON as list of JsonProperty objects
        private void TraverseJson(JToken token, List<JsonPropertyInfo> mappedProperties, string parentName = "")
        {
            foreach (var prop in token)
            {
                if (prop is JProperty)
                {
                    var property = (JProperty)prop;
                    var propertyName = string.IsNullOrEmpty(parentName) ? property.Name : $"{parentName}.{property.Name}";

                    if (property.Value is JObject) // if the property value is a nested object, recursively traverse it
                    {
                        var objectPath = string.IsNullOrEmpty(parentName) ? property.Name : $"{parentName}.{property.Name}";

                        var nestedObjectProperties = new List<JsonPropertyInfo>();
                        TraverseJson(property.Value, nestedObjectProperties, propertyName);

                        // add the nested object properties as a single property
                        mappedProperties.Add(new JsonPropertyInfo
                        {
                            ObjectPath = objectPath,
                            PropertyPath = propertyName,
                            Name = property.Name,
                            Type = property.Value?.Type.ToString(), 
                            ObjectType = "Object", 
                            Value = "Object", 
                            NestedProperties = nestedObjectProperties 
                        });
                    }
                    else if (property.Value is JArray)
                    {
                        var objectPath = string.IsNullOrEmpty(parentName) ? property.Name : $"{parentName}";

                        var array = (JArray)property.Value;
                        if (array.Any() && array.First().Type == JTokenType.String) // if the array contains strings, save them as a single property

                        {
                            mappedProperties.Add(new JsonPropertyInfo
                            {
                                ObjectPath = objectPath,
                                PropertyPath = propertyName,
                                Name = property.Name,
                                Type = "Array of Strings",
                                ObjectType = property.Value?.Type.ToString(),
                                Value = string.Join(", ", array.Select(a => a.ToString()))
                            });
                        }
                        else // if the array contains nested objects or other types, recursively traverse each element
                        {
                            foreach (var item in array)
                            {
                                var nestedArrayObjectProperties = new List<JsonPropertyInfo>();
                                TraverseJson(item, nestedArrayObjectProperties, propertyName);
                                mappedProperties.AddRange(nestedArrayObjectProperties);
                            }
                        }
                    }
                    else // else add to list

                    {
                        var objectPath = string.IsNullOrEmpty(parentName) ? property.Name : $"{parentName}";
                        mappedProperties.Add(new JsonPropertyInfo
                        {
                            ObjectPath = objectPath,
                            PropertyPath = propertyName,
                            Name = property.Name,
                            Type = property.Value?.Type.ToString() ?? "Unknown",
                            ObjectType = "", // if object
                            Value = property.Value?.ToString() ?? ""
                        });
                    }
                }
            }
        }




        private void MapSourceDataToTargetProperties(List<JsonPropertyInfo> sourceDataProperties, Dictionary<string, object> resultData, string prefix = "")
        {
            foreach (var mapping in Mappings)
            {
                var sourceKey = mapping.Key;
                var targetKey = mapping.Value;

                // find the corresponding source property based on the mapping
                var sourceProperty = sourceDataProperties.FirstOrDefault(prop => prop.Name == sourceKey); // change prop.Name to prop.PropertyPath if searching by path
                if (sourceProperty != null && !string.IsNullOrEmpty(targetKey))
                {
                    // if the source property is found and the target key is not empty,
                    // map the value to the corresponding target property

                    // if the source property has nested properties, recursively map its properties
                    if (sourceProperty.NestedProperties != null && sourceProperty.NestedProperties.Any())
                    {
                        var nestedResultData = new Dictionary<string, object>();
                        MapSourceDataToTargetProperties(sourceProperty.NestedProperties, nestedResultData, $"{prefix}{targetKey}.");
                        resultData[targetKey] = nestedResultData;
                    }
                    else
                    {
                        // otherwise, map the value to the corresponding target property
                        resultData[targetKey] = sourceProperty.Value;
                    }
                }
            }
        }


        public IActionResult OnPost()
        {
            foreach (var key in Request.Form.Keys)
            {

                var actualKey = key.StartsWith("Mappings[") && key.EndsWith("]")
                    ? key.Substring("Mappings[".Length, key.Length - "Mappings[".Length - 1) : key;
                Mappings[actualKey] = Request.Form[key];
            }

            /*var sourceDataJson = TempData["SourceData"] as string;
            var sourceData = JsonConvert.DeserializeObject<Dictionary<string, object>>(sourceDataJson);*/
            var sourceDataJson = TempData.Peek("SourceData") as string; 
            var sourceDataObject = JObject.Parse(sourceDataJson);
            var sourceDataProperties = new List<JsonPropertyInfo>();
            TraverseJson(sourceDataObject, sourceDataProperties);
            SourceDataProperties = sourceDataProperties;

            var resultData = new Dictionary<string, object>();

            //map source data to selected target properties based on user selected fields
            MapSourceDataToTargetProperties(SourceDataProperties, resultData);


            // serialize the result data to JSON and save it in TempData
            var outputJson = JsonConvert.SerializeObject(resultData);
            TempData["OutputJson"] = outputJson;

            return RedirectToPage("Result");
        }
    }




}
