namespace p1eXu5.FSharp.Ports.Tests

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
