NJsonSchema for .NET
====================

JSON Schema draft v4 reader, generator and validator for .NET

NuGet package: https://www.nuget.org/packages/NJsonSchema

The library uses [Json.NET](http://james.newtonking.com/json) to read and write JSON data. The project is still in development: Some features are not implemented yet. 

## Usage

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

        public Sex Sex { get; set; }

        public DateTime Birthday { get; set; }

        public Collection<Job> Jobs { get; set; }

        [Range(2, 5)]
        public int Test { get; set; }
    }

    public class Job
    {
        public string Company { get; set; }
    }

    public enum Sex
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
        "Sex",
        "Test"
      ],
      "properties": {
        "FirstName": {
          "type": "string"
        },
        "LastName": {
          "type": "string"
        },
        "Sex": {
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
