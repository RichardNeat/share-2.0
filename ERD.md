# Entity relationship diagram

The database as built: EF Core code-first on SQLite. Generated from `src/Share.Web/Models` and `Data/AppDbContext.cs`. Keep it in step with every migration.

`PK` primary key, `FK` foreign key, `UK` unique. Optional columns are marked "optional".

![Entity relationship diagram of the Share database: visa applications with their guests, decision updates, sponsors and hosts (people), accommodations and cases; cases with their safeguarding checks, offers and timeline events; plus ingest runs, duplicate dismissals, announcements and notifications. The Mermaid source below lists every table and column.](ERD.svg)

The image is generated from the Mermaid source below. After changing the source, run `node --experimental-websocket tools/render-erd.mjs` to regenerate `ERD.svg`.

```mermaid
erDiagram
    IngestRun ||--o{ VisaApplication : "added"
    IngestRun ||--o{ DecisionUpdate : "added"
    IngestRun ||--o{ SkippedRow : "skipped"
    IngestRun |o--o{ TimelineEvent : "logged"

    VisaApplication ||--|{ Guest : "lists"
    Guest ||--o{ ApplicationAnswer : "answered"
    Guest |o--o{ Guest : "duplicate of"
    VisaApplication ||--o{ DecisionUpdate : "updated by"
    Person |o--o{ VisaApplication : "sponsors"
    Person |o--o{ VisaApplication : "hosts"
    Accommodation |o--o{ VisaApplication : "staying at"
    Case |o--o{ VisaApplication : "groups"

    Person |o--o{ Case : "sponsors"
    Person |o--o{ Case : "hosts after rematch"
    Accommodation |o--o{ Case : "houses"
    Case ||--o{ CaseCheck : "checked by"
    Case |o--o{ Offer : "took"

    Case |o--o{ TimelineEvent : "history"
    VisaApplication |o--o{ TimelineEvent : "about"
    Offer |o--o{ TimelineEvent : "about"

    IngestRun {
        int Id PK
        string FileName UK
        int FileNumber
        datetime ProcessedAt
        int RecordsInFile
        int Added
        int AlreadyPresent
        int PeopleAdded
        int AccommodationsAdded
        int CasesAdded
        int StatusesChanged
        int Arrivals
    }
    SkippedRow {
        int Id PK
        int IngestRunId FK
        int RowNumber
        string Reference "optional"
        string Reason
    }
    VisaApplication {
        int Id PK
        string SubmissionGuid UK
        string Uan "indexed"
        string Gwf "optional, indexed"
        datetime SubmittedAt
        string Status "VisaStatus, derived from decision updates"
        string SponsorGivenName "optional, as given"
        string SponsorFamilyName "optional, as given"
        string Council "optional, from the accommodation address"
        bool StayingWithSponsor "optional"
        string HostGivenName "optional, as given"
        string HostFamilyName "optional, as given"
        int SponsorId FK "optional"
        int HostId FK "optional"
        int AccommodationId FK "optional"
        int CaseId FK "optional"
        int IngestRunId FK
    }
    Guest {
        int Id PK
        int VisaApplicationId FK
        int Position "1 is the lead applicant"
        string Role "optional"
        string GivenName "optional"
        string FamilyName "optional"
        date DateOfBirth "optional"
        string Nationality "optional"
        string PassportNumber "optional"
        string Gwf "optional, indexed"
        int DuplicateOfGuestId FK "optional"
        datetime MarkedDuplicateAt "optional"
    }
    ApplicationAnswer {
        int Id PK
        int GuestId FK
        int Position
        string Title
        string Answer "optional"
    }
    DecisionUpdate {
        int Id PK
        int VisaApplicationId FK
        int IngestRunId FK
        int RowNumber "UK with IngestRunId"
        string Gwf "optional"
        string Uan "optional"
        date DecisionDate "optional"
        string Decision "optional"
        string VoyageCode "optional"
        string ArrivalPort "optional"
        datetime ArrivedAt "optional, UTC"
        string PersonIdentifier "optional"
    }
    Person {
        int Id PK
        string MatchKey UK "email, or one record per application"
        string GivenName "optional"
        string FamilyName "optional"
        date DateOfBirth "optional"
        string Email "optional"
        string Telephone "optional"
        string Address "optional"
        string Postcode "optional"
        string Council "optional"
    }
    Accommodation {
        int Id PK
        string MatchKey UK "address and council"
        string Address
        string Postcode "optional"
        string Council "optional"
    }
    Case {
        int Id PK "shown as CASE-0001"
        string MatchKey UK "sponsor and accommodation"
        int SponsorId FK "optional"
        int AccommodationId FK "optional"
        int HostId FK "optional, set by a rematch"
        string Council "optional"
        datetime RematchedAt "optional"
        date ArrivalRecordedOn "optional, by a caseworker"
        datetime ArrivalRecordedAt "optional"
    }
    CaseCheck {
        int Id PK
        int CaseId FK "UK with Kind"
        string Kind "CheckKind 1 to 4"
        string Status "CheckStatus"
        string FailureReason "optional, only when failed"
        string DbsType "optional, check 3 only"
        datetime UpdatedAt
    }
    Offer {
        int Id PK
        string SubmissionReference UK
        datetime SubmittedAt
        string HostName "optional"
        string Email "optional"
        string Telephone "optional"
        string Address1 "optional"
        string Address2 "optional"
        string TownOrCity "optional"
        string Postcode "optional"
        string Council "optional, from the town or city"
        date AvailableFrom "optional"
        int Adults "optional"
        int Children "optional"
        int Bedrooms "optional"
        bool StepFree "optional"
        bool Pets "optional"
        string Status "Open or Taken"
        int TakenByCaseId FK "optional"
        datetime TakenAt "optional"
    }
    TimelineEvent {
        int Id PK
        datetime OccurredAt "from the data for data events"
        string Kind "TimelineEventKind"
        string Title
        string Description "optional"
        int VisaApplicationId FK "optional"
        int CaseId FK "optional"
        int OfferId FK "optional"
        int IngestRunId FK "optional"
    }
    DuplicateDismissal {
        int Id PK
        string PairKey UK "UAN#position pair"
        datetime DismissedAt
    }
    Announcement {
        int Id PK
        string Title
        string Body
        string Link "optional"
        datetime CreatedAt
        datetime PublishAt "optional"
        bool IsHidden
    }
    Notification {
        int Id PK
        string UserId "indexed with IsRead"
        string EventType "NotificationEventType"
        string Title
        string Description "optional"
        bool IsRead
        datetime CreatedAt
        string RelatedEntityType "optional"
        int RelatedEntityId "optional"
    }
```

## Notes

- **Not stored, worked out on read:** case status (from the four checks, `SafeguardingRules`), the Enhanced DBS flag and guests' ages (from dates of birth and the pinned date), "needs rematch" (any failed check), suggested duplicates (`DuplicateFinder`), the case title and its `CASE-0001` reference.
- **Stored but derived:** a visa application's `Status` is re-derived from all its decision updates on every arrivals ingest.
- **Linked by value, not by foreign key:** `DuplicateDismissal.PairKey` names two guests by application UAN and position, so it survives a reseed. `Notification.UserId` and `RelatedEntityType`/`RelatedEntityId` point at a demo user and any record. Demo users live in code (`Users/DemoUser.cs`) and a cookie, not the database.
- **Council scoping (M6):** EF global query filters limit every scoped table to the signed-in council: visa applications, guests, people, accommodations, cases, decision updates, offers and timeline events. Ingest reads with `IgnoreQueryFilters`.
- **Enums are stored as text:** `VisaStatus`, `CheckKind`, `CheckStatus`, `DbsType`, `OfferStatus`, `TimelineEventKind`, `NotificationEventType`.
