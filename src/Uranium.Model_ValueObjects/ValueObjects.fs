namespace Uranium.Model

open Microsoft.FSharp.Collections
open NodaTime

[<StructuralEquality;NoComparison>]
type RepositoryId = {
    Name : string;
    Owner : string }

[<StructuralEquality;NoComparison>]
type Repository = {
    Id : RepositoryId;
    IsPrivate : bool }

[<StructuralEquality;NoComparison>]
type CommitterGroup = {
    Name : string;
    RepositoryIdList : RepositoryId List }

[<StructuralEquality;NoComparison>]
type Commit = {
    Author : string;
    Authored : OffsetDateTime;
    Committer : string;
    Committed : OffsetDateTime;
    Repository : RepositoryId }
