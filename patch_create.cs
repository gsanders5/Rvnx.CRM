<<<<<<< SEARCH
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
=======
            await repository.AddAsync(relationship);

            if (suggestedEntityIds != null && suggestedEntityIds.Count > 0)
            {
                foreach (string payload in suggestedEntityIds)
                {
                    string[] parts = payload.Split('_');
                    if (parts.Length == 3 && Guid.TryParse(parts[0], out Guid sId) && Guid.TryParse(parts[1], out Guid tId) && bool.TryParse(parts[2], out bool reverse))
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
>>>>>>> REPLACE
