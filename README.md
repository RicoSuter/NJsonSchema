NJsonSchema for .NET
====================

[![Build status](https://ci.appveyor.com/api/projects/status/pextintxxmn5xt46?svg=true)](https://ci.appveyor.com/project/rsuter/njsonschema)
[![NuGet Version](http://img.shields.io/nuget/v/NJsonSchema.svg?style=flat)](https://www.nuget.org/packages?q=NJsonSchema)

JSON Schema draft v4 reader, generator and validator for .NET. 

**NuGet packages:** 
-   [NJsonSchema](https://www.nuget.org/packages/NJsonSchema): JSON Schema 4 validation and parsing classes
-   [NJsonSchema.CodeGeneration](https://www.nuget.org/packages/NJsonSchema.CodeGeneration): Classes to generate code from a JSON Schema 4 (C# and TypeScript)

The library uses [Json.NET](http://james.newtonking.com/json) to read and write JSON data. 

Features: 

- Read existing JSON Schemas and validate JSON data
- Generate JSON Schema from .NET type via reflection
- Support for schema references ($ref)
- Generate C# and TypeScript code from JSON Schema

NJsonSchema is heavily used in [NSwag](http://nswag.org), a Swagger API toolchain for .NET

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
  
The generated JSON schema data stored in the `schemaData` variable: 
  
	{
	  "$schema": "http://json-schema.org/draft-04/schema#",
	  "typeName": "Person",
	  "type": "object",
	  "required": [
		"FirstName",
		"LastName",
		"Gender",
		"NumberWithRange",
		"Birthday"
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
		  "typeName": "Company",
		  "type": "object",
		  "properties": {
			"Name": {
			  "type": "string"
			}
		  }
		},
		"Cars": {
		  "items": {
			"typeName": "Car",
			"type": "object",
			"properties": {
			  "Name": {
				"type": "string"
			  },
			  "Manufacturer": {
				"typeName": "Company",
				"type": "object",
				"$ref": "#/properties/Company"
			  }
			}
		  },
		  "type": "array"
		}
	  }
	}

## NJsonSchema.CodeGeneration usage

The `NJsonSchema.CodeGeneration` can be used to generate C# or TypeScript code from a JSON schema:

    var generator new CSharpClassGenerator(schema);
    var file = generator.GenerateFile();
    
The `file` variable now contains the C# code for all the classes defined in the JSON schema. 

## Final notes

Applications which use the library: 

- [VisualJsonEditor](http://visualjsoneditor.org), a JSON schema based file editor for Windows. 
- [NSwag](http://nswag.org): The Swagger API toolchain for .NET
