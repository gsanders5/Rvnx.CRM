import re

with open("Rvnx.CRM.Tests/Services/FavoriteServiceTests.cs", "r") as f:
    content = f.read()

content = content.replace("Rvnx.CRM.Core.Models.Contact.Attachment", "Rvnx.CRM.Core.Models.Base.Attachment")

with open("Rvnx.CRM.Tests/Services/FavoriteServiceTests.cs", "w") as f:
    f.write(content)
