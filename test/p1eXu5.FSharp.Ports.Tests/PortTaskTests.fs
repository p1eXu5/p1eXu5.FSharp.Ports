namespace p1eXu5.FSharp.Ports.Tests

open System
open System.Threading.Tasks

open NUnit.Framework
open FsUnit
open p1eXu5.FSharp.Testing.ShouldExtensions

open p1eXu5.FSharp.Ports
open p1eXu5.FSharp.Ports.PortTaskBuilderCE

open p1eXu5.FSharp.Ports.Tests.Tasks
open p1eXu5.FSharp.Ports.PortResultBuilderCE
open FsUnitTyped.TopLevelOperators

module PortTaskTests =

    [<Test>]
    let ``Bind with succeeded Task{T}`` () =
        let tp =
            portTask {
                let! _ = TestTaskFactory.SimpleTaskWithReturn(5)
                return 5
            }

        let res = tp |> PortTask.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``Bind with succeeded ValueTask{T}`` () =
        let tp =
            portTask {
                let! _ = TestValueTaskFactory.SimpleValueTaskWithReturn(5)
                return 5
            }

        let res = tp |> PortTask.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``Bind with succeeded portResult`` () =
        let success () = portResult { return! Ok 7 }
        let tp =
            portTask {
                let! v = success () |> PortTask.fromPort
                return v
            }

        let res = tp |> PortTask.runSynchronously ()
        res |> Result.shouldEqual 7

    [<Test>]
    let ``Bind with portTask of unit`` () =
        let inner asd : PortTask<unit, unit> =
            portTask {
                return ()
            }

        let tp =
            portTask {
                do! inner "asd"
                return 7
            }

        let res = tp |> PortTask.runSynchronously ()
        res |> shouldEqual 7


    [<Test>]
    let ``ask returns env``() =
        let sut =
            portTask {
                let! env = PortTask.ask
                return env
            }

        let res = sut |> PortTask.runSynchronously 3
        res |> should equal 3

    [<Test>]
    let ``Return from task`` () =
        let task = task { return 5 }
        let tp = portTask { return! task }
        let res = tp |> PortTask.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``Return from value task`` () =
        let valueTask = ValueTask.FromResult(5)
        let tp = portTask { return! valueTask }
        let res = tp |> PortTask.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``Return from cs task`` () =
        let tp = portTask { return! TestTaskFactory.SimpleTaskWithReturn(5) }
        let res = tp |> PortTask.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``Return from cs value task`` () =
        let tp = portTask { return! TestValueTaskFactory.SimpleValueTaskWithReturn(5) }
        let res = tp |> PortTask.runSynchronously ()
        res |> should equal 5

    [<Test>]
    let ``Try binding with thowing cs task test`` () =
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
    let ``Try binding with port task thowing cs task test`` () =
        let innertp =
            portTask {
                let! _ = TestTaskFactory.SimpleTaskWithException(5)
                return 5
            }

        let tp =
            portTask {
                try
                    return! innertp
                with
                    ex ->
                        return! portTask { return 6 }
            }

        let res = tp |> PortTask.runSynchronously ()
        res |> should equal 6

    [<Test>]
    let ``Try binding with discarding cs value task`` () =
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
    let ``Use binding with cs disposable task`` () =
        let tp =
            portTask {
                use! d = TestTaskFactory.CreateSyncDisposableAsync()
                do! (d :> ISyncDisposable).DoTaskAsync()
                return d
            }

        let res = tp |> PortTask.runSynchronously ()
        (res :?> SyncDisposable).IsDisposed |> should be True


    [<Test>]
    let ``Use binding with cs async disposable task`` () =
        let tp =
            portTask {
                use! d = TestTaskFactory.CreateAsynchronousDisposableAsync()
                do! (d :> IAsynchronousDisposable).DoTaskAsync()
                return d
            }

        let res = tp |> PortTask.runSynchronously ()
        (res :?> AsynchronousDisposable).IsDisposed |> should be True


    [<Test>]
    let ``For binding with cs task test`` () =
        let tp =
            portTask {
                for _ in 1..3 do
                    do! TestTaskFactory.DoTaskAsync()

                return true
            }

        let res = tp |> PortTask.runSynchronously ()
        res |> should be True
