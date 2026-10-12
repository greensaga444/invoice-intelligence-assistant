using System.IO;

namespace InvoiceIntelligence.Core.Configuration;

public interface IModuleConfigurationLoader
{
    ModuleConfiguration Load(string iniPath);
}

public sealed class IniModuleConfigurationLoader : IModuleConfigurationLoader
{
    private const string DefaultKnowledgeRepositoryPath = "data\\knowledge\\knowledge-cases.json";

    public ModuleConfiguration Load(string iniPath)
    {
        if (string.IsNullOrWhiteSpace(iniPath))
        {
            throw new ArgumentException("INI path cannot be empty.", nameof(iniPath));
        }

        if (!File.Exists(iniPath))
        {
            throw new FileNotFoundException("INI file does not exist.", iniPath);
        }

        var iniData = IniFileParser.Parse(File.ReadLines(iniPath));

        var moduleName = iniData.GetRequired("module", "name");
        var folderAPath = iniData.GetRequired("paths", "folderA");
        var folderBPath = iniData.GetRequired("paths", "folderB");
        var outputNamingRule = iniData.GetRequired("output", "fileNamingRule");
        var mofAccount = iniData.GetRequired("mof", "account");
        var mofCredentialKey = iniData.GetRequired("mof", "credentialKey");
        var knowledgeRepositoryPath = iniData.GetOptional("knowledge", "repositoryFile")
            ?? DefaultKnowledgeRepositoryPath;

        NamingRuleValidator.Validate(outputNamingRule);

        return new ModuleConfiguration(
            moduleName,
            folderAPath,
            folderBPath,
            outputNamingRule,
            mofAccount,
            mofCredentialKey,
            knowledgeRepositoryPath);
    }
}
