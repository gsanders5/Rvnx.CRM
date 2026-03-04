using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Business;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Services
{
    public class RelationshipService(IRepository repository) : IRelationshipService
    {
        public async Task<RelationshipOperationResult> CreateRelationshipAsync(Relationship relationship,
            string selectedRelationshipType, List<Guid>? suggestedEntityIds = null)
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

            if (suggestedEntityIds != null && suggestedEntityIds.Count > 0 && RelationshipTypeService.TransitiveRelationshipTypeIds.Contains(typeId))
            {
                Guid mainEntityId = relationship.EntityId;
                Guid mainRelatedEntityId = relationship.RelatedEntityId;

                foreach (Guid suggestedId in suggestedEntityIds)
                {
                    // Check if suggestedId is already linked to mainEntityId
                    bool isLinkedToEntity = await repository.CountAsync<Relationship>(
                        r => r.RelationshipTypeId == typeId &&
                             ((r.EntityId == mainEntityId && r.RelatedEntityId == suggestedId) ||
                              (r.RelatedEntityId == mainEntityId && r.EntityId == suggestedId))) > 0;

                    Guid targetIdToLink = isLinkedToEntity ? mainRelatedEntityId : mainEntityId;

                    // Double check it's not already linked to the target
                    bool isAlreadyLinkedToTarget = await repository.CountAsync<Relationship>(
                        r => r.RelationshipTypeId == typeId &&
                             ((r.EntityId == targetIdToLink && r.RelatedEntityId == suggestedId) ||
                              (r.RelatedEntityId == targetIdToLink && r.EntityId == suggestedId))) > 0;

                    if (!isAlreadyLinkedToTarget)
                    {
                        await repository.AddAsync(new Relationship
                        {
                            EntityId = targetIdToLink,
                            RelatedEntityId = suggestedId,
                            EntityType = relationship.EntityType,
                            RelationshipTypeId = typeId,
                            Description = "Automatically added from suggested relationship."
                        });
                    }
                }
            }

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
                        options = await repository.ListProjectedAsync<Contact, SelectOptionDto, string>(
                            p => p.Id != entityId,
                            p => new SelectOptionDto
                            {
                                Value = p.Id.ToString(),
                                Text = p.IsPartial
                                    ? p.FirstName + " " + (p.LastName ?? "") + " (partial contact)"
                                    : p.FirstName + " " + (p.LastName ?? ""),
                                Selected = selectedId == p.Id
                            },
                            p => p.FirstName + " " + (p.LastName ?? ""));
                        break;
                    }
                case EntityTypes.Company:
                    {
                        options = await repository.ListProjectedAsync<Employer, SelectOptionDto, string>(
                            c => c.Id != entityId,
                            c => new SelectOptionDto
                            {
                                Value = c.Id.ToString(),
                                Text = c.CompanyName,
                                Selected = selectedId == c.Id
                            },
                            c => c.CompanyName);
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
                    EventDate = DateOnly.FromDateTime(dto.Birthday.Value),
                    Description = "Birthday",
                    RecurrenceType = Enumerations.RecurrenceType.Annual,
                    IsActive = true
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

            if (dto.SuggestedEntityIds != null && dto.SuggestedEntityIds.Count > 0 && RelationshipTypeService.TransitiveRelationshipTypeIds.Contains(typeId))
            {
                foreach (Guid suggestedId in dto.SuggestedEntityIds)
                {
                    // The suggested id is already related to parentEntityId, so we link it to the new partial contact
                    await repository.AddAsync(new Relationship
                    {
                        EntityId = partialContact.Id,
                        RelatedEntityId = suggestedId,
                        EntityType = EntityTypes.Person,
                        RelationshipTypeId = typeId,
                        Description = "Automatically added from suggested relationship."
                    });
                }
            }

            await repository.SaveChangesAsync();

            return RelationshipOperationResult.Ok(parentEntityId, EntityTypes.Person);
        }

        public async Task<List<SuggestedRelationshipDto>> GetSuggestedRelationshipsAsync(Guid entityId, Guid? relatedEntityId, Guid relationshipTypeId, string? partialContactName)
        {
            List<SuggestedRelationshipDto> suggestions = [];

            if (!RelationshipTypeService.TransitiveRelationshipTypeIds.Contains(relationshipTypeId))
            {
                return suggestions;
            }

            RelationshipTypeDefinition? typeDef = RelationshipTypeService.GetById(relationshipTypeId);
            if (typeDef == null)
            {
                return suggestions;
            }

            // Get names (to construct the suggestion label)
            Contact? entity = await repository.GetByIdAsync<Contact>(entityId);
            if (entity == null) return suggestions;
            string entityName = $"{entity.FirstName} {entity.LastName}".Trim();

            // All contacts related to entityId via this relationshipTypeId
            var eRels = await repository.ListAsNoTrackingAsync<Relationship>(
                r => r.RelationshipTypeId == relationshipTypeId && (r.EntityId == entityId || r.RelatedEntityId == entityId));

            if (relatedEntityId.HasValue)
            {
                Contact? relatedEntity = await repository.GetByIdAsync<Contact>(relatedEntityId.Value);
                if (relatedEntity == null) return suggestions;
                string relatedEntityName = $"{relatedEntity.FirstName} {relatedEntity.LastName}".Trim();

                // All contacts related to relatedEntityId via this relationshipTypeId
                var rRels = await repository.ListAsNoTrackingAsync<Relationship>(
                    r => r.RelationshipTypeId == relationshipTypeId && (r.EntityId == relatedEntityId.Value || r.RelatedEntityId == relatedEntityId.Value));

                HashSet<Guid> eRelIds = eRels.Select(r => r.EntityId == entityId ? r.RelatedEntityId : r.EntityId).ToHashSet();
                HashSet<Guid> rRelIds = rRels.Select(r => r.EntityId == relatedEntityId.Value ? r.RelatedEntityId : r.EntityId).ToHashSet();

                // For each contact C related to relatedEntityId, if C is NOT related to entityId, suggest entityId <-> C
                foreach (Guid cId in rRelIds)
                {
                    if (cId != entityId && !eRelIds.Contains(cId))
                    {
                        Contact? cContact = await repository.GetByIdAsync<Contact>(cId);
                        if (cContact != null)
                        {
                            suggestions.Add(new SuggestedRelationshipDto
                            {
                                ExistingContactId = cId,
                                SourceName = entityName,
                                TargetName = $"{cContact.FirstName} {cContact.LastName}".Trim(),
                                RelationshipName = typeDef.Name
                            });
                        }
                    }
                }

                // For each contact C related to entityId, if C is NOT related to relatedEntityId, suggest relatedEntityId <-> C
                foreach (Guid cId in eRelIds)
                {
                    if (cId != relatedEntityId.Value && !rRelIds.Contains(cId))
                    {
                        Contact? cContact = await repository.GetByIdAsync<Contact>(cId);
                        if (cContact != null)
                        {
                            suggestions.Add(new SuggestedRelationshipDto
                            {
                                ExistingContactId = cId,
                                SourceName = relatedEntityName,
                                TargetName = $"{cContact.FirstName} {cContact.LastName}".Trim(),
                                RelationshipName = typeDef.Name
                            });
                        }
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(partialContactName))
            {
                // New partial contact: has no existing relations.
                // For each contact C related to entityId, suggest partialContact <-> C
                HashSet<Guid> eRelIds = eRels.Select(r => r.EntityId == entityId ? r.RelatedEntityId : r.EntityId).ToHashSet();
                foreach (Guid cId in eRelIds)
                {
                    Contact? cContact = await repository.GetByIdAsync<Contact>(cId);
                    if (cContact != null)
                    {
                        suggestions.Add(new SuggestedRelationshipDto
                        {
                            ExistingContactId = cId,
                            SourceName = partialContactName,
                            TargetName = $"{cContact.FirstName} {cContact.LastName}".Trim(),
                            RelationshipName = typeDef.Name
                        });
                    }
                }
            }

            return suggestions;
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

        public async Task<Relationship?> GetRelationshipForEditAsync(Guid id)
        {
            return await repository.GetByIdAsync<Relationship>(id);
        }

        public async Task<Relationship?> GetRelationshipForDeleteAsync(Guid id)
        {
            Relationship? relationship = await repository.GetByIdAsync<Relationship>(id);
            if (relationship == null)
            {
                return null;
            }

            Guid p1Id = relationship.EntityId;
            Guid p2Id = relationship.RelatedEntityId;
            List<Contact> contacts = await repository.ListAsync<Contact>(c => c.Id == p1Id || c.Id == p2Id);

            relationship.Person = contacts.FirstOrDefault(c => c.Id == p1Id);
            relationship.RelatedPerson = contacts.FirstOrDefault(c => c.Id == p2Id);

            return relationship;
        }

        public async Task<OperationResult> DeleteRelationshipAsync(Guid id)
        {
            Relationship? relationship = await repository.GetByIdAsync<Relationship>(id);
            if (relationship != null)
            {
                Guid entityId = relationship.EntityId;
                string entityType = relationship.EntityType;
                await repository.DeleteAsync<Relationship>(id);
                await repository.SaveChangesAsync();
                return OperationResult.Ok(entityId, entityType);
            }
            return OperationResult.Failure("Relationship not found.");
        }
    }
}
