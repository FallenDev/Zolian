using Xunit;

using ZolianTest.Integration.Fixtures;

namespace ZolianTest.Integration.Collections;

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection
    : ICollectionFixture<ZolianHostFixture>
{
}
