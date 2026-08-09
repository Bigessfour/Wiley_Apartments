namespace Wiley.Apartments.E2ETests;

[CollectionDefinition("E2E", DisableParallelization = true)]
public sealed class E2ECollection : ICollectionFixture<E2EWebApplicationFactory>;
