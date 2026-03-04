Wait, if A is Colleague of B. C is added as Colleague to A.
"Is C also a Colleague of B?" Yes.
This translates to:
When I am on A's profile, and I click "Add Relationship", and I link C as Colleague.
A = Entity (Jack). C = RelatedEntity (Jill).
A already has relationship with B (James, Carol) of type Colleague.
So we should ask: "Is C (Jill) also a Colleague of B (James) and B (Carol)?"
This corresponds exactly to my second scenario.
If we go to C's profile (Jill) and click "Add Relationship", we link A (Jack) as Colleague.
C has no colleagues. A has colleagues B (James, Carol).
We should ask: "Is A (Jack) also a Colleague of B (James) and B (Carol)?" wait, A is already a colleague! We should ask if C (Jill) is also a colleague of B (James) and B (Carol)!
Wait, if A already has colleagues B, and we link C to A, we want to create relationship between C and B.

What if the relationship is asymmetric, like Parent/Child?
If A is Parent of B. C is added as Parent of A.
Should C be Parent of B? No. Grandparent of B.
The feature request mentions: "where it makes sense that the contact you are linking to's relationship might apply"
"This also should apply to siblings, cousins, basically most relationships. Things like "friend" could exclude this check though."
Relationships where this makes sense:
Sibling (Symmetric)
Colleague (Symmetric)
Cousin (Symmetric)
So basically symmetric relationships (except maybe Friend)?
Wait, Sibling is transitive (mostly). Colleague is often transitive (cliques). Cousin is not always transitive, but often is.
What about asymmetric?
If A is Child of B, and C is Child of B, A and C are Siblings. But that's inferring new types!
The prompt explicitly says "apply for every other related Colleague".
"Is Jack also a Colleague of Jill? Yes / No" -> wait, Jack is being linked to Jill!
"Is Jack also a Colleague of James? Yes / No"
"Is Jack also a Colleague of Carol? Yes / No"
This implies we are adding Jack to someone, and Jill, James, Carol are already colleagues of that someone!
Let's reread carefully: "Is Jill, James, and Carol are already linked as Colleague, if we go in the UI to link Jack to Jill, ask for if the relationship applies for every other related Colleague. IE: Is Jack also a Colleague of James? Is Jack also a Colleague of Carol?"
Ah! "If Jill, James, and Carol are already linked as Colleague". Wait, linked to whom?
Oh, Jill is linked to James and Carol as Colleague! (Jill -> James, Jill -> Carol).
Then we go to link Jack to Jill as Colleague.
Then Jack is also a Colleague of James and Carol?
Yes! The user means: Jill has colleagues James and Carol. We are linking Jack as a colleague to Jill. So should Jack also be colleagues with Jill's colleagues (James and Carol)?
Yes! This is what I said earlier: we are finding the existing relationships of the **target** (Jill, the `RelatedEntity`), and asking if they should apply to the **source** (Jack, the `Entity`).

So:
`EntityId` = Jack
`RelatedEntityId` = Jill
`TypeId` = Colleague

1. Find all relationships where (`EntityId == Jill` OR `RelatedEntityId == Jill`) AND `TypeId == Colleague` AND the other contact is not Jack.
2. Let the other contacts be James, Carol.
3. Ask: "Is Jack also a Colleague of James?", "Is Jack also a Colleague of Carol?"
4. Create new relationships `Jack -> James` and `Jack -> Carol`.

What if Jack creates a Partial Contact "Jill" as a Colleague?
Jill doesn't exist yet, so she doesn't have any colleagues!
However, Jack might have colleagues James and Carol.
When Jack creates Partial Contact "Jill" as a Colleague, should we ask if "Jill is also a Colleague of James and Carol"?
Yes! The logic is mirrored:
`EntityId` = Jack
`RelatedEntity` = New Partial Contact (Jill)
`TypeId` = Colleague
Find all relationships where (`EntityId == Jack` OR `RelatedEntityId == Jack`) AND `TypeId == Colleague`.
Let the other contacts be James, Carol.
Ask: "Is Jill also a Colleague of James?", "Is Jill also a Colleague of Carol?"
Create new relationships `Jill -> James` and `Jill -> Carol`.

To generalize this:
Whenever we add `Source -> Target` as `Type`:
1. Find existing contacts `C` that have relationship `Type` with `Target`. Offer to add `Source -> C` as `Type`.
2. Find existing contacts `C` that have relationship `Type` with `Source`. Offer to add `Target -> C` as `Type`.

Since `Source -> Target` is the same as `Target -> Source` for symmetric relationships, this logic is perfectly dual.
Wait, what if the relationship is asymmetric, like `Manager / Employee`?
If Jack is Employee of Jill. Jill has other Employees (James, Carol).
Should Jack be Employee of James? No! Jack is colleague of James!
So the prompt says: "This also should apply to siblings, cousins, basically most relationships. Things like "friend" could exclude this check though."
If we only apply it to symmetric relationships (where `IsSymmetric == true`), except maybe `Friend`, `BestFriend`, `Acquaintance`.
Let's filter for `IsSymmetric == true` and `TypeId NOT IN (Friend, BestFriend, Acquaintance, SignificantOther, ExPartner)`.
Symmetric relationships that make sense:
- Sibling
- Cousin
- StepSibling
- HalfSibling
- Colleague
- BusinessPartner

Wait, if Jack is Sibling of Jill. Jill has Sibling (James).
Is Jack also Sibling of James? Yes, mostly.
If Jack is Colleague of Jill. Jill has Colleague (James).
Is Jack also Colleague of James? Maybe, that's why we prompt.
So, the suggestions should ONLY appear for certain Relationship Types.
