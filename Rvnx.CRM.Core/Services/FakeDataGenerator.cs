using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Services
{
    public class FakeDataGenerator
    {
        private static readonly Random _random = new();

        private static readonly string[] FirstNames = { "James", "John", "Robert", "Michael", "William", "David", "Richard", "Joseph", "Thomas", "Charles", "Mary", "Patricia", "Jennifer", "Linda", "Elizabeth", "Barbara", "Susan", "Jessica", "Sarah", "Karen", "Liam", "Noah", "Oliver", "Elijah", "James", "William", "Benjamin", "Lucas", "Henry", "Alexander", "Olivia", "Emma", "Ava", "Charlotte", "Sophia", "Amelia", "Isabella", "Mia", "Evelyn", "Harper" };
        private static readonly string[] LastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin" };
        private static readonly string[] Cities = { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego", "Dallas", "San Jose" };
        private static readonly string[] States = { "NY", "CA", "IL", "TX", "AZ", "PA", "TX", "CA", "TX", "CA" };
        private static readonly string[] Streets = { "Main St", "High St", "Broadway", "Market St", "Park Ave", "Oak St", "Washington St", "Maple Ave", "Cedar St", "Elm St" };
        private static readonly string[] Companies = { "Acme Corp", "Globex", "Soylent Corp", "Initech", "Umbrella Corp", "Stark Ind", "Wayne Ent", "Cyberdyne", "Massive Dynamic" };
        private static readonly string[] JobTitles = { "Developer", "Manager", "Designer", "Engineer", "Analyst", "Consultant", "Director", "VP", "CEO", "CTO" };

        public static Contact GenerateContact()
        {
            string firstName = FirstNames[_random.Next(FirstNames.Length)];
            string lastName = LastNames[_random.Next(LastNames.Length)];

            Contact contact = new()
            {
                Id = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                Nickname = _random.Next(2) == 0 ? firstName[..Math.Min(3, firstName.Length)] : null,
                Company = _random.Next(3) == 0 ? Companies[_random.Next(Companies.Length)] : null,
                JobTitle = _random.Next(3) == 0 ? JobTitles[_random.Next(JobTitles.Length)] : null,
                IsHidden = _random.Next(10) == 0 // 10% chance
            };

            // Generate Address
            if (_random.Next(2) == 0)
            {
                int cityIndex = _random.Next(Cities.Length);
                contact.Addresses.Add(new Address
                {
                    Id = Guid.NewGuid(),
                    EntityId = contact.Id,
                    EntityType = EntityTypes.Person,
                    Street = $"{_random.Next(100, 9999)} {Streets[_random.Next(Streets.Length)]}",
                    City = Cities[cityIndex],
                    State = States[cityIndex],
                    Zip = _random.Next(10000, 99999).ToString(),
                    Country = "USA",
                    AddressType = _random.Next(2) == 0 ? "Home" : "Work"
                });
            }

            // Generate Phone
            if (_random.Next(2) == 0)
            {
                contact.ContactMethods.Add(new ContactMethod
                {
                    Id = Guid.NewGuid(),
                    EntityId = contact.Id,
                    EntityType = EntityTypes.Person,
                    Type = ContactMethodType.Phone,
                    Value = $"{_random.Next(200, 999)}-{_random.Next(200, 999)}-{_random.Next(1000, 9999)}",
                    Label = "Mobile"
                });
            }

            // Generate Email
            if (_random.Next(2) == 0)
            {
                contact.ContactMethods.Add(new ContactMethod
                {
                    Id = Guid.NewGuid(),
                    EntityId = contact.Id,
                    EntityType = EntityTypes.Person,
                    Type = ContactMethodType.Email,
                    Value = $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
                    Label = "Personal"
                });
            }

            // Generate Birthday
            if (_random.Next(2) == 0)
            {
                int year = DateTime.Today.Year - _random.Next(20, 70);
                int month = _random.Next(1, 13);
                int day = _random.Next(1, 28);
                contact.SignificantDates.Add(new SignificantDate
                {
                    Id = Guid.NewGuid(),
                    EntityId = contact.Id,
                    EntityType = EntityTypes.Person,
                    Title = "Birthday",
                    Date = new DateTime(year, month, day),
                    Description = "Birthday"
                });
            }

            return contact;
        }

        public static List<Contact> GenerateContacts(int count)
        {
            List<Contact> list = new();
            for (int i = 0; i < count; i++)
            {
                list.Add(GenerateContact());
            }
            return list;
        }
    }
}
