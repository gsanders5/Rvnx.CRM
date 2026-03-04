```csharp
public static class RelationshipInferenceRules
{
    // A -> B -> C => A -> C
    // Tuple: (TypeId1, IsRev1, TypeId2, IsRev2) => (TypeId3, IsRev3)
    public static readonly Dictionary<(Guid, bool, Guid, bool), (Guid, bool)> Rules = new()
    {
        // Transitive
        { (RelationshipTypeIds.Sibling, false, RelationshipTypeIds.Sibling, false), (RelationshipTypeIds.Sibling, false) },
        { (RelationshipTypeIds.Colleague, false, RelationshipTypeIds.Colleague, false), (RelationshipTypeIds.Colleague, false) },
        { (RelationshipTypeIds.Cousin, false, RelationshipTypeIds.Cousin, false), (RelationshipTypeIds.Cousin, false) },
        { (RelationshipTypeIds.BusinessPartner, false, RelationshipTypeIds.BusinessPartner, false), (RelationshipTypeIds.BusinessPartner, false) },

        // Parent + Sibling
        { (RelationshipTypeIds.Parent, false, RelationshipTypeIds.Sibling, false), (RelationshipTypeIds.Parent, false) },
        { (RelationshipTypeIds.Sibling, false, RelationshipTypeIds.Parent, true), (RelationshipTypeIds.Parent, true) },

        // Parent + Parent
        { (RelationshipTypeIds.Parent, false, RelationshipTypeIds.Parent, false), (RelationshipTypeIds.Grandparent, false) },
        { (RelationshipTypeIds.Parent, true, RelationshipTypeIds.Parent, true), (RelationshipTypeIds.Grandparent, true) },

        // Sibling + Parent => Uncle/Aunt
        { (RelationshipTypeIds.Sibling, false, RelationshipTypeIds.Parent, false), (RelationshipTypeIds.UncleAunt, false) },
        { (RelationshipTypeIds.Parent, true, RelationshipTypeIds.Sibling, false), (RelationshipTypeIds.UncleAunt, true) },

        // Grandparent + Sibling (Wait, Grandparent(A,B) + Sibling(B,C) => Grandparent(A,C) ?)
        // We probably don't need this if we infer Parent(A,C) then Grandparent(A,C).
    };

    // Add symmetrical counterparts for IsSymmetric = true types.
    // If a type is symmetric, IsRev is always false in the rules.
    // But in DB, it might be stored either way?
    // Actually, when fetching, we normalize Symmetric types to always have IsRev = false.
}
```

Graph representation:
A list of edges: `(Guid Node1, Guid Node2, Guid TypeId, bool IsRev)`.
When we say "IsRev", it means Node1 is the RelatedEntityId, and Node2 is the EntityId.
So the relationship goes `Node1 --(Type, IsRev)--> Node2`.
If IsRev == true, Node1 is the Child, Node2 is the Parent.
If IsRev == false, Node1 is the Parent, Node2 is the Child.
Wait, if `Type = Parent`, `IsRev = false`, it means `Node1 -> Node2` is `Parent -> Child`.
So Node1 is Parent of Node2.
If `IsRev = true`, Node1 is Child of Node2.

Let's do Graph Closure:
```csharp
List<Edge> edges = GetEdgesFromDB();
bool changed = true;
while (changed)
{
    changed = false;
    // For every pair of edges A->B and B->C
    foreach (var e1 in edges)
    {
        foreach (var e2 in edges)
        {
            if (e1.Node2 == e2.Node1)
            {
                var ruleKey = (e1.TypeId, e1.IsRev, e2.TypeId, e2.IsRev);
                if (Rules.TryGetValue(ruleKey, out var result))
                {
                    var newEdge = new Edge(e1.Node1, e2.Node2, result.Item1, result.Item2);
                    if (!edges.Contains(newEdge))
                    {
                        edges.Add(newEdge);
                        changed = true;
                    }
                }
            }
        }
    }
}
```
Wait, if we loop over `edges` while adding to it, we get collection modified exception.
We can add to `newEdges` list and then merge.
This is O(N^3) or worse, but N is small (the immediate neighborhood of Jack and Jill).
Wait, we only care about new edges involving Jack or Jill!
Actually, we only want to suggest edges that connect `Jack` (or `Jill`) to someone else.
Or someone connected to `Jack` to someone connected to `Jill`!
Since we are evaluating a specific "New Edge" `Jack -> Jill`.
We can just seed the graph with `Jack`'s immediate neighborhood, `Jill`'s immediate neighborhood, AND the new edge `Jack -> Jill`.
Then run closure.
Any inferred edge that didn't exist before is a suggestion!
Wait, if Jack is already Grandparent of someone, the closure might infer things that already exist. We just filter those out.
For suggestions, we want to suggest edges that:
1. Did not exist in the DB.
2. Involve `Jack` or `Jill` directly?
   Or involve `Jack`'s neighbors and `Jill`'s neighbors?
   "Is Jack also a Colleague of James?" -> Involves Jack.
   "Is Jill also a Colleague of Bob?" -> Involves Jill.
   "Is Jack's brother Bob also a Colleague of Jill's friend James?" -> Too far. The prompt says "ask the user if the relationship applies to the contact as well". "Is Jack also a Colleague of James?" "This also should apply to siblings, cousins, basically most relationships."
   So the suggestions should ALWAYS have `Jack` (the source) or `Jill` (the target) as one of the nodes!
   Actually, if `Jack -> Jill` is a Sibling link.
   And `Jill -> James` is a Parent link (Jill is parent of James).
   We infer `Jack -> James` is Uncle/Aunt. (Jack is uncle of James).
   Does this involve Jack? Yes!
   So any new edge that involves Jack or Jill is a valid suggestion.
   Actually, any new edge is a consequence of adding `Jack -> Jill`.
