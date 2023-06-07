namespace p1eXu5.FSharp.Ports.Tests

open System
open System.Threading.Tasks

open NUnit.Framework
open FsUnit

open p1eXu5.FSharp.Ports
open p1eXu5.FSharp.Ports.PortTaskBuilderCE

open p1eXu5.FSharp.Ports.Tests.Tasks

module PortTaskTests =

    [<Test>]
    let ``ask return test``() =
        let sut =
            portTask {
                let! env = PortTask.ask
                return env
            }

        let res = sut |> PortTask.runSynchronously 3
        res |> should equal 3

    [<Test>]
    let ``return from task test`` () =
        let task = task { return 5 }
        let tp = portTask { return! task }
        let res = tp |> PortTask.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``return from value task test`` () =
        let valueTask = ValueTask.FromResult(5)
        let tp = portTask { return! valueTask }
        let res = tp |> PortTask.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``return from cs task test`` () =
        let tp = portTask { return! TestTaskFactory.SimpleTaskWithReturn(5) }
        let res = tp |> PortTask.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``return from cs value task test`` () =
        let tp = portTask { return! TestValueTaskFactory.SimpleValueTaskWithReturn(5) }
        let res = tp |> PortTask.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``binding discarding cs task test`` () =
        let tp =
            portTask {
                let! _ = TestTaskFactory.SimpleTaskWithReturn(5)
                return 5
            }

        let res = tp |> PortTask.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``binding discarding cs value task test`` () =
        let tp =
            portTask {
                let! _ = TestValueTaskFactory.SimpleValueTaskWithReturn(5)
                return 5
            }

        let res = tp |> PortTask.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``try with discarding cs exception task test`` () =
        let tp =
            portTask {
                try
                    let! _ = TestTaskFactory.SimpleTaskWithException(5)
                    return 5
                with
                    ex ->
                        return! portTask { return 6 }
            }

        let res = tp |> PortTask.runSynchronously ()
        res |> should equal 6

    [<Test>]
    let ``try with discarding cs value task test`` () =
        let tp =
            portTask {
                try
                    let! _ = TestValueTaskFactory.SimpleValueTaskWithException(5)
                    return 5
                with
                    ex -> return 6
            }

        let res = tp |> PortTask.runSynchronously ()
        res |> should equal 6


    [<Test>]
    let ``use cs disposable task test`` () =
        let tp =
            portTask {
                use! d = TestTaskFactory.CreateSyncDisposableAsync()
                do! (d :> ISyncDisposable).DoTaskAsync()
                return d
            }

        let res = tp |> PortTask.runSynchronously ()
        (res :?> SyncDisposable).IsDisposed |> should be True


    [<Test>]
    let ``use cs async disposable task test`` () =
        let tp =
            portTask {
                use! d = TestTaskFactory.CreateAsynchronousDisposableAsync()
                do! (d :> IAsynchronousDisposable).DoTaskAsync()
                return d
            }

        let res = tp |> PortTask.runSynchronously ()
        (res :?> AsynchronousDisposable).IsDisposed |> should be True


    [<Test>]
    let ``for cs task test`` () =
        let tp =
            portTask {
                for _ in 1..3 do
                    do! TestTaskFactory.DoTaskAsync()

                return true
            }

        let res = tp |> PortTask.runSynchronously ()
        res |> should be True
