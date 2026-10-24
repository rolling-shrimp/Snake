using PatternNetwork.Gates;

namespace PatternNetwork.Gates.Tests;

public class ProjectNamePolicyTests
{
    [Fact]
    public void GetProjectNameCombinesSolutionNameAndPatternNodeName()
    {
        ProjectNamePolicy policy = new ProjectNamePolicy();

        string projectName = policy.GetProjectName("C:\\work\\SampleGame.sln", "Entities");

        Assert.Equal("SampleGame.Entities", projectName);
    }

    [Theory]
    [InlineData("invalid-name.sln")]
    [InlineData("invalid name.sln")]
    [InlineData("1Invalid.sln")]
    public void GetProjectNameRejectsInvalidSolutionNames(string solutionFileName)
    {
        ProjectNamePolicy policy = new ProjectNamePolicy();

        ArgumentException exception = Assert.Throws<ArgumentException>(() => policy.GetProjectName(solutionFileName, "Entities"));

        Assert.Contains("SolutionName", exception.Message);
    }
}
