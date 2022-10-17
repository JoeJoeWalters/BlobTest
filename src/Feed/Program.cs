using Common;
using Bogus;
using Bogus.DataSets;
using Bogus.Extensions.UnitedKingdom;
using static Bogus.DataSets.Name;
using Newtonsoft.Json;

var fakeAddress = new Faker<Common.Address>("en_GB")
    .RuleFor(a => a.AddressLine1, (f, a) => f.Address.StreetAddress())
    .RuleFor(a => a.AddressLine2, (f, a) => f.Address.SecondaryAddress())
    .RuleFor(a => a.BuildingNumber, (f, a) => f.Address.BuildingNumber())
    .RuleFor(a => a.City, (f, a) => f.Address.City())
    .RuleFor(a => a.State, (f, a) => f.Address.County())
    .RuleFor(a => a.PostalCode, (f, a) => f.Address.ZipCode());

var fakePerson = new Faker<Common.Person>("en_GB")
    .RuleFor(p => p.Gender, f => f.PickRandom<Common.Gender>())
    .RuleFor(p => p.Name, (f, u) => f.Name.FirstName((Name.Gender)u.Gender))
    .RuleFor(p => p.Surname, (f, u) => f.Name.LastName((Name.Gender)u.Gender))
    .RuleFor(p => p.EMail, (f, u) => f.Internet.Email(u.Name, u.Surname));

while (!false)
{
    Common.Person person = fakePerson.Generate();
    person.Address = fakeAddress.Generate();

    HttpClient httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri("http://localhost:7162/api/person");
    HttpContent content = new StringContent(JsonConvert.SerializeObject(person));
    HttpResponseMessage HttpResponseMessage = httpClient.PostAsync("http://localhost:7162/api/person", content).Result;

    Thread.Sleep(1000);

    // https://github.com/JoshClose/CsvHelper/issues/347

}