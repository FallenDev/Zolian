using Darkages.Network.Server;

using Xunit;

using ZolianTest.Integration.Fixtures;

namespace ZolianTest.Integration.Boot;

[Collection("Integration")]
public sealed class TemplateCacheInvariantsTests
{
    private readonly ZolianHostFixture _fx;

    public TemplateCacheInvariantsTests(ZolianHostFixture fx) => _fx = fx;

    [Fact]
    public void Global_map_cache_is_populated()
    {
        Assert.NotNull(_fx.Setup.GlobalMapCache);
        Assert.True(_fx.Setup.GlobalMapCache.Count > 0);
    }

    [Fact]
    public void Core_template_caches_are_populated()
    {
        Assert.NotNull(_fx.Setup.GlobalWorldMapTemplateCache);
        Assert.NotNull(_fx.Setup.GlobalWarpTemplateCache);
        Assert.NotNull(_fx.Setup.GlobalSkillTemplateCache);
        Assert.NotNull(_fx.Setup.GlobalSpellTemplateCache);
        Assert.NotNull(_fx.Setup.GlobalItemTemplateCache);
        Assert.NotNull(_fx.Setup.GlobalNationTemplateCache);
        Assert.NotNull(_fx.Setup.GlobalMonsterTemplateCache);
        Assert.NotNull(_fx.Setup.GlobalMundaneTemplateCache);

        Assert.True(_fx.Setup.GlobalWorldMapTemplateCache.Count > 0);
        Assert.True(_fx.Setup.GlobalWarpTemplateCache.Count > 0);
        Assert.True(_fx.Setup.GlobalSkillTemplateCache.Count > 0);
        Assert.True(_fx.Setup.GlobalSpellTemplateCache.Count > 0);
        Assert.True(_fx.Setup.GlobalItemTemplateCache.Count > 0);
        Assert.True(_fx.Setup.GlobalNationTemplateCache.Count > 0);
        Assert.True(_fx.Setup.GlobalMonsterTemplateCache.Count > 0);
        Assert.True(_fx.Setup.GlobalMundaneTemplateCache.Count > 0);
    }

    [Fact]
    public void Buffs_and_debuffs_are_cached()
    {
        Assert.NotNull(_fx.Setup.GlobalBuffCache);
        Assert.NotNull(_fx.Setup.GlobalDeBuffCache);
        Assert.True(_fx.Setup.GlobalBuffCache.Count > 0);
        Assert.True(_fx.Setup.GlobalDeBuffCache.Count > 0);
    }

    [Fact]
    public void Monster_creation_script_is_configured()
    {
        Assert.False(string.IsNullOrWhiteSpace(ServerSetup.Instance.Config.MonsterCreationScript));
    }
}
