namespace NJsonSchema.CodeGeneration.CSharp
{
    public class JsonSerializerSettings
    {
        public Newtonsoft.Json.DateParseHandling? DateParseHandling { get; set; }

        public string DateFormatString { get; set; }

        override
        public string ToString() {
            var settingsString = DateParseHandling == null ? 
                                    string.Empty : 
                                    $"DateParseHandling = Newtonsoft.Json.DateParseHandling.{DateParseHandling.ToString()}";
            
            settingsString += string.IsNullOrEmpty(DateFormatString) ? 
                                    string.Empty : 
                                    $"DateFormatString = \"{DateFormatString}\"";

            return settingsString;
        }
    }
}
