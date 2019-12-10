using UTools.Utility;

internal class RgArgumentsInfo : ArgumentsInfo
{
    #region Methods

    internal RgArgumentsInfo(string pattern, string args) : this(
        pattern,
        UToolsUtil.DataPath,
        args
    )
    {
    }

    private string pattern;
    private string workDir;
    private string exArgs;

    internal RgArgumentsInfo(
        string pattern,
        string workDir,
        string exArgs
    ) : base(
        ReferenceToolSetting.ripgrepPath
    )
    {
        this.pattern = pattern;
        this.workDir = workDir;
        this.exArgs = exArgs;
        if (UToolsUtil.IsMac)
        {
            args = $"\'{pattern}\' \'{workDir}\' {exArgs}";
        }
        else
        {
            args = $"\"{pattern}\" \"{workDir}\" {exArgs}";
        }
    }

    internal void SetWorkDir(string workDir) => args = $"\'{pattern}\' \'{workDir}\' {exArgs}";

    #endregion
}