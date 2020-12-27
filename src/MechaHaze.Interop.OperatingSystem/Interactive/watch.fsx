//https://github.com/Karamell/fsipavlov

#r "nuget: FSharp.Compiler.Service"

open FSharp.Compiler.Interactive.Shell
open System
open System.IO
open System.Text

let runScript =
    Path.Combine(Environment.CurrentDirectory, "run.fsx")

let dependenciesScriptFileName = "dependencies.fsx"
let dependenciesScript =
    Path.Combine(Environment.CurrentDirectory, dependenciesScriptFileName)

module Session =
    let sbOut = StringBuilder()
    let sbErr = StringBuilder()
    let inStream = new StringReader("")
    let outStream = new StringWriter(sbOut)
    let errStream = new StringWriter(sbErr)

    // Build command line arguments & start FSI session
    let argv = [| "dotnet fsi" |]

    let allArgs =
        Array.append argv [| "--noninteractive" |]

    let fsiConfig =
        FsiEvaluationSession.GetDefaultConfiguration()

    let fsiSession =
        FsiEvaluationSession.Create(fsiConfig, allArgs, inStream, outStream, errStream)

    let eval (scriptFile) =
        try
            let lines =
                File.ReadAllLines(scriptFile)
                |> Array.filter (fun s -> not <| s.Contains dependenciesScriptFileName)

            fsiSession.EvalInteraction(String.Join('\n', lines))
        with ex ->
            printfn "%s" (sbErr.ToString())
            sbErr.Clear() |> ignore

let handleWatcherEvents (e: FileSystemEventArgs) =
    let fi = FileInfo e.FullPath
    printfn "'%s' %A" (fi.Name) (e.ChangeType)

    if fi.Attributes.HasFlag FileAttributes.Hidden
       || fi.Attributes.HasFlag FileAttributes.Directory
       || fi.Name = __SOURCE_FILE__ then
        ()
    elif Path.Combine(Environment.CurrentDirectory, fi.Name) = dependenciesScript then
        Session.fsiSession.EvalScriptNonThrowing e.FullPath
        |> ignore
    else
        Session.eval (runScript)

let watcher =
    let srcDir = Environment.CurrentDirectory

    let w =
        new FileSystemWatcher(srcDir, "*.fsx", EnableRaisingEvents = true, IncludeSubdirectories = true)

    w.Changed.Add(handleWatcherEvents)

printfn "run the load script '%s'." dependenciesScript

Session.fsiSession.EvalScriptNonThrowing dependenciesScript
|> ignore

printfn "run startup/run script '%s'." runScript
Session.eval runScript
printfn "Ready. Now try and change '%s' ..." runScript
Console.Read() |> ignore
printfn "Wow. That was fast!"
