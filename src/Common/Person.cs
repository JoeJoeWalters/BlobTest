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
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public Gender? Gender { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string EMail { get; set; }
        public DateTime DOB { get; set; }
        public Address Address { get; set; } = new Address() { };
    }
}
