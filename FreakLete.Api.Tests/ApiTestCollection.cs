namespace FreakLete.Api.Tests;

/// <summary>
/// Collection definition that shares a single FreakLeteApiFactory across all test classes.
/// This prevents parallel test classes from dropping each other's database.
/// </summary>
[CollectionDefinition("Api")]
public class ApiTestCollection : ICollectionFixture<FreakLeteApiFactory>;
