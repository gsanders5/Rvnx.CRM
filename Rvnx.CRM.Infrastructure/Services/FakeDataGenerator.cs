using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Globalization;

namespace Rvnx.CRM.Infrastructure.Services;

public class FakeDataGenerator
{
    private static readonly Random _random = new();

    private static readonly string[] MaleFirstNames = { "James", "John", "Robert", "Michael", "William", "David", "Richard", "Joseph", "Thomas", "Charles", "Liam", "Noah", "Oliver", "Elijah", "Benjamin", "Lucas", "Henry", "Alexander" };
    private static readonly string[] FemaleFirstNames = { "Mary", "Patricia", "Jennifer", "Linda", "Elizabeth", "Barbara", "Susan", "Jessica", "Sarah", "Karen", "Olivia", "Emma", "Ava", "Charlotte", "Sophia", "Amelia", "Isabella", "Mia", "Evelyn", "Harper" };
    private static readonly string[] LastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin" };

    private static readonly string MaleGender = PersonalAttributeOptions.Male;
    private static readonly string FemaleGender = PersonalAttributeOptions.Female;
    private static readonly string MalePronoun = "He/Him";
    private static readonly string FemalePronoun = "She/Her";

    private static readonly string[] Cities = { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego", "Dallas", "San Jose" };
    private static readonly string[] States = { "NY", "CA", "IL", "TX", "AZ", "PA", "TX", "CA", "TX", "CA" };
    private static readonly string[] Streets = { "Main St", "High St", "Broadway", "Market St", "Park Ave", "Oak St", "Washington St", "Maple Ave", "Cedar St", "Elm St" };
    private static readonly string[] Companies = { "Acme Corp", "Globex", "Soylent Corp", "Initech", "Umbrella Corp", "Stark Ind", "Wayne Ent", "Cyberdyne", "Massive Dynamic" };
    private static readonly string[] JobTitles = { "Developer", "Manager", "Designer", "Engineer", "Analyst", "Consultant", "Director", "VP", "CEO", "CTO" };

    private static Contact GenerateContact()
    {
        bool isMale = _random.Next(2) == 0;
        string firstName;
        string gender;
        string pronouns;

        if (isMale)
        {
            firstName = MaleFirstNames[_random.Next(MaleFirstNames.Length)];
            gender = MaleGender;
            pronouns = MalePronoun;
        }
        else
        {
            firstName = FemaleFirstNames[_random.Next(FemaleFirstNames.Length)];
            gender = FemaleGender;
            pronouns = FemalePronoun;
        }

        string lastName = LastNames[_random.Next(LastNames.Length)];

        Contact contact = new()
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            Gender = gender,
            Pronouns = pronouns,
            Nickname = _random.Next(2) == 0 ? firstName[..Math.Min(3, firstName.Length)] : null,
            Company = _random.Next(3) == 0 ? Companies[_random.Next(Companies.Length)] : null,
            JobTitle = _random.Next(3) == 0 ? JobTitles[_random.Next(JobTitles.Length)] : null,
            IsHidden = _random.Next(10) == 0 // 10% chance
        };

        if (_random.Next(2) == 0)
        {
            int cityIndex = _random.Next(Cities.Length);
            contact.Addresses.Add(new Address
            {
                Id = Guid.NewGuid(),
                ContactId = contact.Id,
                Line1 = $"{_random.Next(100, 9999)} {Streets[_random.Next(Streets.Length)]}",
                City = Cities[cityIndex],
                State = States[cityIndex],
                Zip = _random.Next(10000, 99999).ToString(CultureInfo.InvariantCulture),
                Country = "USA",
                AddressType = _random.Next(2) == 0 ? "Home" : "Work"
            });
        }

        if (_random.Next(2) == 0)
        {
            contact.ContactMethods.Add(new ContactMethod
            {
                Id = Guid.NewGuid(),
                ContactId = contact.Id,
                Type = ContactMethodType.Phone,
                Value = $"{_random.Next(200, 999)}-{_random.Next(200, 999)}-{_random.Next(1000, 9999)}",
                Label = "Mobile"
            });
        }

        if (_random.Next(2) == 0)
        {
            contact.ContactMethods.Add(new ContactMethod
            {
                Id = Guid.NewGuid(),
                ContactId = contact.Id,
                Type = ContactMethodType.Email,
                Value = $"{firstName.ToLowerInvariant()}.{lastName.ToLowerInvariant()}@example.com",
                Label = "Personal"
            });
        }

        if (_random.Next(2) == 0)
        {
            int year = DateTime.Today.Year - _random.Next(20, 70);
            int month = _random.Next(1, 13);
            int day = _random.Next(1, 28);
            contact.SignificantDates.Add(new SignificantDate
            {
                Id = Guid.NewGuid(),
                ContactId = contact.Id,
                Title = "Birthday",
                EventDate = new DateOnly(year, month, day),
                Description = "Birthday",
                RecurrenceType = Core.Enumerations.RecurrenceType.Annual,
                IsActive = true
            });
        }

        return contact;
    }

    public static List<Contact> GenerateContacts(int count)
    {
        List<Contact> list = [];
        for (int i = 0; i < count; i++)
        {
            list.Add(GenerateContact());
        }
        return list;
    }

    public static List<Relationship> GenerateRelationships(List<Contact> contacts, int count)
    {
        List<Relationship> relationships = [];
        IReadOnlyList<RelationshipTypeDefinition> personTypes = RelationshipTypeService.GetAll();

        if (contacts.Count < 2)
        {
            return relationships;
        }

        for (int i = 0; i < count; i++)
        {
            Contact contact1 = contacts[_random.Next(contacts.Count)];
            Contact contact2 = contacts[_random.Next(contacts.Count)];

            while (contact1.Id == contact2.Id)
            {
                contact2 = contacts[_random.Next(contacts.Count)];
            }

            RelationshipTypeDefinition type = personTypes[_random.Next(personTypes.Count)];

            relationships.Add(new Relationship
            {
                Id = Guid.NewGuid(),
                ContactId = contact1.Id,
                RelatedContactId = contact2.Id,
                RelationshipTypeId = type.Id,
                Description = "Generated relationship"
            });
        }
        return relationships;
    }
}
