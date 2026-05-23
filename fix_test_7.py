import re

with open("Rvnx.CRM.Tests/Services/FavoriteServiceTests.cs", "r") as f:
    content = f.read()

# Since ListProjectedByChunkedContainsAsync is an extension method, we shouldn't mock it directly on IRepository.
# We should mock the underlying ListProjectedAsync call instead, because ListProjectedByChunkedContainsAsync calls repository.ListProjectedAsync.

content = content.replace("It.IsAny<Expression<Func<ContactFavorite, bool>>>(),\n                It.IsAny<Expression<Func<ContactFavorite, Guid>>>()", "It.IsAny<Expression<Func<ContactFavorite, bool>>>(),\n                It.IsAny<Expression<Func<ContactFavorite, Guid>>>()")

with open("Rvnx.CRM.Tests/Services/FavoriteServiceTests.cs", "w") as f:
    f.write(content)
