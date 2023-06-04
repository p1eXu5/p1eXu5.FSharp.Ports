namespace p1eXu5.FSharp.Ports.Tests

open System
open System.Threading.Tasks

open NUnit.Framework
open FsUnit

open p1eXu5.FSharp.Ports
open p1eXu5.FSharp.Ports.TaskPortBuilderCE

open p1eXu5.FSharp.Ports.Tests.Tasks

module TaskPortTests =

    [<Test>]
    let ``return from task test`` () =
        let task = task { return 5 }
        let tp = taskPort { return! task }
        let res = tp |> TaskPort.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``return from value task test`` () =
        let valueTask = ValueTask.FromResult(5)
        let tp = taskPort { return! valueTask }
        let res = tp |> TaskPort.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``return from cs task test`` () =
        let tp = taskPort { return! TestTaskFactory.SimpleTaskWithReturn(5) }
        let res = tp |> TaskPort.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``return from cs value task test`` () =
        let tp = taskPort { return! TestValueTaskFactory.SimpleValueTaskWithReturn(5) }
        let res = tp |> TaskPort.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``binding discarding cs task test`` () =
        let tp =
            taskPort {
                let! _ = TestTaskFactory.SimpleTaskWithReturn(5)
                return 5
            }

        let res = tp |> TaskPort.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``binding discarding cs value task test`` () =
        let tp =
            taskPort {
                let! _ = TestValueTaskFactory.SimpleValueTaskWithReturn(5)
                return 5
            }

        let res = tp |> TaskPort.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``try with discarding cs exception task test`` () =
        let tp =
            taskPort {
                try
                    let! _ = TestTaskFactory.SimpleTaskWithException(5)
                    return 5
                with
                    ex ->
                        return 6
            }

        let res = tp |> TaskPort.runSynchronously ()
        res |> should equal 6

    [<Test>]
    let ``try with discarding cs value task test`` () =
        let tp =
            taskPort {
                try
                    let! _ = TestValueTaskFactory.SimpleValueTaskWithException(5)
                    return 5
                with
                    ex ->
                        return 6
            }

        let res = tp |> TaskPort.runSynchronously ()
        res |> should equal 6


    [<Test>]
    let ``use cs disposable task test`` () =
        let tp =
            taskPort {
                use! d = TestTaskFactory.CreateSyncDisposableAsync()
                do! (d :> ISyncDisposable).DoTaskAsync()
                return d
            }

        let res = tp |> TaskPort.runSynchronously ()
        (res :?> SyncDisposable).IsDisposed |> should be True


    [<Test>]
    let ``use cs async disposable task test`` () =
        let tp =
            taskPort {
                use! d = TestTaskFactory.CreateAsynchronousDisposableAsync()
                do! (d :> IAsynchronousDisposable).DoTaskAsync()
                return d
            }

        let res = tp |> TaskPort.runSynchronously ()
        (res :?> AsynchronousDisposable).IsDisposed |> should be True
