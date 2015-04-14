using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace NJsonSchema.Demo.CodeGeneration
{
    public partial class Person : INotifyPropertyChanged
    {
        private string _firstName;
        private string _lastName;
        private DateTime _birthday;


        [JsonProperty("FirstName", Required = Required.Default)]
        public string FirstName
        {
            get { return _firstName; }
            set 
            {
                if (_firstName != value)
                {
                    _firstName = value; 
                    RaisePropertyChanged();
                }
            }
        }

        [JsonProperty("LastName", Required = Required.Default)]
        public string LastName
        {
            get { return _lastName; }
            set 
            {
                if (_lastName != value)
                {
                    _lastName = value; 
                    RaisePropertyChanged();
                }
            }
        }

        [JsonProperty("Birthday", Required = Required.Always)]
        public DateTime Birthday
        {
            get { return _birthday; }
            set 
            {
                if (_birthday != value)
                {
                    _birthday = value; 
                    RaisePropertyChanged();
                }
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public string ToJson() 
        {
            return JsonConvert.SerializeObject(this);
        }

        public static Person FromJson(string data)
        {
            return JsonConvert.DeserializeObject<Person>(data);
        }

        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) 
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}