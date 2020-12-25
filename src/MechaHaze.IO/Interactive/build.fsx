#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.DotNet.MSBuild
nuget Fake.Core.Target
//"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.DotNet
open Fake.IO.Globbing.Operators

Target.initEnvironment ()

Target.create "Clean" (fun p -> Trace.trace " --- Cleaning stuff (empty) --- ")

Target.create "Build" (fun _ ->
    Trace.trace " --- Building the app --- "

    !! "../*.fsproj"
    |> MSBuild.runDebug id ".." "Build"
    |> Trace.logItems "TestBuild-Output: ")

Target.create "Watch" (fun _ ->
    Trace.trace " --- Watching app --- "
    )

open Fake.Core.TargetOperators

"Clean" ==> "Build" ==> "Watch"

Target.runOrDefault "Watch"
