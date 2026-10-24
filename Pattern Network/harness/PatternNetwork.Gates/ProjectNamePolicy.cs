using System.Text.RegularExpressions;

namespace PatternNetwork.Gates;

public class ProjectNamePolicy
{
    public string GetProjectName(string solutionPath, string patternNodeName)
    {
        string solutionName = Path.GetFileNameWithoutExtension(solutionPath);
        ValidateIdentifierPath(solutionName, "SolutionName");
        ValidateIdentifierPath(patternNodeName, "PatternNodeName");
        return $"{solutionName}.{patternNodeName}";
    }

    private void ValidateIdentifierPath(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value) || !Regex.IsMatch(value, "^[A-Z][A-Za-z0-9]*(\\.[A-Z][A-Za-z0-9]*)*$"))
        {
            throw new ArgumentException($"{fieldName} 必須是 PascalCase 或以句點分隔的 PascalCase 識別字。", fieldName);
        }
    }
}
