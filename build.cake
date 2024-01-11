#tool "nuget:?package=xunit.runner.console&version=2.6.2"
#tool "nuget:?package=OpenCover&version=4.7.1221"

var configuration = Argument("configuration", "Release");
var version = Argument<string>("buildVersion", null);
var target = Argument("target", "Default");

Task("Default").IsDependentOn("Pack");

Task("Clean").Does(()=> 
{
    CleanDirectory("./build");
    StartProcess("dotnet", "clean -v minimal -c:" + configuration);
});

Task("Build")
    .IsDependentOn("UseAppVeyorVersion")
    .IsDependentOn("Clean")
    .Does(()=> 
{
    var buildSettings =  new DotNetBuildSettings { Configuration = configuration };
    if(!string.IsNullOrEmpty(version)) buildSettings.ArgumentCustomization = args => args.Append("/p:Version=" + version);

    DotNetBuild("src/Cronos/Cronos.csproj",  buildSettings);
});

Task("Test").IsDependentOn("Build").Does(() =>
{
    DotNetTest("./tests/Cronos.Tests/Cronos.Tests.csproj", new DotNetTestSettings
    {
        Configuration = configuration,
        ArgumentCustomization = args => args.Append("/p:BuildProjectReferences=false")
    });
});

Task("TestCoverage").IsDependentOn("Test").Does(() => 
{
    OpenCover(
        tool => { tool.XUnit2("tests/Cronos.Tests/bin/" + configuration + "/**/Cronos.Tests.dll", new XUnit2Settings { ShadowCopy = false }); },
        new FilePath("coverage.xml"),
        new OpenCoverSettings()
            .WithFilter("+[Cronos]*")
            .WithFilter("-[Cronos.Tests]*"));
});

Task("Pack").IsDependentOn("TestCoverage").Does(()=> 
{
    CreateDirectory("build");
    
    CopyFiles(GetFiles("./src/Cronos/bin/**/*.nupkg"), "build");
    CopyFiles(GetFiles("./src/Cronos/bin/**/*.snupkg"), "build");
    Zip("./src/Cronos/bin/" + configuration, "build/Cronos-" + version +".zip");
});

Task("UseAppVeyorVersion").WithCriteria(AppVeyor.IsRunningOnAppVeyor).Does(() => 
{
    version = AppVeyor.Environment.Build.Version;

    if (AppVeyor.Environment.Repository.Tag.IsTag)
    {
        var tagName = AppVeyor.Environment.Repository.Tag.Name;
        if(tagName.StartsWith("v"))
        {
            version = tagName.Substring(1);
        }

        AppVeyor.UpdateBuildVersion(version);
    }
});
    
RunTarget(target);