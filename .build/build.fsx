#r "System.Xml"
#I "../packages/FAKE/tools"
#r "FakeLib.dll"
#load "../packages/SourceLink.Fake/tools/SourceLink.fsx"
#r "../packages/Tachyus.ConfigTools/lib/net45/Tachyus.ConfigTools.dll"

open System
open Fake
open Fake.AssemblyInfoFile
open System.Text
open SourceLink

MSBuildDefaults <- { MSBuildDefaults with Verbosity = Some MSBuildVerbosity.Minimal }

let isAppVeyorBuild = buildServer = BuildServer.AppVeyor

let buildDate =
    let pst = TimeZoneInfo.FindSystemTimeZoneById "Pacific Standard Time"
    DateTimeOffset(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, pst), pst.BaseUtcOffset)

let buildVersion =
    match getBuildParamOrDefault "version" "" with
    | "" -> environVarOrDefault "version" "0.0.0"
    | v -> v

printfn "__SOURCE_DIRECTORY__ is %s" __SOURCE_DIRECTORY__
    
Target "Clean" <| fun _ -> ["./bin";
                            "./SqlCommandAnalyze/bin";]
                            |> CleanDirs 

Target "AssemblyInfo" <| fun _ ->
    let iv = StringBuilder() // json
    iv.Appendf "{\\\"buildVersion\\\":\\\"%s\\\"" buildVersion
    iv.Appendf ",\\\"buildDate\\\":\\\"%s\\\"" (buildDate.ToString "yyyy'-'MM'-'dd'T'HH':'mm':'sszzz")
    if isAppVeyorBuild then
        iv.Appendf ",\\\"gitCommit\\\":\\\"%s\\\"" AppVeyor.AppVeyorEnvironment.RepoCommit
        iv.Appendf ",\\\"gitBranch\\\":\\\"%s\\\"" AppVeyor.AppVeyorEnvironment.RepoBranch
    iv.Appendf "}"

    let common = [
        Attribute.Company "Tachyus"
        Attribute.Copyright (sprintf "Copyright © 2015 - %s" (DateTime.Now.Year.ToString()))
        Attribute.Version buildVersion
        Attribute.InformationalVersion (iv.ToString()) ]

    common |> CreateCSharpAssemblyInfo "./SqlCommandAnalyze/AssemblyInfo.fs"

Target "BuildDebug" <| fun _ ->
    !! "./ProxyUtilities.sln" |> MSBuildDebug "" "Build" |> ignore

Target "BuildRelease" <| fun _ ->
    !! "./ProxyUtilities.sln" |> MSBuildRelease "" "Build" |> ignore

Target "Entry" (fun _ ->())
Target "All" (fun _ ->())

"Entry"
    ==> "Clean"
    ==> "AssemblyInfo"
    ==> "BuildDebug"
    ==> "BuildRelease"
    ==> "All"

RunTargetOrDefault "All"
