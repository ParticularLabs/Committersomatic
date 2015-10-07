namespace Uranium.Model

open NodaTime

[<StructuralEquality;NoComparison>]
type Repository = {
    Name : string;
    Owner : string }

[<StructuralEquality;NoComparison>]
type Commit = {
    Author : string;
    Authored : OffsetDateTime;
    Committer : string;
    Committed : OffsetDateTime;
    Repository : Repository }
