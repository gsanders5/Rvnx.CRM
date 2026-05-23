import re

with open("Rvnx.CRM.Tests/Services/FavoriteServiceTests.cs", "r") as f:
    content = f.read()

content = content.replace("Func<IEnumerable<Guid>>", "Func<IEnumerable<Guid>")

with open("Rvnx.CRM.Tests/Services/FavoriteServiceTests.cs", "w") as f:
    f.write(content)
