using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Tests.Models;

public class PersonTests
{
    [Fact]
    public void FullNameWithOnlyFirstNameShouldTrimTrailingSpace()
    {
        var contact = new Contact { FirstName = "John", LastName = null };

        Assert.Equal("John", contact.FullName);
    }

    [Fact]
    public void FullNameWithWhitespaceOnlyLastNameShouldTrimExtraSpaces()
    {
        var contact = new Contact { FirstName = "John", LastName = "   " };

        Assert.Equal("John", contact.FullName);
    }
}
