<<<<<<< SEARCH
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
=======
        public async Task<List<SuggestedRelationshipDto>> GetSuggestedRelationshipsAsync(Guid entityId, Guid? relatedEntityId, Guid relationshipTypeId, bool isReverse, string? partialContactName)
        {
            List<SuggestedRelationshipDto> suggestions = [];

            bool isTransitive = RelationshipTypeService.TransitiveRelationshipTypeIds.Contains(relationshipTypeId);
            bool isFamilyAdultChild = RelationshipTypeService.FamilyAdultChildRelationshipTypeIds.Contains(relationshipTypeId);

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
                if (relatedEntity != null) relatedEntityName = $"{relatedEntity.FirstName} {relatedEntity.LastName}".Trim();
            }

            async Task<HashSet<Guid>> GetComponentAsync(Guid startId, Guid typeIdToSearch)
            {
                var comp = new HashSet<Guid>();
                var q = new Queue<Guid>();
                q.Enqueue(startId);
                comp.Add(startId);

                var allEdges = await repository.ListAsNoTrackingAsync<Relationship>(r => r.RelationshipTypeId == typeIdToSearch);
                var adj = new Dictionary<Guid, List<Guid>>();
                foreach (var edge in allEdges)
                {
                    if (!adj.ContainsKey(edge.EntityId)) adj[edge.EntityId] = [];
                    if (!adj.ContainsKey(edge.RelatedEntityId)) adj[edge.RelatedEntityId] = [];
                    adj[edge.EntityId].Add(edge.RelatedEntityId);
                    adj[edge.RelatedEntityId].Add(edge.EntityId);
                }

                while (q.Count > 0)
                {
                    var curr = q.Dequeue();
                    if (adj.TryGetValue(curr, out var nbrs))
                    {
                        foreach (var nbr in nbrs)
                        {
                            if (comp.Add(nbr))
                            {
                                q.Enqueue(nbr);
                            }
                        }
                    }
                }
                return comp;
            }

            async Task AddSuggestionAsync(Guid sId, Guid tId, string sName, string tName, bool reverse)
            {
                if (sId != Guid.Empty && tId != Guid.Empty)
                {
                    bool exists = await repository.CountAsync<Relationship>(r => r.RelationshipTypeId == relationshipTypeId &&
                        ((r.EntityId == sId && r.RelatedEntityId == tId) || (r.EntityId == tId && r.RelatedEntityId == sId))) > 0;
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
                var compR = relatedEntityId.HasValue ? await GetComponentAsync(relatedEntityId.Value, relationshipTypeId) : new HashSet<Guid> { Guid.Empty };

                foreach (var x in compE)
                {
                    if (x == entityId) continue;
                    Contact? xContact = await repository.GetByIdAsync<Contact>(x);
                    if (xContact != null)
                    {
                        string xName = $"{xContact.FirstName} {xContact.LastName}".Trim();
                        Guid tId = relatedEntityId ?? Guid.Empty;
                        await AddSuggestionAsync(x, tId, xName, relatedEntityName, isReverse);
                    }
                }

                foreach (var y in compR)
                {
                    if (y == relatedEntityId || y == Guid.Empty) continue;
                    Contact? yContact = await repository.GetByIdAsync<Contact>(y);
                    if (yContact != null)
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
                string childName = isReverse ? entityName : relatedEntityName;

                if (childId != Guid.Empty)
                {
                    var childSiblings = await GetComponentAsync(childId, RelationshipTypeIds.Sibling);
                    foreach (var sibling in childSiblings)
                    {
                        if (sibling == childId) continue;
                        Contact? sibContact = await repository.GetByIdAsync<Contact>(sibling);
                        if (sibContact != null)
                        {
                            string sibName = $"{sibContact.FirstName} {sibContact.LastName}".Trim();
                            await AddSuggestionAsync(adultId, sibling, adultName, sibName, false);
                        }
                    }
                }
            }

            return suggestions;
        }
>>>>>>> REPLACE
