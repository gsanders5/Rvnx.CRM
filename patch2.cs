<<<<<<< SEARCH
        public async Task<RelationshipOperationResult> PromotePartialContactAsync(Guid contactId)
=======
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
>>>>>>> REPLACE
