using Common;
using Bogus;
using Bogus.DataSets;
using Bogus.Extensions;
using static Bogus.DataSets.Name;

var fakeAddress = new Faker<Common.Address>()
    .RuleFor(a => a.AddressLine1, (f, a) => f.Address.StreetAddress())
    .RuleFor(a => a.AddressLine2, (f, a) => f.Address.SecondaryAddress())
    .RuleFor(a => a.BuildingNumber, (f, a) => f.Address.BuildingNumber())
    .RuleFor(a => a.City, (f, a) => f.Address.City())
    .RuleFor(a => a.State, (f, a) => f.Address.State())
    .RuleFor(a => a.PostalCode, (f, a) => f.Address.ZipCode())
    .RuleFor(a => a.CountryCode, (f, a) => f.Address.CountryCode());

var fakePerson = new Faker<Common.Person>()
    .RuleFor(p => p.Gender, f => f.PickRandom<Common.Gender>())
    .RuleFor(p => p.Name, (f, u) => f.Name.FirstName((Name.Gender)u.Gender))
    .RuleFor(p => p.Surname, (f, u) => f.Name.LastName((Name.Gender)u.Gender))
    .RuleFor(p => p.EMail, (f, u) => f.Internet.Email(u.Name, u.Surname));

while (!false)
{
    Common.Person person = fakePerson.Generate();
    person.Address = fakeAddress.Generate();

    Thread.Sleep(1000);

}