1. **Fix Performance Bug**: `GetComponentAsync` fetches ALL edges of `typeIdToSearch`. Instead, we should do iterative querying, OR restrict it to a smaller subgraph. Wait, fetching 1 hop at a time from DB is slow if the component is large. But we can limit the depth to, say, 2 hops. The request said "do not stop at one level deep", so 2 hops is fine. Or we just do BFS up to a max number of nodes (e.g. 50).
Actually, doing iterative queries in a loop is fine since the component size is usually small (e.g., < 20 colleagues).
```csharp
            async Task<HashSet<Guid>> GetComponentAsync(Guid startId, Guid typeIdToSearch)
            {
                var comp = new HashSet<Guid>();
                var q = new Queue<Guid>();
                q.Enqueue(startId);
                comp.Add(startId);

                int maxNodes = 50; // Prevent infinite loops or massive fetches

                while (q.Count > 0 && comp.Count < maxNodes)
                {
                    var curr = q.Dequeue();
                    var edges = await repository.ListAsNoTrackingAsync<Relationship>(
                        r => r.RelationshipTypeId == typeIdToSearch && (r.EntityId == curr || r.RelatedEntityId == curr));

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
```
2. **Fix XSS Bug**: In `Create.cshtml`, change `.innerHTML = ...` to DOM element creation for strong tags and text nodes.
```javascript
                                const label = document.createElement('label');
                                label.className = 'form-check-label pt-1';
                                label.htmlFor = input.id;

                                label.appendChild(document.createTextNode('Set '));
                                const strong1 = document.createElement('strong');
                                strong1.textContent = sugg.sourceName;
                                label.appendChild(strong1);

                                label.appendChild(document.createTextNode(' as '));
                                const strong2 = document.createElement('strong');
                                strong2.textContent = sugg.relationshipName;
                                label.appendChild(strong2);

                                label.appendChild(document.createTextNode(' of '));
                                const strong3 = document.createElement('strong');
                                strong3.textContent = sugg.targetName;
                                label.appendChild(strong3);
```

Let's do this!
