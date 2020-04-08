#r "paket: groupref Build //"
#load "./.fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "netstandard"
#endif

open System

open Fake.Core
open Fake.DotNet
open Fake.IO

let path = Path.getFullName "./src"
let distDir = Path.getFullName "./dist"


let platformTool tool winTool =
    let tool = if Environment.isUnix then tool else winTool
    match ProcessUtils.tryFindFileOnPath tool with
    | Some t -> t
    | _ ->
        let errorMsg =
            tool + " was not found in path. " +
            "Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
        failwith errorMsg

let nodeTool = platformTool "node" "node.exe"
let yarnTool = platformTool "yarn" "yarn.cmd"

let install = lazy DotNet.install id

let inline withWorkDir wd =
    DotNet.Options.lift install.Value
    >> DotNet.Options.withWorkingDirectory wd

let runTool cmd (args:string) workingDir =
    let result =
        RawCommand (cmd, Arguments.Empty |> Arguments.appendRaw args)
        |> CreateProcess.fromCommand
        |> CreateProcess.withWorkingDirectory workingDir
        |> CreateProcess.withTimeout TimeSpan.MaxValue
        |> Proc.run
    if result.ExitCode <> 0 then
        failwithf "'%s %s' failed" cmd args

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (withWorkDir workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let openBrowser url =
    let result =
        ShellCommand (url)
        |> CreateProcess.fromCommand
        |> CreateProcess.withTimeout TimeSpan.MaxValue
        |> Proc.run
        
    //https://github.com/dotnet/corefx/issues/10361
    if result.ExitCode <> 0
        then failwithf "opening browser failed"
    
Target.create "Empty" (fun _ -> ())

Target.create "Clean" (fun _ ->
    Shell.cleanDirs [distDir]
)

Target.create "InstallClient" (fun _ ->
    printfn "Node version:"
    runTool nodeTool "--version" __SOURCE_DIRECTORY__
    printfn "yarn version:"
    runTool yarnTool "--version" __SOURCE_DIRECTORY__
    runTool yarnTool "install --frozen-lockfile" __SOURCE_DIRECTORY__
    runDotNet "restore" path
)

Target.create "Build" (fun _ ->
    runTool yarnTool "run webpack -p" path
)

let run () =
    runTool yarnTool "run webpack-dev-server" path

Target.create "Run" (fun _ ->
    run ()
)

Target.create "FastRun" (fun _ ->
    run ()
)

open Fake.Core.TargetOperators

"Empty"
    ==> "Clean"
    ==> "InstallClient"
    ==> "Build"

"Empty"
    ==> "FastRun"
    
"Empty"
    ==> "Clean"
    ==> "InstallClient"
    ==> "Run"
    

Target.runOrDefault "Bundle"
