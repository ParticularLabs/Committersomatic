namespace Uranium.Model

open NodaTime

type RepositoryId = {
    Owner : string;
    Name : string; }

type Repository = {
    Id : RepositoryId;
    IsPrivate : bool; }

type CommitterGroup = {
    Name : string;
    RepositoryList : RepositoryId List; }

type Commit = {
    Repository : RepositoryId;
    Committed : OffsetDateTime;
    Committer : string;
    Authored : OffsetDateTime;
    Author : string; }

type Issue = {
    Repository : RepositoryId;
    Created : OffsetDateTime;
    Creator : string; }

type PullRequest = {
    Repository : RepositoryId;
    Created : OffsetDateTime;
    Creator : string; }

type Contribution = {
    Group : string;
    Login : string;
    Score : double }
