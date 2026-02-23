using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Services
{
    public class RelationshipService(IRepository repository) : IRelationshipService
    {
        public async Task<RelationshipOperationResult> CreateRelationshipAsync(Relationship relationship,
            string selectedRelationshipType)
        {
            (Guid typeId, bool isReverse, string? error) = ParseRelationshipSelection(selectedRelationshipType);
            if (error != null)
            {
                return RelationshipOperationResult.Failure(error);
            }

            // Validation for Partial Contact vs Full Contact
            if (relationship.IsPartialContact)
            {
                if (string.IsNullOrWhiteSpace(relationship.PartialContactFirstName))
                {
                    return RelationshipOperationResult.Failure("First Name is required for partial contacts.");
                }

                // Ensure mutual exclusion (though IsPartialContact is based on RelatedEntityId being null, so this is implicit)
                // But we should ensure we don't have Partial data if RelatedEntityId is SET.
            }
            else
            {
                // Full contact
                if (!string.IsNullOrWhiteSpace(relationship.PartialContactFirstName) ||
                    !string.IsNullOrWhiteSpace(relationship.PartialContactLastName) ||
                    relationship.PartialContactDateOfBirth.HasValue)
                {
                     // Strict mutual exclusion: if linking to a full contact, partial fields should be empty.
                     // We can either return error or clear them. Clearing them is safer/friendlier.
                     relationship.PartialContactFirstName = null;
                     relationship.PartialContactLastName = null;
                     relationship.PartialContactDateOfBirth = null;
                }
            }

            relationship.RelationshipTypeId = typeId;

            if (isReverse)
            {
                SwapRelationshipEntities(relationship);
            }

            await repository.AddAsync(relationship);
            await repository.SaveChangesAsync();

            Guid redirectId;
            if (isReverse)
            {
                // If partial contact, we didn't swap, so we stay on EntityId.
                // If full contact, we swapped, so the original EntityId is now in RelatedEntityId.
                redirectId = relationship.IsPartialContact ? relationship.EntityId : relationship.RelatedEntityId!.Value;
            }
            else
            {
                redirectId = relationship.EntityId;
            }

            return RelationshipOperationResult.Ok(redirectId, relationship.EntityType);
        }

        public async Task<RelationshipOperationResult> UpdateRelationshipAsync(Guid id,
            Relationship updatedRelationship,
            string selectedRelationshipType)
        {
            (Guid typeId, bool isReverse, string? error) = ParseRelationshipSelection(selectedRelationshipType);
            if (error != null)
            {
                return RelationshipOperationResult.Failure(error);
            }

            Relationship? existingRelationship = await repository.GetByIdAsync<Relationship>(id);
            if (existingRelationship == null)
            {
                return RelationshipOperationResult.Failure("Relationship not found.");
            }

            existingRelationship.EntityId = updatedRelationship.EntityId;
            existingRelationship.RelatedEntityId = updatedRelationship.RelatedEntityId;
            existingRelationship.EntityType = updatedRelationship.EntityType;
            existingRelationship.Description = updatedRelationship.Description;
            existingRelationship.StartDate = updatedRelationship.StartDate;
            existingRelationship.EndDate = updatedRelationship.EndDate;
            existingRelationship.RelationshipTypeId = typeId;

            existingRelationship.PartialContactFirstName = updatedRelationship.PartialContactFirstName;
            existingRelationship.PartialContactLastName = updatedRelationship.PartialContactLastName;
            existingRelationship.PartialContactDateOfBirth = updatedRelationship.PartialContactDateOfBirth;
            // IsTypeReverse is handled by SwapRelationshipEntities logic below or reset
            existingRelationship.IsTypeReverse = false;

            if (isReverse)
            {
                SwapRelationshipEntities(existingRelationship);
            }

            await repository.UpdateAsync(existingRelationship);
            await repository.SaveChangesAsync();

            Guid redirectId;
            if (isReverse)
            {
                 redirectId = existingRelationship.IsPartialContact ? existingRelationship.EntityId : existingRelationship.RelatedEntityId!.Value;
            }
            else
            {
                redirectId = existingRelationship.EntityId;
            }

            return RelationshipOperationResult.Ok(redirectId, existingRelationship.EntityType);
        }

        private static (Guid TypeId, bool IsReverse, string? Error) ParseRelationshipSelection(string selection)
        {
            if (string.IsNullOrEmpty(selection))
            {
                return (Guid.Empty, false, "Relationship Type is required.");
            }

            string[] parts = selection.Split('_');
            if (parts.Length != 2 || !Guid.TryParse(parts[0], out Guid typeId))
            {
                return (Guid.Empty, false, "Invalid Relationship Type.");
            }

            return (typeId, parts[1] == "Rev", null);
        }

        private static void SwapRelationshipEntities(Relationship relationship)
        {
            if (relationship.RelatedEntityId.HasValue)
            {
                (relationship.EntityId, relationship.RelatedEntityId) =
                    (relationship.RelatedEntityId.Value, relationship.EntityId);
            }
            else
            {
                // Partial contact: cannot swap entities.
                // Mark as type reverse so we know the relationship direction is inverted.
                relationship.IsTypeReverse = true;
            }
        }

        public async Task<List<SelectOptionDto>> GetRelatedEntityOptionsAsync(Guid entityId, string entityType,
            Guid? selectedId = null)
        {
            List<SelectOptionDto> options = [];

            switch (entityType)
            {
                case EntityTypes.Person:
                    {
                        List<Contact> available =
                            await repository.ListAsNoTrackingAsync<Contact>(p => p.Id != entityId);
                        available = available.OrderBy(p => p.FullName).ToList();
                        options =
                        [
                            .. available.Select(p => new SelectOptionDto
                            {
                                Value = p.Id.ToString(), Text = p.FullName, Selected = selectedId == p.Id
                            })
                        ];
                        break;
                    }
                case EntityTypes.Company:
                    {
                        List<Employer> available =
                            await repository.ListAsNoTrackingAsync<Employer>(c => c.Id != entityId);
                        available = available.OrderBy(c => c.CompanyName).ToList();
                        options =
                        [
                            .. available.Select(c => new SelectOptionDto
                            {
                                Value = c.Id.ToString(), Text = c.CompanyName, Selected = selectedId == c.Id
                            })
                        ];
                        break;
                    }
            }

            return options;
        }

        public List<SelectOptionDto> GetRelationshipTypeOptions(string entityType, string? selectedValue = null)
        {
            List<RelationshipTypeDefinition> types = RelationshipTypeService.GetByEntityType(entityType);
            types = [.. types.OrderBy(t => t.Category).ThenBy(t => t.Name)];

            List<SelectOptionDto> options = [];

            foreach (RelationshipTypeDefinition t in types)
            {
                string group = t.Category;

                string fwdText = t.IsSymmetric ? $"is {t.Name} of" : $"is {t.Name} of ({t.OppositeName})";
                options.Add(new SelectOptionDto
                {
                    Value = $"{t.Id}_Fwd",
                    Text = fwdText,
                    Group = group,
                    Selected = selectedValue == $"{t.Id}_Fwd"
                });

                if (!t.IsSymmetric)
                {
                    string revText = $"is {t.OppositeName} of ({t.Name})";
                    options.Add(new SelectOptionDto
                    {
                        Value = $"{t.Id}_Rev",
                        Text = revText,
                        Group = group,
                        Selected = selectedValue == $"{t.Id}_Rev"
                    });
                }
            }

            return options;
        }

        public async Task<RelationshipOperationResult> PromotePartialContactAsync(Guid relationshipId)
        {
            Relationship? relationship = await repository.GetByIdAsync<Relationship>(relationshipId);
            if (relationship == null)
            {
                return RelationshipOperationResult.Failure("Relationship not found.");
            }

            if (!relationship.IsPartialContact)
            {
                return RelationshipOperationResult.Failure("Relationship is not a partial contact.");
            }

            if (string.IsNullOrWhiteSpace(relationship.PartialContactFirstName))
            {
                return RelationshipOperationResult.Failure("Partial contact data is incomplete.");
            }

            // Create new contact
            Contact newContact = new()
            {
                Id = Guid.NewGuid(),
                FirstName = relationship.PartialContactFirstName,
                LastName = relationship.PartialContactLastName
            };

            await repository.AddAsync(newContact);

            // Add Birthday if present
            if (relationship.PartialContactDateOfBirth.HasValue)
            {
                SignificantDate birthday = new()
                {
                    Id = Guid.NewGuid(),
                    ContactId = newContact.Id,
                    Title = SignificantDateTitles.Birthday,
                    Date = relationship.PartialContactDateOfBirth.Value,
                    EventFrequency = TimeSpan.FromDays(365) // Yearly frequency
                };
                await repository.AddAsync(birthday);
            }

            // Update relationship
            relationship.RelatedEntityId = newContact.Id;
            relationship.PartialContactFirstName = null;
            relationship.PartialContactLastName = null;
            relationship.PartialContactDateOfBirth = null;

            // If the relationship was marked as "Reverse" because we couldn't swap entities (due to Partial Contact),
            // we should now normalize it by swapping the entities (as both are now Full Contacts) and clearing the flag.
            if (relationship.IsTypeReverse)
            {
                relationship.IsTypeReverse = false;
                SwapRelationshipEntities(relationship);
            }

            await repository.UpdateAsync(relationship);
            await repository.SaveChangesAsync();

            return RelationshipOperationResult.Ok(newContact.Id, EntityTypes.Person);
        }
    }
}
