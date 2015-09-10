NJsonSchema for .NET
====================

[![Build status](https://ci.appveyor.com/api/projects/status/pextintxxmn5xt46?svg=true)](https://ci.appveyor.com/project/rsuter/njsonschema)
[![NuGet Version](http://img.shields.io/nuget/v/NJsonSchema.svg?style=flat)](https://www.nuget.org/packages?q=njsonschema)

JSON Schema draft v4 reader, generator and validator for .NET

**NuGet packages:** 
-   [NJsonSchema](https://www.nuget.org/packages/NJsonSchema): JSON Schema 4 validation and parsing classes
-   [NJsonSchema.CodeGeneration](https://www.nuget.org/packages/NJsonSchema.CodeGeneration): Classes to generate code from a JSON Schema 4 (C# and TypeScript)

The library uses [Json.NET](http://james.newtonking.com/json) to read and write JSON data. 

## NJsonSchema usage

The `JsonSchema4` type can be used as follows: 

    var schema = JsonSchema4.FromType<Person>();
    var schemaData = schema.ToJson();

    var jsonToken = JToken.Parse("...");
    var errors = schema.Validate(jsonToken);

    foreach (var error in errors)
        Console.WriteLine(error.Path + ": " + error.Kind);

    schema = JsonSchema4.FromJson(schemaData);

The `Person` class: 

    public class Person
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        public Gender Gender { get; set; }

        public DateTime Birthday { get; set; }

        public Collection<Job> Jobs { get; set; }

        [Range(2, 5)]
        public int Test { get; set; }
    }

    public class Job
    {
        public string Company { get; set; }
    }

    public enum Gender
    {
        Male, 
        Female
    }
  
The generated JSON schema data stored in the `schemaData` variable: 
  
    {
      "$schema": "http://json-schema.org/draft-04/schema#",
      "title": "Person",
      "type": "object",
      "required": [
        "FirstName",
        "LastName",
        "Birthday",
        "Gender",
        "Test"
      ],
      "properties": {
        "FirstName": {
          "type": "string"
        },
        "LastName": {
          "type": "string"
        },
        "Gender": {
          "type": "string",
          "enum": [
            "Male",
            "Female"
          ]
        },
        "Birthday": {
          "format": "date-time",
          "type": "string"
        },
        "Jobs": {
          "items": {
            "title": "Job",
            "type": "object",
            "properties": {
              "Company": {
                "type": "string"
              }
            }
          },
          "type": "array"
        },
        "Test": {
          "maximum": 5.0,
          "minimum": 2.0,
          "type": "integer"
        }
      }
    }

## NJsonSchema.CodeGeneration usage

The `NJsonSchema.CodeGeneration` can be used to generate code from a JSON schema.

    var generator new CSharpClassGenerator(schema);
    var file = generator.GenerateFile();
    
The `file` variable now contains the C# code for all the classes defined in the JSON schema. 

## Final notes

Applications which use the library: 

- [VisualJsonEditor](http://visualjsoneditor.org), a JSON schema based file editor for Windows. 
- [NSwag](http://nswag.org): The Swagger API toolchain for .NET
