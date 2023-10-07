namespace p1eXu5.FSharp.Ports.Tests

open System
open System.Threading.Tasks

open NUnit.Framework
open FsUnit
open FsToolkit.ErrorHandling

open p1eXu5.FSharp.Testing.ShouldExtensions
open p1eXu5.FSharp.Ports.Tests.Tasks

open p1eXu5.FSharp.Ports
open p1eXu5.FSharp.Ports.PortTaskResult
open p1eXu5.FSharp.Ports.PortTaskResultBuilderCE
open p1eXu5.FSharp.Ports.PortResultBuilderCE

module PortTaskResultTests =
    [<Test>]
    let ``ask return test``() =
        let sut =
            portTaskResult {
                let! env = PortTaskResult.ask
                return env
            }

        let res = sut |> PortTaskResult.runSynchronously 3
        res |> Result.shouldEqual 3

    [<Test>]
    let ``Bind with succeeded taskResult: success``() =
        let success1 () = taskResult { return! Ok 1 }

        let sut = portTaskResult {
                let! a = success1 ()
                return a + 2
            }

        let res = sut |> runSynchronously ()
        res |> Result.shouldEqual 3

    [<Test>]
    let ``Bind with succeeded two taskResult: success``() =
        let success1 () = taskResult { return! Ok 1 }
        let success2 () = task { return Ok 2 }

        let sut =
            portTaskResult {
                let! a = success1 ()
                let! b = success2 ()
                return a + b
            }

        let res = sut |> runSynchronously ()
        res |> Result.shouldEqual 3

    [<Test>]
    let ``Bind with succeeded taskResult and task: success``() =
        let success1 () = taskResult { return! Ok 1 }
        let success2 () = task { return 2 }

        let sut =
            portTaskResult {
                let! a = success1 ()
                let! b = success2 () |> PortTaskResult.fromTaskT
                return a + b
            }

        let res = sut |> runSynchronously ()
        res |> Result.shouldEqual 3

    [<Test>]
    let ``Bind with succeeded taskResult and Task{T}: success``() =
        let success1 () = taskResult { return! Ok 1 }

        let sut =
            portTaskResult {
                let! a = success1 ()
                let! b = TestTaskFactory.SimpleTaskWithReturn(3) |> PortTaskResult.fromTaskT
                return a + b
            }

        let res = sut |> PortTaskResult.runSynchronously ()
        res |> Result.shouldEqual 4

    [<Test>]
    let ``Bind with succeeded taskResult and ValueTask{T}: success``() =
        let success1 () = taskResult { return! Ok 1 }

        let sut =
            portTaskResult {
                let! a = success1 ()
                let! b = TestValueTaskFactory.SimpleValueTaskWithReturn(3) |> PortTaskResult.fromValueTaskT
                return a + b
            }

        let res = sut |> PortTaskResult.runSynchronously ()
        res |> Result.shouldEqual 4

    [<Test>]
    let ``Bind with succeeded portResult and ValueTask{T}: success``() =
        let success () = portResult { return! Ok 1 }

        let sut =
            portTaskResult {
                let! a = success () |> PortTaskResult.fromPortResult
                let! b = TestValueTaskFactory.SimpleValueTaskWithReturn(3) |> PortTaskResult.fromValueTaskT
                return a + b
            }

        let res = sut |> PortTaskResult.runSynchronously ()
        res |> Result.shouldEqual 4

    [<Test>]
    let ``try with discarding cs exception task test`` () =
        let tp =
            portTaskResult {
                try
                    let! a = TestTaskFactory.SimpleTaskWithException(5) |> PortTaskResult.fromTaskT
                    return 5
                with
                    ex ->
                         return 6
            }

        let res = tp |> PortTaskResult.runSynchronously ()
        res |> Result.shouldEqual 6


    [<Test>]
    let ``try with discarding cs exception value task test`` () =
        let tp =
            portTaskResult {
                try
                    let! a = TestValueTaskFactory.SimpleValueTaskWithException(5) |> PortTaskResult.fromValueTaskT
                    return 5
                with
                    ex -> return 6
            }

        let res = tp |> PortTaskResult.runSynchronously ()
        res |> Result.shouldEqual 6


    [<Test>]
    let ``use cs disposable task test`` () =
        let tp =
            portTaskResult {
                use! d = TestTaskFactory.CreateSyncDisposableAsync() |> PortTaskResult.fromTaskT
                do! d.DoTaskAsync() |> PortTaskResult.fromTask
                return d
            }

        let res = tp |> PortTaskResult.runSynchronously ()
        match res with
        | Ok ok ->
            (ok :?> SyncDisposable).IsDisposed |> should be True
        | Error err ->
            raise (AssertionException(sprintf "%A" err))


    [<Test>]
    let ``use cs async disposable task test`` () =
        let tp =
            portTaskResult {
                use! d = TestTaskFactory.CreateAsynchronousDisposableAsync() |> PortTaskResult.fromTaskT
                do! d.DoTaskAsync() |> PortTaskResult.fromTask
                return d
            }

        let res = tp |> PortTaskResult.runSynchronously ()
        match res with
        | Ok ok ->
            (ok :?> AsynchronousDisposable).IsDisposed |> should be True
        | Error err ->
            raise (AssertionException(sprintf "%A" err))


    [<Test>]
    let ``for cs task test`` () =
        let tp =
            portTaskResult {
                for _ in 1..3 do
                    do! TestTaskFactory.DoTaskAsync() |> PortTaskResult.fromTask

                return true
            }

        let res = tp |> PortTaskResult.runSynchronously ()
        res |> Result.shouldBe True
