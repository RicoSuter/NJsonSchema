using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace NJsonSchema.CodeGeneration.Tests.Models
{
    public class Person
    {
        [Required]
        public string FirstName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }
        
        [ReadOnly(true)]
        public DateTime Birthday { get; set; }

        public TimeSpan TimeSpan { get; set; }

        public Gender Gender { get; set; }
        
        public Address Address { get; set; }

        public List<string> Array { get; set; } 

        public Dictionary<string, int> Dictionary { get; set; } 
    }
}