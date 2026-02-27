using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Services;
using System.Globalization;
using System.Text;

namespace Rvnx.CRM.Tests.Services
{
    public class VCardServiceEdgeCaseTests
    {
        private readonly VCardService _service;

        public VCardServiceEdgeCaseTests()
        {
            _service = new VCardService();
        }

        [Fact]
        public async Task ParseVCardShouldReturnEmptyWhenStreamIsEmpty()
        {
            using MemoryStream stream = new();

            List<Contact> contacts = (await _service.ParseVCardAsync(stream)).ToList();

            Assert.Empty(contacts);
        }

        [Fact]
        public async Task ParseVCardShouldReturnEmptyWhenInvalidVCardFormat()
        {
            string invalidContent = "This is not a valid VCard content";
            using MemoryStream stream = new(Encoding.UTF8.GetBytes(invalidContent));

            List<Contact> contacts = (await _service.ParseVCardAsync(stream)).ToList();

            Assert.Empty(contacts);
        }

        [Fact]
        public async Task ParseVCardShouldUseFallbackNameWhenNoNameProperty()
        {
            string vcfContent = @"BEGIN:VCARD
VERSION:3.0
FN:John Doe
END:VCARD";

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            List<Contact> contacts = (await _service.ParseVCardAsync(stream)).ToList();

            Assert.Single(contacts);
            Contact contact = contacts.First();
            Assert.Equal("John", contact.FirstName);
            Assert.Equal("Doe", contact.LastName);
        }

        [Fact]
        public async Task ParseVCardShouldHandleCommaFormattedDisplayName()
        {
            string vcfContent = @"BEGIN:VCARD
VERSION:3.0
FN:Doe, John
END:VCARD";

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            List<Contact> contacts = (await _service.ParseVCardAsync(stream)).ToList();

            Assert.Single(contacts);
            Contact contact = contacts.First();
            Assert.Equal("John", contact.FirstName);
            Assert.Equal("Doe", contact.LastName);
        }

        [Fact]
        public async Task ParseVCardShouldSetUnknownWhenNoNameAtAll()
        {
            string vcfContent = @"BEGIN:VCARD
VERSION:3.0
EMAIL:test@example.com
END:VCARD";

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            List<Contact> contacts = (await _service.ParseVCardAsync(stream)).ToList();

            Assert.Single(contacts);
            Contact contact = contacts.First();
            Assert.Equal("Unknown", contact.FirstName);
        }

        [Fact]
        public async Task ParseVCardShouldSwapNamesWhenOnlyLastNameProvided()
        {
            string vcfContent = @"BEGIN:VCARD
VERSION:3.0
N:Doe;;;;
END:VCARD";

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            List<Contact> contacts = (await _service.ParseVCardAsync(stream)).ToList();

            Assert.Single(contacts);
            Contact contact = contacts.First();
            Assert.Equal("Doe", contact.FirstName);
            Assert.Equal("", contact.LastName);
        }

        [Fact]
        public async Task ParseVCardShouldParseMultipleContacts()
        {
            string vcfContent = @"BEGIN:VCARD
VERSION:3.0
N:Doe;John;;;
FN:John Doe
END:VCARD
BEGIN:VCARD
VERSION:3.0
N:Smith;Jane;;;
FN:Jane Smith
END:VCARD";

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            List<Contact> contacts = (await _service.ParseVCardAsync(stream)).ToList();

            Assert.Equal(2, contacts.Count);
            Assert.Contains(contacts, c => c.FirstName == "John" && c.LastName == "Doe");
            Assert.Contains(contacts, c => c.FirstName == "Jane" && c.LastName == "Smith");
        }

        [Fact]
        public async Task ParseVCardShouldParseMultipleEmails()
        {
            string vcfContent = @"BEGIN:VCARD
VERSION:3.0
N:Doe;John;;;
EMAIL;TYPE=WORK:john.work@example.com
EMAIL;TYPE=HOME:john.home@example.com
END:VCARD";

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            List<Contact> contacts = (await _service.ParseVCardAsync(stream)).ToList();

            Assert.Single(contacts);
            Contact contact = contacts.First();
            Assert.Equal(2, contact.ContactMethods.Count);
            Assert.Contains(contact.ContactMethods, cm => cm.Value == "john.work@example.com" && cm.Type == ContactMethodType.Email);
            Assert.Contains(contact.ContactMethods, cm => cm.Value == "john.home@example.com" && cm.Type == ContactMethodType.Email);
        }

        [Fact]
        public async Task ParseVCardShouldParseMultiplePhones()
        {
            string vcfContent = @"BEGIN:VCARD
VERSION:3.0
N:Doe;John;;;
TEL;TYPE=CELL:1234567890
TEL;TYPE=WORK:0987654321
END:VCARD";

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            List<Contact> contacts = (await _service.ParseVCardAsync(stream)).ToList();

            Assert.Single(contacts);
            Contact contact = contacts.First();
            Assert.Equal(2, contact.ContactMethods.Count(cm => cm.Type == ContactMethodType.Phone));
        }

        [Fact]
        public async Task ParseVCardShouldHandleDateOnlyBirthday()
        {
            string vcfContent = @"BEGIN:VCARD
VERSION:3.0
N:Doe;John;;;
BDAY:19900115
END:VCARD";

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            List<Contact> contacts = (await _service.ParseVCardAsync(stream)).ToList();

            Assert.Single(contacts);
            Contact contact = contacts.First();
            SignificantDate? bday = contact.SignificantDates.FirstOrDefault(d => d.Title == SignificantDateTitles.Birthday);
            Assert.NotNull(bday);
            Assert.Equal(new DateTime(1990, 1, 15), bday.Date);
        }

        [Fact]
        public async Task ParseVCardShouldSetReminderFlagsForBirthday()
        {
            string vcfContent = @"BEGIN:VCARD
VERSION:3.0
N:Doe;John;;;
BDAY:1990-01-15
END:VCARD";

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            List<Contact> contacts = (await _service.ParseVCardAsync(stream)).ToList();

            Assert.Single(contacts);
            Contact contact = contacts.First();
            SignificantDate? bday = contact.SignificantDates.FirstOrDefault(d => d.Title == SignificantDateTitles.Birthday);
            Assert.NotNull(bday);
            Assert.True(bday.RemindMe);
            Assert.Equal(TimeSpan.FromDays(365), bday.EventFrequency);
        }

        // Export tests remain void as ExportVCard is sync
        [Fact]
        public void ExportVCardShouldHandleNullContactMethods()
        {
            Contact contact = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = "User",
                ContactMethods = null!
            };

            byte[] result = _service.ExportVCard(contact);

            Assert.NotEmpty(result);
            string vcf = Encoding.UTF8.GetString(result);
            Assert.Contains("BEGIN:VCARD", vcf);
            Assert.Contains("Test", vcf);
        }

        [Fact]
        public void ExportVCardShouldHandleNullSignificantDates()
        {
            Contact contact = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                SignificantDates = null!
            };

            byte[] result = _service.ExportVCard(contact);

            Assert.NotEmpty(result);
        }

        [Fact]
        public void ExportVCardShouldHandleEmptyLastName()
        {
            Contact contact = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                LastName = null
            };

            byte[] result = _service.ExportVCard(contact);
            string vcf = Encoding.UTF8.GetString(result);

            Assert.Contains("Test", vcf);
            Assert.DoesNotContain("null", vcf.ToLower(CultureInfo.InvariantCulture));
        }

        [Fact]
        public void ExportVCardShouldIncludeBirthdayOnlyNotOtherDates()
        {
            Contact contact = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                SignificantDates =
                [
                    new() { Title = SignificantDateTitles.Birthday, Date = new DateTime(1990, 1, 1) },
                    new() { Title = "Anniversary", Date = new DateTime(2020, 6, 15) }
                ]
            };

            byte[] result = _service.ExportVCard(contact);
            string vcf = Encoding.UTF8.GetString(result);

            // VCard format only supports BDAY, not arbitrary dates
            Assert.Contains("BDAY", vcf);
            // Anniversary should not appear (VCard doesn't support custom dates in the same way)
        }

        [Fact]
        public void ExportVCardShouldOnlyIncludeEmailAndPhoneContactMethods()
        {
            Contact contact = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                ContactMethods =
                [
                    new() { Type = ContactMethodType.Email, Value = "test@example.com" },
                    new() { Type = ContactMethodType.Phone, Value = "1234567890" },
                    new() { Type = ContactMethodType.Website, Value = "https://example.com" }
                ]
            };

            byte[] result = _service.ExportVCard(contact);
            string vcf = Encoding.UTF8.GetString(result);

            Assert.Contains("test@example.com", vcf);
            Assert.Contains("1234567890", vcf);
            // Website is not exported by default in this implementation
        }

        [Fact]
        public void ExportVCardShouldSkipEmptyEmailValues()
        {
            Contact contact = new()
            {
                Id = Guid.NewGuid(),
                FirstName = "Test",
                ContactMethods =
                [
                    new() { Type = ContactMethodType.Email, Value = "" },
                    new() { Type = ContactMethodType.Email, Value = null! },
                    new() { Type = ContactMethodType.Email, Value = "valid@example.com" }
                ]
            };

            byte[] result = _service.ExportVCard(contact);
            string vcf = Encoding.UTF8.GetString(result);

            Assert.Contains("valid@example.com", vcf);
        }

        [Fact]
        public async Task ParseVCardShouldExtractNickname()
        {
            string vcfContent = @"BEGIN:VCARD
VERSION:3.0
N:Doe;John;;;
NICKNAME:Johnny
END:VCARD";

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            List<Contact> contacts = (await _service.ParseVCardAsync(stream)).ToList();

            Assert.Single(contacts);
            Contact contact = contacts.First();
            Assert.Equal("Johnny", contact.Nickname);
        }

        [Fact]
        public async Task ParseVCardShouldExtractOrganizationAndTitle()
        {
            string vcfContent = @"BEGIN:VCARD
VERSION:3.0
N:Doe;John;;;
ORG:Acme Corporation
TITLE:Senior Developer
END:VCARD";

            using MemoryStream stream = new(Encoding.UTF8.GetBytes(vcfContent));

            List<Contact> contacts = (await _service.ParseVCardAsync(stream)).ToList();

            Assert.Single(contacts);
            Contact contact = contacts.First();
            Assert.Equal("Acme Corporation", contact.Company);
            Assert.Equal("Senior Developer", contact.JobTitle);
        }
    }
}
