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

            relationship.RelationshipTypeId = typeId;

            if (isReverse)
            {
                SwapRelationshipEntities(relationship);
            }

            await repository.AddAsync(relationship);
            await repository.SaveChangesAsync();

            Guid redirectId = isReverse ? relationship.RelatedEntityId : relationship.EntityId;
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

            if (isReverse)
            {
                SwapRelationshipEntities(existingRelationship);
            }

            await repository.UpdateAsync(existingRelationship);
            await repository.SaveChangesAsync();

            Guid redirectId = isReverse ? existingRelationship.RelatedEntityId : existingRelationship.EntityId;
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
            (relationship.EntityId, relationship.RelatedEntityId) =
                (relationship.RelatedEntityId, relationship.EntityId);
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
                                Value = p.Id.ToString(), Text = p.IsPartial ? $"{p.FullName} (partial contact)" : p.FullName, Selected = selectedId == p.Id
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

        public List<RelationshipTypeDefinition> GetRelationshipTypes(string entityType)
        {
            return [.. RelationshipTypeService.GetByEntityType(entityType).OrderBy(t => t.Category).ThenBy(t => t.Name)];
        }

        public async Task<RelationshipOperationResult> CreatePartialContactRelationshipAsync(Guid parentEntityId, string selectedRelationshipType, CreatePartialContactRelationshipDto dto)
        {
            (Guid typeId, bool isReverse, string? error) = ParseRelationshipSelection(selectedRelationshipType);
            if (error != null)
            {
                return RelationshipOperationResult.Failure(error);
            }

            Contact partialContact = new()
            {
                Id = Guid.NewGuid(),
                IsPartial = true,
                FirstName = dto.PartialContactFirstName,
                LastName = dto.PartialContactLastName
            };

            await repository.AddAsync(partialContact);

            if (dto.Birthday.HasValue)
            {
                SignificantDate bday = new()
                {
                    Id = Guid.NewGuid(),
                    ContactId = partialContact.Id,
                    Title = SignificantDateTitles.Birthday,
                    Date = dto.Birthday.Value,
                    Description = "Birthday",
                    RemindMe = true,
                    EventFrequency = TimeSpan.FromDays(365)
                };
                await repository.AddAsync(bday);
            }

            Relationship relationship = new()
            {
                Id = Guid.NewGuid(),
                EntityId = parentEntityId,
                RelatedEntityId = partialContact.Id,
                EntityType = EntityTypes.Person,
                RelationshipTypeId = typeId,
                Description = dto.Description
            };

            if (isReverse)
            {
                SwapRelationshipEntities(relationship);
            }

            await repository.AddAsync(relationship);
            await repository.SaveChangesAsync();

            return RelationshipOperationResult.Ok(parentEntityId, EntityTypes.Person);
        }

        public async Task<RelationshipOperationResult> PromotePartialContactAsync(Guid contactId)
        {
            Contact? contact = await repository.GetByIdAsync<Contact>(contactId);
            if (contact == null)
            {
                return RelationshipOperationResult.Failure("Contact not found.");
            }

            if (!contact.IsPartial)
            {
                return RelationshipOperationResult.Failure("Contact is not a partial contact.");
            }

            contact.IsPartial = false;
            await repository.UpdateAsync(contact);
            await repository.SaveChangesAsync();

            return RelationshipOperationResult.Ok(contact.Id, EntityTypes.Person);
        }
    }
}
