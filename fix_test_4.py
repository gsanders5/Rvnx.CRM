import re

with open("Rvnx.CRM.Tests/Services/FavoriteServiceTests.cs", "r") as f:
    content = f.read()

content = content.replace("ListProjectedByChunkedContainsAsync(\n                It.IsAny<List<Guid>>(),\n                It.IsAny<Func<List<Guid>", "ListProjectedByChunkedContainsAsync(\n                It.IsAny<IEnumerable<Guid>>(),\n                It.IsAny<Func<IEnumerable<Guid>>")
content = content.replace("Func<Attachment", "Func<Rvnx.CRM.Core.Models.Contact.Attachment")
content = content.replace("[Fact]\n        public async Task WhenUserIdNull", "[Fact]\n        [System.Diagnostics.CodeAnalysis.SuppressMessage(\"Naming\", \"CA1707:Identifiers should not contain underscores\", Justification = \"Test names follow a standard convention\")]\n        public async Task WhenUserIdNull")
content = content.replace("[Fact]\n        public async Task WhenNoFavorites", "[Fact]\n        [System.Diagnostics.CodeAnalysis.SuppressMessage(\"Naming\", \"CA1707:Identifiers should not contain underscores\", Justification = \"Test names follow a standard convention\")]\n        public async Task WhenNoFavorites")
content = content.replace("[Fact]\n        public async Task WhenContactsFilteredOut", "[Fact]\n        [System.Diagnostics.CodeAnalysis.SuppressMessage(\"Naming\", \"CA1707:Identifiers should not contain underscores\", Justification = \"Test names follow a standard convention\")]\n        public async Task WhenContactsFilteredOut")
content = content.replace("[Fact]\n        public async Task WithValidFavorites", "[Fact]\n        [System.Diagnostics.CodeAnalysis.SuppressMessage(\"Naming\", \"CA1707:Identifiers should not contain underscores\", Justification = \"Test names follow a standard convention\")]\n        public async Task WithValidFavorites")

with open("Rvnx.CRM.Tests/Services/FavoriteServiceTests.cs", "w") as f:
    f.write(content)
