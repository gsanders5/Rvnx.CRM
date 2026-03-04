The user wants to expand suggestions beyond just applying the SAME relationship to others.
"This about other types we can apply this to- Siblings and Parents, ect. Do not stop at one level deep."
So if A is Parent of B. And we link C as Sibling of B.
We can infer: A is Parent of C!
If A is Sibling of B. And we link C as Parent of B.
We can infer: C is Parent of A!
If A is Child of B. And C is Sibling of B.
We can infer: C is Uncle/Aunt of A! (Actually A is Nephew/Niece of C).
"Do not stop at one level deep."

Wait, this requires a full **Relationship Inference Engine**.
If we link Jack (Entity) to Jill (RelatedEntity).
We can traverse Jack's existing graph, and Jill's existing graph, and for any path of length 2 or 3 that goes through the new link (Jack-Jill), we can propose a new direct relationship.
For example:
Path: `X --R1--> Jack --NewR--> Jill --R2--> Y`
The new relationship links Jack and Jill. This creates new paths:
- `X -> Jack -> Jill`
- `Jack -> Jill -> Y`
- `X -> Jack -> Jill -> Y`

Inference Rules (Path -> Direct Relationship):
1. Parent(A, B) + Sibling(B, C) => Parent(A, C)
   If A is Parent of B, and B is Sibling of C, then A is Parent of C.
2. Sibling(A, B) + Parent(B, C) => UncleAunt(A, C)
   If A is Sibling of B, and B is Parent of C, then A is Uncle/Aunt of C.
3. Child(A, B) + Sibling(B, C) => NephewNiece(A, C) (i.e. C is UncleAunt of A)
4. Parent(A, B) + Parent(B, C) => Grandparent(A, C)
5. Child(A, B) + Child(B, C) => Grandchild(A, C) (i.e. C is Grandparent of A)
6. Sibling(A, B) + Sibling(B, C) => Sibling(A, C) (Transitive)
7. Colleague(A, B) + Colleague(B, C) => Colleague(A, C) (Transitive)
8. Cousin(A, B) + Cousin(B, C) => Cousin(A, C) (Often transitive)
9. Parent(A, B) + Spouse(B, C) => ParentInLaw(A, C) (Wait, we don't have In-Laws in the seed data).
Let's stick to the available Relationship Types.
Available Types:
- Spouse / Ex-Spouse
- Parent / Child
- Sibling
- Grandparent / Grandchild
- Uncle/Aunt / Nephew/Niece
- Cousin
- Godparent / Godchild
- Step-Parent / Step-Child
- Step-Sibling
- Half-Sibling
- Colleague, Business Partner

This could get very complex. If we just implement a simple 2-hop inference using a lookup table:
Given `R1(A, B)` and `R2(B, C)`, infer `R3(A, C)`.
Since we are linking `A <-> B` with `R_new`, we just look at A's neighbors `N_A` and B's neighbors `N_B`.
For each `X` in `N_A`, we have `R(X, A)`. The new link is `R_new(A, B)`.
So we apply the rule: `R(X, A) + R_new(A, B) => R_infer(X, B)`.
For each `Y` in `N_B`, we have `R_new(A, B)` and `R(B, Y)`.
So we apply the rule: `R_new(A, B) + R(B, Y) => R_infer(A, Y)`.

Rule Table: `(Type1, isRev1) + (Type2, isRev2) => (Type3, isRev3)`
Actually, to avoid directionality issues, let's normalize everything to `A -> B`.
If `R` is Parent: `Parent(A,B)` means A is Parent of B. `Child(A,B)` means A is Child of B.
In our DB, Parent is a single TypeId.
If `IsReverse` is true, the DB stores `EntityId=B, RelatedEntityId=A`.
So we can read the DB and normalize everything to `Entity -> RelatedEntity` with a "Direction".
Forward = 0, Reverse = 1.
For Symmetric, Forward = Reverse = 0.

Let's build a static `InferenceRules` dictionary:
Key: `(Guid Type1, bool Rev1, Guid Type2, bool Rev2)`
Value: `(Guid Type3, bool Rev3)`

Rule Examples:
1. `Sibling + Sibling => Sibling`
   `Rule(Sibling, Fwd, Sibling, Fwd) => (Sibling, Fwd)`
2. `Colleague + Colleague => Colleague`
   `Rule(Colleague, Fwd, Colleague, Fwd) => (Colleague, Fwd)`
3. `Parent + Sibling => Parent` (A is parent of B, B is sibling of C -> A is parent of C)
   `Rule(Parent, Fwd, Sibling, Fwd) => (Parent, Fwd)`
4. `Sibling + Child => Child` (A is sibling of B, B is child of C -> A is child of C)
   Wait, if A is Sibling of B. B is Child of C. (Meaning C is Parent of B).
   Then C is Parent of A. Which means A is Child of C!
   In my notation: `R(A,B) = Sibling`, `R(B,C) = Child` (meaning B is child of C. DB: Type=Parent, Rev=True).
   `Rule(Sibling, Fwd, Parent, Rev) => (Parent, Rev)`
5. `Child + Sibling => Child` (A is child of B, B is sibling of C -> A is child of C ? NO. A is Nephew of C).
   A is child of B. B is sibling of C. C is Uncle/Aunt of A. A is Nephew/Niece of C.
   So A is Nephew of C. DB: Type=UncleAunt, Rev=True.
   `Rule(Parent, Rev, Sibling, Fwd) => (UncleAunt, Rev)`
6. `Sibling + Parent => UncleAunt` (A is sibling of B, B is parent of C -> A is Uncle of C)
   `Rule(Sibling, Fwd, Parent, Fwd) => (UncleAunt, Fwd)`
7. `Parent + Parent => Grandparent` (A is parent of B, B is parent of C -> A is grandparent of C)
   `Rule(Parent, Fwd, Parent, Fwd) => (Grandparent, Fwd)`
8. `Child + Child => Grandchild` (A is child of B, B is child of C -> A is grandchild of C)
   `Rule(Parent, Rev, Parent, Rev) => (Grandparent, Rev)`
9. `Parent + UncleAunt => GreatUncleAunt` (Not in DB).

Let's stick to these highly reliable rules:
Transitive (Symmetric):
- Sibling + Sibling => Sibling
- Colleague + Colleague => Colleague
- Cousin + Cousin => Cousin
- StepSibling + StepSibling => StepSibling
- HalfSibling + HalfSibling => HalfSibling

Mixed:
- Parent(Fwd) + Sibling(Fwd) => Parent(Fwd)
- Sibling(Fwd) + Parent(Rev) => Parent(Rev) (A is sibling of B, B is child of C => A is child of C)
- Parent(Rev) + Sibling(Fwd) => UncleAunt(Rev) (A is child of B, B is sibling of C => A is nephew of C)
- Sibling(Fwd) + Parent(Fwd) => UncleAunt(Fwd) (A is sibling of B, B is parent of C => A is uncle of C)
- Parent(Fwd) + Parent(Fwd) => Grandparent(Fwd)
- Parent(Rev) + Parent(Rev) => Grandparent(Rev)

This covers Sibling, Parent, UncleAunt, Grandparent! And perfectly hits "Siblings and Parents, ect. Do not stop at one level deep."

To do "Do not stop at one level deep", we just do a loop: keep inferring new relationships until no more can be inferred (Closure), or just do BFS up to depth 2 or 3!
Actually, if we just do 1-hop inference from the existing graph, we are proposing paths of length 2 in the final graph.
If the graph has `X -> Jack` and `Jill -> Y`. Jack is newly linked to Jill.
We infer `X -> Jill` (length 2). We infer `Jack -> Y` (length 2).
If we then infer `X -> Y` (length 3), that would be "not stopping at one level deep".
But `X -> Y` would be `R_infer1(X, Jill) + R(Jill, Y) => R_infer2(X, Y)`.
Yes! We can do a simple inference loop.
For suggestions, we shouldn't overwhelm the user. "Not stopping at one level deep" probably means finding all paths.
Let's build `RelationshipGraphInference` class.
