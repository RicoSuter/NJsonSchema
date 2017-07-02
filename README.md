NJsonSchema for .NET
====================

[![NuGet Version](https://img.shields.io/nuget/v/NJsonSchema.svg)](https://www.nuget.org/packages?q=NJsonSchema)
[![Build status](https://img.shields.io/appveyor/ci/rsuter/njsonschema.svg?label=build)](https://ci.appveyor.com/project/rsuter/njsonschema)
[![Build status](https://img.shields.io/appveyor/ci/rsuter/njsonschema-jlw0p.svg?label=CI+build)](https://ci.appveyor.com/project/rsuter/njsonschema-jlw0p)
[![Gitter](https://img.shields.io/badge/gitter-join%20chat-1dce73.svg)](https://gitter.im/NJsonSchema/NJsonSchema)
[![StackOverflow](https://img.shields.io/badge/questions-on%20StackOverflow-orange.svg?style=flat)](http://stackoverflow.com/questions/tagged/njsonschema)

NJsonSchema is a .NET library to read, generate and validate JSON Schema draft v4 schemas. The library can read a schema from a file or string and validate JSON data against it. A schema can also be generated from an existing .NET class. With the code generation APIs you can generate C# and TypeScript classes or interfaces from a schema. 

The library uses [Json.NET](http://james.newtonking.com/json) to read and write JSON data. 

**NuGet packages:** 
-   [NJsonSchema](https://www.nuget.org/packages/NJsonSchema) (.NET Standard 1.0 and .NET 4.0): JSON Schema 4 parsing, validation and generation classes
-   [NJsonSchema.CodeGeneration](https://www.nuget.org/packages/NJsonSchema.CodeGeneration) (.NET Standard 1.3): Base classes to generate code from a JSON Schema 4
-   [NJsonSchema.CodeGeneration.CSharp](https://www.nuget.org/packages/NJsonSchema.CodeGeneration.CSharp) (.NET Standard 1.3): Generates CSharp classes
-   [NJsonSchema.CodeGeneration.TypeScript](https://www.nuget.org/packages/NJsonSchema.CodeGeneration.TypeScript) (.NET Standard 1.3): Generates TypeScript interfaces or classes

The NuGet packages may require the **Microsoft.NETCore.Portable.Compatibility** package on .NET Core/UWP targets (if mscorlib is missing). 

CI NuGet Feed: https://www.myget.org/gallery/njsonschema-ci

**Features:**

- Read existing JSON Schemas and validate JSON data (`JsonSchema4.FromJsonAsync()`)
- Generate JSON Schema from .NET type via reflection (with support for many attributes/annotations) (`JsonSchema4.FromTypeAsync<MyType>()`)
- Generate JSON Schema from sample JSON data (`JsonSchema4.FromData()`)
- Support for schema references ($ref) (relative, URL and file)
- Generate C# and TypeScript code from JSON Schema
- Support for .NET Core (via PCL 259 / .NET Standard 1.0, also see [XML Documentation](https://github.com/NJsonSchema/NJsonSchema/wiki/XML-Documentation))

NJsonSchema is heavily used in [NSwag](http://nswag.org), a Swagger API toolchain for .NET which generates client code for Web API services. NSwag also provides command line tools to use the NJsonSchema's JSON Schema generator (command `types2swagger`). 

The project is developed and maintained by [Rico Suter](http://rsuter.com) and other contributors. 

## NJsonSchema usage

The [JsonSchema4](https://github.com/NJsonSchema/NJsonSchema/wiki/JsonSchema4) class can be used as follows: 

```cs
var schema = await JsonSchema4.FromTypeAsync<Person>();
var schemaData = schema.ToJson();
var errors = schema.Validate("{...}");

foreach (var error in errors)
    Console.WriteLine(error.Path + ": " + error.Kind);

schema = await JsonSchema4.FromJsonAsync(schemaData);
```

The `Person` class: 

```cs
public class Person
{
    [Required]
    public string FirstName { get; set; }

    public string MiddleName { get; set; }

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
  "title": "Person",
  "additionalProperties": false,
  "required": [
    "FirstName",
    "LastName"
  ],
  "properties": {
    "FirstName": {
      "type": "string"
    },
    "MiddleName": {
      "type": [
        "null",
        "string"
      ]
    },
    "LastName": {
      "type": "string"
    },
    "Gender": {
      "oneOf": [
        {
          "$ref": "#/definitions/Gender"
        }
      ]
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
      "oneOf": [
        {
          "$ref": "#/definitions/Company"
        },
        {
          "type": "null"
        }
      ]
    },
    "Cars": {
      "type": [
        "array",
        "null"
      ],
      "items": {
        "$ref": "#/definitions/Car"
      }
    }
  },
  "definitions": {
    "Gender": {
      "type": "integer",
      "x-enumNames": [
        "Male",
        "Female"
      ],
      "enum": [
        0,
        1
      ],
      "description": ""
    },
    "Company": {
      "type": "object",
      "additionalProperties": false,
      "properties": {
        "Name": {
          "type": [
            "null",
            "string"
          ]
        }
      }
    },
    "Car": {
      "type": "object",
      "additionalProperties": false,
      "properties": {
        "Name": {
          "type": [
            "null",
            "string"
          ]
        },
        "Manufacturer": {
          "oneOf": [
            {
              "$ref": "#/definitions/Company"
            },
            {
              "type": "null"
            }
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

The previously generated JSON Schema would generate the following TypeScript code: 

```typescript
export interface Person {
    FirstName: string;
    MiddleName?: string;
    LastName: string;
    Gender?: GenderAsInteger;
    NumberWithRange?: number;
    Birthday?: Date;
    Company?: Company;
    Cars?: Car[];
}

export enum GenderAsInteger
{
    Male = 0, 
    Female = 1, 
}

export interface Company {
    Name?: string;
}

export interface Car {
    Name?: string;
    Manufacturer?: Company;
}
```

## Final notes

Applications which use the library: 

- [VisualJsonEditor](http://visualjsoneditor.org), a JSON schema based file editor for Windows. 
- [NSwag](http://nswag.org): The Swagger API toolchain for .NET
