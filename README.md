NJsonSchema for .NET
====================

[![NuGet Version](https://badge.fury.io/nu/njsonschema.svg)](https://www.nuget.org/packages?q=NJsonSchema)
[![Build status](https://ci.appveyor.com/api/projects/status/pextintxxmn5xt46?svg=true)](https://ci.appveyor.com/project/rsuter/njsonschema)
CI: [![Build status](https://ci.appveyor.com/api/projects/status/0n9hi0o61al5g2uu?svg=true)](https://ci.appveyor.com/project/rsuter/njsonschema-jlw0p)

NJsonSchema is a .NET library to read, generate and validate JSON Schema draft v4 schemas. The library can read a schema from a file or string and validate JSON data against them. A schema can also be generated from an existing .NET class. Using the code generation tools, it is also possible to generate C# and TypeScript classes or interfaces from a schema. 

**NuGet packages:** 
-   [NJsonSchema](https://www.nuget.org/packages/NJsonSchema): JSON Schema 4 validation and parsing classes
-   [NJsonSchema.CodeGeneration](https://www.nuget.org/packages/NJsonSchema.CodeGeneration): Classes to generate code from a JSON Schema 4 (C# and TypeScript)

The library uses [Json.NET](http://james.newtonking.com/json) to read and write JSON data. 

**Features:**

- Read existing JSON Schemas and validate JSON data
- Generate JSON Schema from .NET type via reflection (with support for many attributes/annotations)
- Support for schema references ($ref) (relative, URL and file)
- Generate C# and TypeScript code from JSON Schema

NJsonSchema is heavily used in [NSwag](http://nswag.org), a Swagger API toolchain for .NET which generates client code for Web API services.

## NJsonSchema usage

The `JsonSchema4` class can be used as follows: 

```cs
var schema = JsonSchema4.FromType<Person>();
var schemaData = schema.ToJson();
var errors = schema.Validate("{...}");

foreach (var error in errors)
    Console.WriteLine(error.Path + ": " + error.Kind);

schema = JsonSchema4.FromJson(schemaData);
```

The `Person` class: 

```cs
public class Person
{
    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    public Gender Gender { get; set; }

    [Range(2, 5)]
    public int NumberWithRange { get; set; }

    public DateTime Birthday { get; set; }

    public Company Company { get; set; }

    public Collection<Car> Cars { get; set; }
}

public enum Gender
{
    Male,
    Female
}

public class Car
{
    public string Name { get; set; }

    public Company Manufacturer { get; set; }
}

public class Company
{
    public string Name { get; set; }
}
```
  
The generated JSON schema data stored in the `schemaData` variable: 

```json
{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "type": "object",
  "typeName": "Person",
  "additionalProperties": false,
  "required": [
    "FirstName",
    "LastName"
  ],
  "properties": {
    "FirstName": {
      "type": "string"
    },
    "LastName": {
      "type": "string"
    },
    "Gender": {
      "type": "integer",
      "$ref": "#/definitions/Gender"
    },
    "NumberWithRange": {
      "type": "integer",
      "maximum": 5.0,
      "minimum": 2.0
    },
    "Birthday": {
      "type": "string",
      "format": "date-time"
    },
    "Company": {
      "type": [
        "null",
        "object"
      ],
      "$ref": "#/definitions/Company"
    },
    "Cars": {
      "type": [
        "array",
        "null"
      ],
      "items": {
        "type": "object",
        "typeName": "Car",
        "additionalProperties": false,
        "properties": {
          "Name": {
            "type": [
              "null",
              "string"
            ]
          },
          "Manufacturer": {
            "type": [
              "null",
              "object"
            ],
            "$ref": "#/definitions/Company"
          }
        }
      }
    }
  },
  "definitions": {
    "Gender": {
      "type": "integer",
      "typeName": "Gender",
      "enum": [
        0,
        1
      ],
      "enumNames": [
        "Male",
        "Female"
      ]
    },
    "Company": {
      "type": "object",
      "typeName": "Company",
      "additionalProperties": false,
      "properties": {
        "Name": {
          "type": [
            "null",
            "string"
          ]
        }
      }
    }
  }
}
```

## NJsonSchema.CodeGeneration usage

The `NJsonSchema.CodeGeneration` can be used to generate C# or TypeScript code from a JSON schema:

```cs
var generator = new CSharpGenerator(schema);
var file = generator.GenerateFile();
```
    
The `file` variable now contains the C# code for all the classes defined in the JSON schema. 

## Final notes

Applications which use the library: 

- [VisualJsonEditor](http://visualjsoneditor.org), a JSON schema based file editor for Windows. 
- [NSwag](http://nswag.org): The Swagger API toolchain for .NET
