NJsonSchema for .NET
====================

JSON Schema draft v4 reader, generator and validator for .NET

NuGet package: https://www.nuget.org/packages/NJsonSchema

This project is still in development, some features are not implemented yet. 

## Usage

The JsonSchema4 type can be used as follows: 

    var schema = JsonSchema4.FromType<Person>();
    var schemaJsonData = schema.ToJson();
    var errors = schema.Validate("...");

The schema class: 

    public class Person
    {
        [Required]
        public string FirstName { get; set; }
  
        [Required]
        public string LastName { get; set; }
  
        public DateTime Birthday { get; set; }
  
        public Collection<Job> Jobs { get; set; }
    }
  
    public class Job
    {
        public string Company { get; set; }
    }
  
The generated JSON data: 
  
    {
      "$schema": "http://json-schema.org/draft-04/schema#",
      "title": "Person",
      "type": "object",
      "required": [
        "FirstName",
        "LastName",
        "Birthday"
      ],
      "properties": {
        "FirstName": {
          "type": "string"
        },
        "LastName": {
          "type": "string"
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
        }
      }
    }
