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
            string selectedRelationshipType, List<string>? suggestedEntityIds = null)
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

            if (suggestedEntityIds != null && suggestedEntityIds.Count > 0)
            {
                foreach (string payload in suggestedEntityIds)
                {
                    string[] parts = payload.Split('_');
                    if (parts.Length == 3 && Guid.TryParse(parts[0], out Guid sId) &&
                        Guid.TryParse(parts[1], out Guid tId) && bool.TryParse(parts[2], out bool reverse))
                    {
                        Relationship newRel = new()
                        {
                            EntityId = sId,
                            RelatedEntityId = tId,
                            EntityType = relationship.EntityType,
                            RelationshipTypeId = typeId,
                            Description = "Automatically added from suggested relationship."
                        };

                        if (reverse)
                        {
                            SwapRelationshipEntities(newRel);
                        }

                        // Just in case, check existence before adding to avoid unique constraint issues
                        bool exists = await repository.CountAsync<Relationship>(r => r.RelationshipTypeId == typeId &&
                            ((r.EntityId == newRel.EntityId && r.RelatedEntityId == newRel.RelatedEntityId) ||
                             (r.EntityId == newRel.RelatedEntityId && r.RelatedEntityId == newRel.EntityId))) > 0;

                        if (!exists)
                        {
                            await repository.AddAsync(newRel);
                        }
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
                                Value = c.Id.ToString(), Text = c.CompanyName, Selected = selectedId == c.Id
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
                    Value = $"{t.Id}_Fwd", Text = fwdText, Group = group, Selected = selectedValue == $"{t.Id}_Fwd"
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
            return
                [.. RelationshipTypeService.GetByEntityType(entityType).OrderBy(t => t.Category).ThenBy(t => t.Name)];
        }

        public async Task<RelationshipOperationResult> CreatePartialContactRelationshipAsync(Guid parentEntityId,
            string selectedRelationshipType, CreatePartialContactRelationshipDto dto)
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

            if (dto.SuggestedRelationships != null && dto.SuggestedRelationships.Count > 0)
            {
                foreach (string payload in dto.SuggestedRelationships)
                {
                    string[] parts = payload.Split('_');
                    if (parts.Length == 3 && Guid.TryParse(parts[0], out Guid sId) &&
                        Guid.TryParse(parts[1], out Guid tId) && bool.TryParse(parts[2], out bool reverse))
                    {
                        if (sId == Guid.Empty) sId = partialContact.Id;
                        if (tId == Guid.Empty) tId = partialContact.Id;

                        Relationship newRel = new()
                        {
                            EntityId = sId,
                            RelatedEntityId = tId,
                            EntityType = EntityTypes.Person,
                            RelationshipTypeId = typeId,
                            Description = "Automatically added from suggested relationship."
                        };

                        if (reverse)
                        {
                            SwapRelationshipEntities(newRel);
                        }

                        // Just in case, check existence before adding to avoid unique constraint issues
                        bool exists = await repository.CountAsync<Relationship>(r => r.RelationshipTypeId == typeId &&
                            ((r.EntityId == newRel.EntityId && r.RelatedEntityId == newRel.RelatedEntityId) ||
                             (r.EntityId == newRel.RelatedEntityId && r.RelatedEntityId == newRel.EntityId))) > 0;

                        if (!exists)
                        {
                            await repository.AddAsync(newRel);
                        }
                    }
                }
            }

            await repository.SaveChangesAsync();

            return RelationshipOperationResult.Ok(parentEntityId, EntityTypes.Person);
        }

        public async Task<List<SuggestedRelationshipDto>> GetSuggestedRelationshipsAsync(Guid entityId,
            Guid? relatedEntityId, Guid relationshipTypeId, bool isReverse, string? partialContactName)
        {
            List<SuggestedRelationshipDto> suggestions = [];

            bool isTransitive = RelationshipTypeService.TransitiveRelationshipTypeIds.Contains(relationshipTypeId);
            bool isFamilyAdultChild =
                RelationshipTypeService.FamilyAdultChildRelationshipTypeIds.Contains(relationshipTypeId);

            if (!isTransitive && !isFamilyAdultChild)
            {
                return suggestions;
            }

            RelationshipTypeDefinition? typeDef = RelationshipTypeService.GetById(relationshipTypeId);
            if (typeDef == null) return suggestions;

            Contact? entity = await repository.GetByIdAsync<Contact>(entityId);
            if (entity == null) return suggestions;
            string entityName = $"{entity.FirstName} {entity.LastName}".Trim();

            string relatedEntityName = partialContactName ?? string.Empty;
            if (relatedEntityId.HasValue)
            {
                Contact? relatedEntity = await repository.GetByIdAsync<Contact>(relatedEntityId.Value);
                if (relatedEntity != null)
                    relatedEntityName = $"{relatedEntity.FirstName} {relatedEntity.LastName}".Trim();
            }

            async Task<HashSet<Guid>> GetComponentAsync(Guid startId, Guid typeIdToSearch)
            {
                var comp = new HashSet<Guid>();
                var q = new Queue<Guid>();
                q.Enqueue(startId);
                comp.Add(startId);

                int maxNodes = 50;

                while (q.Count > 0 && comp.Count < maxNodes)
                {
                    var curr = q.Dequeue();
                    var edges = await repository.ListAsNoTrackingAsync<Relationship>(r =>
                        r.RelationshipTypeId == typeIdToSearch && (r.EntityId == curr || r.RelatedEntityId == curr));

                    foreach (var edge in edges)
                    {
                        var nbr = edge.EntityId == curr ? edge.RelatedEntityId : edge.EntityId;
                        if (comp.Add(nbr))
                        {
                            q.Enqueue(nbr);
                        }
                    }
                }

                return comp;
            }

            async Task AddSuggestionAsync(Guid sId, Guid tId, string sName, string tName, bool reverse)
            {
                if (sId != Guid.Empty && tId != Guid.Empty)
                {
                    bool exists = await repository.CountAsync<Relationship>(r =>
                        r.RelationshipTypeId == relationshipTypeId &&
                        ((r.EntityId == sId && r.RelatedEntityId == tId) ||
                         (r.EntityId == tId && r.RelatedEntityId == sId))) > 0;
                    if (exists) return;
                }

                string payload = $"{sId}_{tId}_{reverse}";
                // Avoid duplicates in suggestions (if multiple paths lead to same edge)
                if (!suggestions.Any(s => s.Payload == payload))
                {
                    suggestions.Add(new SuggestedRelationshipDto
                    {
                        Payload = payload,
                        SourceName = sName,
                        TargetName = tName,
                        RelationshipName = reverse ? typeDef.OppositeName : typeDef.Name
                    });
                }
            }

            if (isTransitive)
            {
                var compE = await GetComponentAsync(entityId, relationshipTypeId);
                var compR = relatedEntityId.HasValue
                    ? await GetComponentAsync(relatedEntityId.Value, relationshipTypeId)
                    : new HashSet<Guid> { Guid.Empty };

                // Batch-load all contacts from both components in two queries instead of one per node
                var compEIds = compE.Where(id => id != entityId).ToHashSet();
                var compRIds = compR.Where(id => id != Guid.Empty && id != relatedEntityId).ToHashSet();

                var allNeededIds = new HashSet<Guid>(compEIds.Concat(compRIds));
                List<Contact> batchContacts = allNeededIds.Count > 0
                    ? await repository.ListAsNoTrackingAsync<Contact>(c => allNeededIds.Contains(c.Id))
                    : [];
                Dictionary<Guid, Contact> contactMap = batchContacts.ToDictionary(c => c.Id);

                foreach (var x in compEIds)
                {
                    if (contactMap.TryGetValue(x, out Contact? xContact))
                    {
                        string xName = $"{xContact.FirstName} {xContact.LastName}".Trim();
                        Guid tId = relatedEntityId ?? Guid.Empty;
                        await AddSuggestionAsync(x, tId, xName, relatedEntityName, isReverse);
                    }
                }

                foreach (var y in compRIds)
                {
                    if (contactMap.TryGetValue(y, out Contact? yContact))
                    {
                        string yName = $"{yContact.FirstName} {yContact.LastName}".Trim();
                        await AddSuggestionAsync(entityId, y, entityName, yName, isReverse);
                    }
                }
            }
            else if (isFamilyAdultChild)
            {
                Guid adultId = isReverse ? (relatedEntityId ?? Guid.Empty) : entityId;
                Guid childId = isReverse ? entityId : (relatedEntityId ?? Guid.Empty);

                string adultName = isReverse ? relatedEntityName : entityName;

                if (childId != Guid.Empty)
                {
                    var childSiblings = await GetComponentAsync(childId, RelationshipTypeIds.Sibling);
                    var siblingIds = childSiblings.Where(id => id != childId).ToHashSet();

                    // Batch-load all sibling contacts in one query
                    List<Contact> sibContacts = siblingIds.Count > 0
                        ? await repository.ListAsNoTrackingAsync<Contact>(c => siblingIds.Contains(c.Id))
                        : [];

                    foreach (Contact sibContact in sibContacts)
                    {
                        string sibName = $"{sibContact.FirstName} {sibContact.LastName}".Trim();
                        await AddSuggestionAsync(adultId, sibContact.Id, adultName, sibName, false);
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
