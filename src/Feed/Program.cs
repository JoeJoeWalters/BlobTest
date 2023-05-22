using Bogus;
using Common;
using Newtonsoft.Json;

//var challengeMethods = new List<String>() { "OTP", "OOB", "BIO", "None" };

var fakeLog = new Faker<Log>("en_GB")
    .RuleFor(p => p.Account, f => f.Random.ReplaceNumbers("0000000000##"))
    .RuleFor(p => p.Timestamp, f => f.Date.Between(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow));

var fakeRequest = new Faker<PurchaseRequest>("en_GB")
    .RuleFor(p => p.Currency, p => p.Finance.Currency(false).Code)
    .RuleFor(p => p.Amount, p => p.Finance.Amount(0, 1000, 2))
    .RuleFor(p => p.Merchant, p => p.Company.CompanyName())
    .RuleFor(p => p.Country, p => p.Address.CountryCode());

var fakeResponse = new Faker<PurchaseResponse>()
    .RuleFor(p => p.Authorised, p => p.PickRandom<bool>())
    .RuleFor(p => p.ChallengeMethod, p => p.Random.ArrayElement<string>(new string[]{ "OTP", "OOB", "BIO", "None" }));

while (!false)
{
    try
    {

        Log log = fakeLog.Generate();
        log.Request = fakeRequest.Generate();
        //log.Response = fakeResponse.Generate();

        HttpClient httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri("http://localhost:7162/api/log");
        HttpContent content = new StringContent(JsonConvert.SerializeObject(log));
        HttpResponseMessage HttpResponseMessage = httpClient.PostAsync("http://localhost:7162/api/log", content).Result;

        Thread.Sleep(100);
    }
    catch(Exception ex)
    {

        Thread.Sleep(500);
    }
}