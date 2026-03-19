using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Services;
using System.Text;

namespace Rvnx.CRM.Tests.Services;

public class VCardServiceExportTests
{
    private readonly VCardService _service;

    public VCardServiceExportTests()
    {
        _service = new VCardService();
    }

    [Fact]
    public void ExportVCardShouldIncludeFullName()
    {
        var contact = new Contact
        {
            FirstName = "John",
            LastName = "Doe"
        };

        byte[] result = _service.ExportVCard(contact);
        string output = Encoding.UTF8.GetString(result);

        Assert.Contains("FN:John Doe", output);
    }

    [Fact]
    public void ExportVCardShouldIncludeEmail()
    {
        var contact = new Contact
        {
            FirstName = "John",
            ContactMethods = [
                new ContactMethod { Type = ContactMethodType.Email, Value = "john@example.com" }
            ]
        };

        byte[] result = _service.ExportVCard(contact);
        string output = Encoding.UTF8.GetString(result);

        Assert.Contains("john@example.com", output);
    }

    [Fact]
    public void ExportVCardShouldIncludePhone()
    {
        var contact = new Contact
        {
            FirstName = "John",
            ContactMethods = [
                new ContactMethod { Type = ContactMethodType.Phone, Value = "555-1234" }
            ]
        };

        byte[] result = _service.ExportVCard(contact);
        string output = Encoding.UTF8.GetString(result);

        Assert.Contains("555-1234", output);
    }

    [Fact]
    public void ExportVCardShouldIncludeBirthday()
    {
        var contact = new Contact
        {
            FirstName = "John",
            SignificantDates = [
                new SignificantDate { Title = "Birthday", EventDate = new DateOnly(1990, 5, 15) }
            ]
        };

        byte[] result = _service.ExportVCard(contact);
        string output = Encoding.UTF8.GetString(result);

        Assert.Contains("BDAY", output);
        Assert.Contains("1990", output);
    }

    [Fact]
    public void ExportVCardShouldIncludeMaidenName()
    {
        var contact = new Contact
        {
            FirstName = "Jane",
            MaidenName = "Smith"
        };

        byte[] result = _service.ExportVCard(contact);
        string output = Encoding.UTF8.GetString(result);

        Assert.Contains("X-MAIDENNAME:Smith", output);
    }

    [Fact]
    public void ExportVCardShouldIncludeGender()
    {
        var contact = new Contact
        {
            FirstName = "Jane",
            Gender = "Female"
        };

        byte[] result = _service.ExportVCard(contact);
        string output = Encoding.UTF8.GetString(result);

        Assert.Contains("X-GENDER:Female", output);
    }

    [Fact]
    public void ExportVCardShouldReturnValidUtf8Bytes()
    {
        var contact = new Contact
        {
            FirstName = "Minimal"
        };

        byte[] result = _service.ExportVCard(contact);

        Assert.NotEmpty(result);

        string output = Encoding.UTF8.GetString(result);
        Assert.Contains("BEGIN:VCARD", output);
        Assert.Contains("END:VCARD", output);
    }
}
