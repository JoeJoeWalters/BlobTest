using System;

namespace Common
{
    public enum Gender
    {
        Male,
        Female
    }

    public class Person
    {
        public string Account { get; set; } = string.Empty;
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public Gender? Gender { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string EMail { get; set; } = string.Empty;
        public DateTime DOB { get; set; } = DateTime.MinValue;
        public Address Address { get; set; } = new Address() { };
    }
}
