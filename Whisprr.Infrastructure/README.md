# Infrastructure

Contains modules for database, MQ, auth, etc.

We set indexes, uniqeness, etc. using Fluent API to avoid bloating the Entities class library with EF Core specific coupling.