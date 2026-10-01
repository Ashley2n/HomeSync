using application.Services;

namespace test.unit.Services;

public class InviteCodeGeneratorTests
{
    private readonly InviteCodeGenerator _generator = new();

    [Fact]
    public void Generate_ReturnsCodeOfConfiguredLength()
    {
        Assert.Equal(InviteCodeGenerator.Length, _generator.Generate().Length);
    }

    [Fact]
    public void Generate_UsesOnlyAllowedCharacters()
    {
        for (var i = 0; i < 1000; i++)
            Assert.All(_generator.Generate(), c => Assert.Contains(c, InviteCodeGenerator.Alphabet));
    }

    [Fact]
    public void Generate_ExcludesLookAlikeCharacters()
    {
        Assert.DoesNotContain(InviteCodeGenerator.Alphabet, c => "0O1IL".Contains(c));
    }

    [Fact]
    public void Generate_ProducesDistinctCodes()
    {
        var codes = Enumerable.Range(0, 1000).Select(_ => _generator.Generate()).ToHashSet();
        Assert.Equal(1000, codes.Count);
    }
}