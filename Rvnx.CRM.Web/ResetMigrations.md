# Create a migration
Add-Migration InitialCreate

# Update database
Update-Database


# ------------

# List migrations
Get-Migration

# Remove last migration (if not applied to database)
Remove-Migration

# Generate SQL script
Script-Migration

# Update to specific migration
Update-Database -Migration SpecificMigrationName