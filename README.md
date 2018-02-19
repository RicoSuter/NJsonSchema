NJsonSchema for .NET
====================

## PageUp Fork.
This PageUp for is to make it compatable with Newtonsoft.Json 6.0.8 for the monolth.
This is done by multi-targetting the project.


[![Gitter](https://img.shields.io/badge/gitter-join%20chat-1dce73.svg)](https://gitter.im/NJsonSchema/NJsonSchema)
[![StackOverflow](https://img.shields.io/badge/questions-on%20StackOverflow-orange.svg?style=flat)](http://stackoverflow.com/questions/tagged/njsonschema)

<img align="left" src="https://raw.githubusercontent.com/RSuter/NJsonSchema/master/assets/GitHubIcon.png">

NJsonSchema is a .NET library to read, generate and validate JSON Schema draft v4 schemas. The library can read a schema from a file or string and validate JSON data against it. A schema can also be generated from an existing .NET class. With the code generation APIs you can generate C# and TypeScript classes or interfaces from a schema. 

The library uses [Json.NET](http://james.newtonking.com/json) to read and write JSON data. 

**NuGet packages:** 
- [NJsonSchema](https://www.nuget.org/packages/NJsonSchema) (.NET Standard 1.0/.NET 4.5/.NET 4.0): JSON Schema 4 parsing, validation and generation classes
- [NJsonSchema.CodeGeneration](https://www.nuget.org/packages/NJsonSchema.CodeGeneration) (.NET Standard 1.3/.NET 4.5.1): Base classes to generate code from a JSON Schema 4
- [NJsonSchema.CodeGeneration.CSharp](https://www.nuget.org/packages/NJsonSchema.CodeGeneration.CSharp) (.NET Standard 1.3/.NET 4.5.1): Generates CSharp classes
- [NJsonSchema.CodeGeneration.TypeScript](https://www.nuget.org/packages/NJsonSchema.CodeGeneration.TypeScript) (.NET Standard 1.3/.NET 4.5.1): Generates TypeScript interfaces or classes

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
  "title": "Person",
  "type": "object",
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
      "format": "int32",
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
      "description": "",
      "x-enumNames": [
        "Male",
        "Female"
      ],
      "enum": [
        0,
        1
      ]
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

### TypeScript

The previously generated JSON Schema would generate the following TypeScript interfaces. 

**Settings:** 

    new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Interface, TypeScriptVersion = 2.0m }
    
**Output:** 

```typescript
export enum Gender {
    Male = 0, 
    Female = 1, 
}

export interface Company {
    Name: string | undefined;
}

export interface Car {
    Name: string | undefined;
    Manufacturer: Company | undefined;
}

export interface Person {
    FirstName: string;
    MiddleName: string | undefined;
    LastName: string;
    Gender: Gender;
    NumberWithRange: number;
    Birthday: Date;
    Company: Company | undefined;
    Cars: Car[] | undefined;
}
```

... and the following TypeScript classes. 

**Settings:** 

    new TypeScriptGeneratorSettings { TypeStyle = TypeScriptTypeStyle.Class, TypeScriptVersion = 2.0m }

**Output:**

```typescript
export enum Gender {
    Male = 0, 
    Female = 1, 
}

export class Company implements ICompany {
    name: string | undefined;

    constructor(data?: ICompany) {
        if (data) {
            for (var property in data) {
                if (data.hasOwnProperty(property))
                    (<any>this)[property] = (<any>data)[property];
            }
        }
    }

    init(data?: any) {
        if (data) {
            this.name = data["Name"];
        }
    }

    static fromJS(data: any): Company {
        let result = new Company();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        data["Name"] = this.name;
        return data; 
    }
}

export interface ICompany {
    name: string | undefined;
}

export class Car implements ICar {
    name: string | undefined;
    manufacturer: Company | undefined;

    constructor(data?: ICar) {
        if (data) {
            for (var property in data) {
                if (data.hasOwnProperty(property))
                    (<any>this)[property] = (<any>data)[property];
            }
        }
    }

    init(data?: any) {
        if (data) {
            this.name = data["Name"];
            this.manufacturer = data["Manufacturer"] ? Company.fromJS(data["Manufacturer"]) : <any>undefined;
        }
    }

    static fromJS(data: any): Car {
        let result = new Car();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        data["Name"] = this.name;
        data["Manufacturer"] = this.manufacturer ? this.manufacturer.toJSON() : <any>undefined;
        return data; 
    }
}

export interface ICar {
    name: string | undefined;
    manufacturer: Company | undefined;
}

export class Person implements IPerson {
    firstName: string;
    middleName: string | undefined;
    lastName: string;
    gender: Gender;
    numberWithRange: number;
    birthday: Date;
    company: Company | undefined;
    cars: Car[] | undefined;

    constructor(data?: IPerson) {
        if (data) {
            for (var property in data) {
                if (data.hasOwnProperty(property))
                    (<any>this)[property] = (<any>data)[property];
            }
        }
    }

    init(data?: any) {
        if (data) {
            this.firstName = data["FirstName"];
            this.middleName = data["MiddleName"];
            this.lastName = data["LastName"];
            this.gender = data["Gender"];
            this.numberWithRange = data["NumberWithRange"];
            this.birthday = data["Birthday"] ? new Date(data["Birthday"].toString()) : <any>undefined;
            this.company = data["Company"] ? Company.fromJS(data["Company"]) : <any>undefined;
            if (data["Cars"] && data["Cars"].constructor === Array) {
                this.cars = [];
                for (let item of data["Cars"])
                    this.cars.push(Car.fromJS(item));
            }
        }
    }

    static fromJS(data: any): Person {
        let result = new Person();
        result.init(data);
        return result;
    }

    toJSON(data?: any) {
        data = typeof data === 'object' ? data : {};
        data["FirstName"] = this.firstName;
        data["MiddleName"] = this.middleName;
        data["LastName"] = this.lastName;
        data["Gender"] = this.gender;
        data["NumberWithRange"] = this.numberWithRange;
        data["Birthday"] = this.birthday ? this.birthday.toISOString() : <any>undefined;
        data["Company"] = this.company ? this.company.toJSON() : <any>undefined;
        if (this.cars && this.cars.constructor === Array) {
            data["Cars"] = [];
            for (let item of this.cars)
                data["Cars"].push(item.toJSON());
        }
        return data; 
    }
}

export interface IPerson {
    firstName: string;
    middleName: string | undefined;
    lastName: string;
    gender: Gender;
    numberWithRange: number;
    birthday: Date;
    company: Company | undefined;
    cars: Car[] | undefined;
}
```

## Final notes

Applications which use the library: 

- [VisualJsonEditor](http://visualjsoneditor.org), a JSON schema based file editor for Windows. 
- [NSwag](http://nswag.org): The Swagger API toolchain for .NET
