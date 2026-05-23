import re

with open("Rvnx.CRM.Tests/Services/FavoriteServiceTests.cs", "r") as f:
    content = f.read()

# Fix the rest of the mock setups that Moq is rejecting for extension methods
content = content.replace("ListProjectedByChunkedContainsAsync(\n                It.IsAny<IEnumerable<Guid>>(),\n                It.IsAny<Func<IEnumerable<Guid>, Expression<Func<Rvnx.CRM.Core.Models.Base.Attachment, bool>>>>(),", "ListProjectedAsync(\n                It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.Base.Attachment, bool>>>(),")

content = content.replace("ListProjectedByChunkedContainsAsync(\n                It.IsAny<IEnumerable<Guid>>(),\n                It.IsAny<Func<IEnumerable<Guid>, Expression<Func<Contact, bool>>>>(),", "ListProjectedAsync(\n                It.IsAny<Expression<Func<Contact, bool>>>(),")

with open("Rvnx.CRM.Tests/Services/FavoriteServiceTests.cs", "w") as f:
    f.write(content)
